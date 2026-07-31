using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// 수학, 랜덤, NavMesh 활용 관련 확장 메서드 정의
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// 단위 원판(Disk) 내 임의의 점(float3, y=0)을 반환
    /// </summary>
    public static float3 NextOnDisk(this ref Unity.Mathematics.Random self)
    {
        while (true)
        {
            var v = self.NextFloat2(-1, 1);
            if (math.length(v) <= 1)
                return math.float3(v.x, 0, v.y);
        }
    }

    /// <summary>
    /// y축을 기준으로 임의의 회전(quaternion) 반환
    /// </summary>
    public static quaternion NextYRotation(this ref Unity.Mathematics.Random self)
        => quaternion.RotateY(self.NextFloat(math.PI * 2));

    /// <summary>
    /// NavMesh 위에 놓인 임의의 몬스터 스폰 위치를 반환(최대 maxAttempts 회 재시도)
    /// </summary>
    public static float3 CreateRandomSpawnPosition(this ref Unity.Mathematics.Random self, float radiusX, float radiusZ)
    {
        int maxAttempts = 5; // 최대 재시도 횟수
        float3 candidatePos = float3.zero;

        for (int i = 0; i < maxAttempts; i++)
        {
            var diskPoint = self.NextOnDisk();
            candidatePos = new float3(diskPoint.x * radiusX, 0f, diskPoint.z * radiusZ);

            // NavMesh 상 유효 위치 찾으면 바로 반환
            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        // 끝까지 실패 시 디폴트 위치(float3.zero) 반환, 경고 출력
        Debug.LogWarning("NavMesh 위치 생성 실패! 기본 위치로 대체합니다.");
        return float3.zero;
    }
}

/// <summary>
/// 몬스터 스폰을 담당하는 System. (InitializationSystemGroup에서 동작)
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))] 
public partial struct MonsterSpawnSystem: ISystem
{
    private double timer; // 스폰 주기 타이머

    // GC Alloc 방지: EntityQuery 및 ComponentLookup 멤버 변수 캐싱
    private EntityQuery monsterQuery;
    private ComponentLookup<LocalTransform> transformLookup;
    private ComponentLookup<MonsterData> monsterDataLookup;
    private ComponentLookup<NavAgentComponent> navAgentLookup;

    /// <summary> 최초 1회만 쿼리/lookup 초기화 </summary>
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        timer = 0.0;

        // 몬스터 쿼리 1회 구축 (모든 MonsterData 가진 Entity)
        monsterQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<MonsterData>() },
            Options = EntityQueryOptions.IncludeDisabledEntities
        });

        // Lookup 초기화
        transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: false);
        monsterDataLookup = state.GetComponentLookup<MonsterData>(isReadOnly: false);
        navAgentLookup = state.GetComponentLookup<NavAgentComponent>(isReadOnly: false);
    }

    /// <summary>매 프레임마다 몬스터 스폰 로직 수행</summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {        
        var config = SystemAPI.GetSingleton<Config>();
        
        // 현재 몬스터 개수 측정 (GC 없음)
        int currentMonsterCount = monsterQuery.CalculateEntityCount();

        // 최대치 도달 시 조기 반환
        if (currentMonsterCount >= config.SpawncountMax)
        {
            return;
        }

        // 타이머 누적
        timer += SystemAPI.Time.DeltaTime;
        
        if (timer >= 1.0)
        {
            timer -= 1.0; // 1초마다 스폰

            // 이번 틱에서 스폰 가능한 최대 몬스터 수 계산
            int allowedSpawnCount = config.SpawncountMax - currentMonsterCount;
            int spawnCountThisTick = math.min(config.Spawncount, allowedSpawnCount);

            // 더 이상 생성 불필요하면 탈출
            if (spawnCountThisTick <= 0)
            {
                return;
            }

            // UI 카운트 증가(Entity 있으면)
            if (SystemAPI.TryGetSingletonEntity<UICountComponent>(out var uiCountEntity))
            {
                var uiCountRW = SystemAPI.GetComponentRW<UICountComponent>(uiCountEntity);
                uiCountRW.ValueRW.MonsterCount += spawnCountThisTick;
            }

            // 몬스터 프리팹 복수개 Instantiate
            var instances = state.EntityManager.Instantiate(config.Prefab, spawnCountThisTick, Allocator.TempJob);

            // 랜덤 시드 준비(시간+설정값)
            var rand = new Unity.Mathematics.Random((uint)( SystemAPI.Time.ElapsedTime * 1000 ) + config.RandomSeed);

            // 씬 내 플레이어 Entity 탐색(없으면 Null)
            Entity playerEntity = Entity.Null;
            if (SystemAPI.TryGetSingletonEntity<ECSPlayerData>(out var foundPlayer))
            {
                playerEntity = foundPlayer;
            }

            // Spawn 정보 저장용 NativeArray들 할당
            var spawnPositions = new NativeArray<float3>(spawnCountThisTick, Allocator.TempJob);
            var outputTransforms = new NativeArray<LocalTransform>(spawnCountThisTick, Allocator.TempJob);
            var outputMonsterData = new NativeArray<MonsterData>(spawnCountThisTick, Allocator.TempJob);
            
            // 각 몬스터의 NavMesh상 생성 위치 계산
            for (int i = 0; i < spawnCountThisTick; i++)
            {
                // 메인스레드: NavMesh.SamplePosition 사용
                spawnPositions[i] = rand.CreateRandomSpawnPosition(config.SpawnRadiusX, config.SpawnRadiusZ);
            }

            // 병렬로 위치/스탯 생성 및 결과 전달받을 잡 생성
            var spawnJob = new SpawnMonsterPosQurJob
            {
                Instances = instances,
                SpawnPositions = spawnPositions,
                BaseSeed = (uint)(SystemAPI.Time.ElapsedTime * 1000) + config.RandomSeed,
                OutputTransforms = outputTransforms,
                OutputMonsterData = outputMonsterData
            };

            // 잡 병렬 실행 및 완료까지 대기
            state.Dependency = spawnJob.Schedule(instances.Length, 64, state.Dependency);
            state.Dependency.Complete();

            // lookup 최신화
            transformLookup.Update(ref state);
            monsterDataLookup.Update(ref state);
            navAgentLookup.Update(ref state);

            // 결과 반영: 스폰된 각 Entity에 데이터 반영
            for (int i = 0; i < instances.Length; i++)
            {
                Entity monster = instances[i];
                if (monster == Entity.Null) continue;

                // 위치 및 몬스터 데이터 세팅
                transformLookup[monster] = outputTransforms[i];
                monsterDataLookup[monster] = outputMonsterData[i];

                // 길찾기 타겟을 플레이어로 셋팅(속도값 등은 덮어쓰지 않음)
                if (playerEntity != Entity.Null && navAgentLookup.HasComponent(monster))
                {
                    ref var navAgent = ref navAgentLookup.GetRefRW(monster).ValueRW;
                    navAgent.targetEntity = playerEntity;
                }
            }

            // 사용한 NativeArray 해제(메모리 릭 방지)
            instances.Dispose();
            spawnPositions.Dispose();
            outputTransforms.Dispose();
            outputMonsterData.Dispose();     
        }
    }
}
 
