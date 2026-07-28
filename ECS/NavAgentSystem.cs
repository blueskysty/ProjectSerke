using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

[BurstCompile]
public partial struct NavAgentSystem: ISystem
{
    // 웨이포인트 버퍼를 빠르게 조회하기 위한 룩업 객체
    private BufferLookup<WaypointBuffer> waypointBufferLookup;

    // 유니티 내브메시 월드 인스턴스
    private NavMeshWorld navMeshWorld;

    // 에이전트들을 관리하기 위한 쿼리
    private EntityQuery agentQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 1. 버퍼 룩업 초기화 (읽기/쓰기 가능 상태)
        waypointBufferLookup = state.GetBufferLookup<WaypointBuffer>(false);

        // 2. 기본 내브메시 월드 가져오기
        navMeshWorld = NavMeshWorld.GetDefaultWorld();

        // 3. 네비게이션 에이전트와 위치 컴포넌트를 가진 엔티티 쿼리 빌드
        agentQuery = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<NavAgentComponent, LocalTransform>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // 자원 해제 작업 (필요한 경우 여기에 작성)
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 매 프레임 버퍼 룩업 상태 갱신
        waypointBufferLookup.Update(ref state);

        // 비동기 잡(Job) 안에서 컴포넌트 변경 사항을 안전하게 기록하기 위한 ECB(Entity Command Buffer) 생성
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // -------------------------------------------------------------
        // [1단계] 이동 로직 (MoveJob) - 멀티스레드 병렬 처리를 위해 잡 스케줄링
        // -------------------------------------------------------------
        var moveJob = new MoveJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            WaypointBufferLookup = waypointBufferLookup,
            Ecb = ecb
        };
        // 시스템의 의존성(Dependency) 체인에 등록하여 병렬 실행 예약
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);

        // -------------------------------------------------------------
        // [동기화] 이동 잡이 완전히 끝날 때까지 메인 스레드 대기
        // -------------------------------------------------------------
        // 멀티스레드로 돌던 MoveJob이 끝난 후, 아래 메인 스레드 영역에서 
        // 데이터 충돌(Race Condition) 없이 안전하게 EntityManager에 접근할 수 있도록 동기화합니다.
        state.Dependency.Complete();

        // -------------------------------------------------------------
        // [2단계] 경로 계산 로직 (CalculatePath) - 메인 스레드에서 순차 처리
        // -------------------------------------------------------------
        // 유니티 내장 NavMeshQuery는 반드시 메인 스레드에서만 생성되어야 하므로 이곳에서 처리합니다.
        double elapsedTime = SystemAPI.Time.ElapsedTime;

        // 메인 스레드 전용 임시 쿼리 인스턴스 생성 (1회성 생성 후 재사용)
        NavMeshQuery query = new NavMeshQuery(navMeshWorld, Allocator.TempJob, 1000);

        // 모든 에이전트를 순회하며 경로 갱신 주기가 된 유닛을 색출
        foreach (var (navAgent, transform, waypointBuffer, entity) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>>().WithEntityAccess())
        {
            if (navAgent.ValueRO.nextPathCalculateTime < elapsedTime)
            {
                // 다음 갱신 시간 설정 및 경로 계산 대기 상태로 전환
                navAgent.ValueRW.nextPathCalculateTime = (float)elapsedTime + 1.0f;
                navAgent.ValueRW.pathCalculated = false;

                // 실제 길찾기 연산 수행 메서드 호출
                CalculatePath(navAgent, transform, waypointBuffer, ref state, query);
            }
        }

        // 사용이 끝난 쿼리 자원 해제
        query.Dispose();
    }

    /// <summary>
    /// NavMeshQuery를 이용해 실제로 길을 찾고 웨이포인트 버퍼를 채워주는 메인 스레드 전용 함수
    /// </summary>
    private void CalculatePath(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, DynamicBuffer<WaypointBuffer> waypointBuffer,
        ref SystemState state, NavMeshQuery query)
    {
        float3 fromPosition = transform.ValueRO.Position;

        // 타겟(목적지) 엔티티가 실제로 존재하는지 검증 후 위치 추출
        if (!state.EntityManager.Exists(navAgent.ValueRO.targetEntity))
        {
            Debug.LogWarning($"Target entity {navAgent.ValueRO.targetEntity} does not exist.");
            return;
        }

        float3 toPosition = state.EntityManager.GetComponentData<LocalTransform>(navAgent.ValueRO.targetEntity).Position;

        float3 extents = new float3(1, 1, 1);

        // 출발지와 목적지를 내브메시 표면에 매핑
        NavMeshLocation fromLocation = query.MapLocation(fromPosition, extents, 0);
        NavMeshLocation toLocation = query.MapLocation(toPosition, extents, 0);

        PathQueryStatus status;
        PathQueryStatus returningStatus;
        int maxPathSize = 100;

        // 매핑된 위치가 유효한 경우 길찾기 시작
        if (query.IsValid(fromLocation) && query.IsValid(toLocation))
        {
            status = query.BeginFindPath(fromLocation, toLocation);
            if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
            {
                // 경로 탐색 업데이트 (최대 100번의 연산 허용)
                status = query.UpdateFindPath(100, out int iterationsPerformed);
                if (status == PathQueryStatus.Success)
                {
                    status = query.EndFindPath(out int pathSize);

                    // 경로 결과 데이터를 담을 임시 NativeArray 생성
                    NativeArray<NavMeshLocation> result = new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                    NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                    NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                    NativeArray<PolygonId> polygonIds = new NativeArray<PolygonId>(pathSize + 1, Allocator.Temp);
                    int straightPathCount = 0;

                    query.GetPathResult(polygonIds);

                    // 폴리곤 경로를 매끄러운 직선 경로(Straight Path)로 변환
                    returningStatus = PathUtils.FindStraightPath(
                        query,
                        fromPosition,
                        toPosition,
                        polygonIds,
                        pathSize,
                        ref result,
                        ref straightPathFlag,
                        ref vertexSide,
                        ref straightPathCount,
                        maxPathSize
                    );

                    // 길찾기가 성공적으로 완료되면 웨이포인트 버퍼 갱신
                    if (returningStatus == PathQueryStatus.Success)
                    {
                        waypointBuffer.Clear();

                        foreach (NavMeshLocation location in result)
                        {
                            if (location.position != Vector3.zero)
                            {
                                waypointBuffer.Add(new WaypointBuffer { wayPoint = location.position });
                            }
                        }

                        // 에이전트 상태를 이동 가능하도록 플래그 갱신
                        navAgent.ValueRW.currentWaypoint = 0;
                        navAgent.ValueRW.pathCalculated = true;
                    }

                    // 사용한 임시 네이티브 배열 자원 해제
                    straightPathFlag.Dispose();
                    polygonIds.Dispose();
                    vertexSide.Dispose();
                    result.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// 에이전트의 이동 및 회전을 멀티스레드(Burst)로 고속 처리하는 잡 구조체
    /// </summary>
    [WithAll(typeof(NavAgentComponent), typeof(LocalTransform))]
    [BurstCompile]
    private partial struct MoveJob: IJobEntity
    {
        public float DeltaTime;
        [NativeDisableParallelForRestriction] public BufferLookup<WaypointBuffer> WaypointBufferLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [ChunkIndexInQuery] int sortedIndex, ref NavAgentComponent navAgent, ref LocalTransform transform)
        {
            // 경로가 계산되지 않았거나 유효한 웨이포인트 버퍼가 없으면 중단
            if (!navAgent.pathCalculated)
                return;

            if (!WaypointBufferLookup.TryGetBuffer(entity, out DynamicBuffer<WaypointBuffer> waypointBuffer) || waypointBuffer.Length == 0)
                return;

            // 현재 목표 웨이포인트에 충분히 도달했는지 체크 (0.4유닛 이내)
            if (navAgent.currentWaypoint < waypointBuffer.Length &&
                math.distance(transform.Position, waypointBuffer[navAgent.currentWaypoint].wayPoint) < 0.4f)
            {
                // 다음 웨이포인트가 남아있다면 인덱스 증가
                if (navAgent.currentWaypoint + 1 < waypointBuffer.Length)
                {
                    navAgent.currentWaypoint += 1;
                    Ecb.SetComponent(sortedIndex, entity, navAgent);
                }
            }

            // 모든 웨이포인트를 다 돌았으면 이동 중지
            if (navAgent.currentWaypoint >= waypointBuffer.Length)
                return;

            // 이동 방향 계산
            float3 direction = waypointBuffer[navAgent.currentWaypoint].wayPoint - transform.Position;
            if (math.lengthsq(direction) > 0.001f)
            {
                // 바라봐야 할 각도 계산 후 부드럽게 회전 (slerp)
                float angle = math.degrees(math.atan2(direction.z, direction.x));

                transform.Rotation = math.slerp(
                    transform.Rotation,
                    quaternion.Euler(new float3(0, angle, 0)),
                    DeltaTime);

                // 정해진 속도(moveSpeed)에 따라 위치 전진
                transform.Position += math.normalize(direction) * DeltaTime * navAgent.moveSpeed;

                // 변경된 위치 및 회전 값을 ECB에 예약
                Ecb.SetComponent(sortedIndex, entity, transform);
            }
        }
    }
}