using Unity.Entities;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

[BurstCompile]
public partial struct NavAgentSystem: ISystem
{
    // 웨이포인트 버퍼 룩업
    private BufferLookup<WaypointBuffer> waypointBufferLookup;

    // 내브메시 월드 인스턴스
    private NavMeshWorld navMeshWorld;

    // 에이전트 쿼리
    private EntityQuery agentQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 버퍼 룩업 초기화
        waypointBufferLookup = state.GetBufferLookup<WaypointBuffer>(false);

        // 내브메시 월드 받아오기
        navMeshWorld = NavMeshWorld.GetDefaultWorld();

        // 쿼리 빌드
        agentQuery = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<NavAgentComponent, LocalTransform>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // 리소스 해제 (필요 시 구현)
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 버퍼 룩업 갱신
        waypointBufferLookup.Update(ref state);

        // ECB 생성 (멀티스레드 잡 내부에서 사용)
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        //   이동 로직 (MoveJob) - 멀티스레드 병렬 처리를 위해 잡 스케줄링
        var moveJob = new MoveJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            WaypointBufferLookup = waypointBufferLookup,
            Ecb = ecb
        };
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);

        // 이동 잡 동기화
        state.Dependency.Complete();

        // 경로 계산 (메인 스레드, NavMeshQuery 생성 필요)
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
        var targetEntity = navAgent.ValueRO.targetEntity;
        if (targetEntity == Entity.Null || !SystemAPI.Exists(targetEntity))
        {   
            return;
        }

        float3 fromPosition = transform.ValueRO.Position;
        float3 toPosition = state.EntityManager.GetComponentData<LocalTransform>(navAgent.ValueRO.targetEntity).Position;     

        float3 extents = new float3(3, 3, 3);

        // 출발지와 목적지를 내브메시 표면에 매핑
        NavMeshLocation fromLocation = query.MapLocation(fromPosition, extents, 0);
        NavMeshLocation toLocation = query.MapLocation(toPosition, extents, 0);

        PathQueryStatus status;
        PathQueryStatus returningStatus;
        int maxPathSize = 256;

        // 매핑된 위치가 유효한 경우 길찾기 시작
        if (query.IsValid(fromLocation) && query.IsValid(toLocation))
        {
            status = query.BeginFindPath(fromLocation, toLocation);

            if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
            {

                // 거리가 아무리 멀어도 길찾기가 완성(Success)될 때까지 반복 연산!
                // (단, 무한 루프 방지를 위해 최대 iteration 상한선 설정)
                int maxIterations = 1000;
                int performedIterations = 0;
                
                while (status == PathQueryStatus.InProgress && performedIterations < maxIterations)
                {
                    status = query.UpdateFindPath(100, out int iterations);
                    performedIterations += iterations;
                }

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

                    if (returningStatus == PathQueryStatus.Success && straightPathCount > 0)
                    {
                        waypointBuffer.Clear();

                        // result 전체가 아니라 실제 계산된 straightPathCount 만큼만 순회!
                        for (int i = 0; i < straightPathCount; i++)
                        {
                            float3 pos = result[i].position;
                            if (!pos.Equals(float3.zero))
                            {
                                waypointBuffer.Add(new WaypointBuffer { wayPoint = pos });
                            }
                        }

                        // 에이전트 이동 가능 상태로 전환
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
            {                
                return;
            }

            if (!WaypointBufferLookup.TryGetBuffer(entity, out DynamicBuffer<WaypointBuffer> waypointBuffer) || waypointBuffer.Length == 0)
            {
                return;
            }

            // 현재 웨이포인트 도달 체크 (0.4, 이하)
            if (navAgent.currentWaypoint < waypointBuffer.Length &&
                math.distance(transform.Position, waypointBuffer[navAgent.currentWaypoint].wayPoint) < 0.4f)
            {
                if (navAgent.currentWaypoint + 1 < waypointBuffer.Length)
                {
                    navAgent.currentWaypoint += 1;
                    Ecb.SetComponent(sortedIndex, entity, navAgent);
                }
            }

            // 경로 끝
            if (navAgent.currentWaypoint >= waypointBuffer.Length)
                return;

            // 이동 벡터 계산
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

                // ECB 반영
                Ecb.SetComponent(sortedIndex, entity, transform);
            }
        }
    }
}