/// <summary>
/// 몬스터 위치/스탯을 병렬로 계산(Burst 지원)
/// </summary>
[BurstCompile]
public struct SpawnMonsterPosQurJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Entity> Instances;                     // 새로 생성된 몬스터 Entity 배열
    [ReadOnly] public NativeArray<float3> SpawnPositions;                // 미리 계산된 NavMesh 스폰 위치 배열

    public uint BaseSeed;                                                // 랜덤 시드 기반값
    [WriteOnly] public NativeArray<LocalTransform> OutputTransforms;     // 최종 위치/회전 결과
    [WriteOnly] public NativeArray<MonsterData> OutputMonsterData;       // 최종 MonsterData 결과

    public void Execute(int index)
    {
        // 실행 인덱스별 랜덤 인스턴스 생성(완전 독립)
        var rand = new Unity.Mathematics.Random(BaseSeed + (uint)index);
        float3 spawnPos = SpawnPositions[index];

        // 임의 회전값 포함하여 위치 결과 저장
        OutputTransforms[index] = LocalTransform.FromPositionRotation(spawnPos, rand.NextYRotation());
        OutputMonsterData[index] = MonsterData.Random(rand.NextUInt());
    }
}

/// <summary>
/// 부활 대기 중(Disabled)인 몬스터를 일정 주기로 살려내는 시스템
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MonsterReviveSystem: ISystem
{
    private double _nextReviveTime; // 다음 부활 대상 처리 타이밍

    public void OnCreate(ref SystemState state)
    {
        // Config 싱글톤 있어야 동작
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 아직 부활 쿨타임 안 됐으면 탈출
        if (SystemAPI.Time.ElapsedTime < _nextReviveTime)
            return;

        // 5초 쿨(하드코딩)
        _nextReviveTime = SystemAPI.Time.ElapsedTime + 5.0;

        // ECB(CommandBuffer) 획득
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 컨피그 등 기본값 준비(랜덤 시드, 스폰반경 등)
        var config = SystemAPI.GetSingleton<Config>();
        var rand = new Unity.Mathematics.Random((uint)( SystemAPI.Time.ElapsedTime * 1000 ) + config.RandomSeed);

        // 플레이어 Entity 탐색(없으면 부활 시 타겟 비워둠)
        Entity playerEntity = Entity.Null;
        if (SystemAPI.TryGetSingletonEntity<ECSPlayerData>(out var foundPlayer))
        {
            playerEntity = foundPlayer;
        }

        // Disabled 가진 모든 몬스터 순회
        foreach (var (monster, agent, transform, entity) in SystemAPI.Query<
                     RefRO<MonsterData>, 
                     RefRO<NavAgentComponent>, 
                     RefRO<LocalTransform>>()
                 .WithAll<Disabled>()
                 .WithEntityAccess())
        {
            // 1. Disabled 태그 제거(부활 처리)
            ecb.RemoveComponent<Disabled>(entity);

            // 2. NavMesh상 부활위치 계산 및 부여(회전은 기존 유지)
            float3 spawnPos = rand.CreateRandomSpawnPosition(config.SpawnRadiusX, config.SpawnRadiusZ);
            LocalTransform newTransform = LocalTransform.FromPositionRotation(spawnPos, transform.ValueRO.Rotation);
            ecb.SetComponent(entity, newTransform);

            // 3. 기존 길찾기 버퍼(Waypoint) 비우기 - 안전하게 GetBuffer 후 Clear
            DynamicBuffer<WaypointBuffer> waypoints = SystemAPI.GetBuffer<WaypointBuffer>(entity);
            waypoints.Clear(); 

            // 4 & 5. NavAgent 데이터 복사/수정/ECB로 넣기
            // (기존 moveSpeed 등 데이터 100% 유지!)
            var agentData = agent.ValueRO;
            agentData.nextPathCalculateTime = 0f;
            agentData.pathCalculated = false;
            
            // 최초 타겟 세팅
            if (playerEntity != Entity.Null)
            {
                agentData.targetEntity = playerEntity;
            }

            // 수정 NavAgent를 ECB에 반영
            ecb.SetComponent(entity, agentData);
        }
    }
}