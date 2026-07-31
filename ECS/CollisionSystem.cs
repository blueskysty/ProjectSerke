using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct CollisionSystem: ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // UICountComponent가 존재할 때만 시스템이 업데이트됨
        state.RequireForUpdate<UICountComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // EndSimulation에 쓸 EntityCommandBufferSystem의 싱글톤 가져옴 (예약된 커맨드 처리용)
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        // 시뮬레이션 싱글톤 (Physics 정보)
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // FixedStep(물리 프레임) 종료 시점의 ECB 생성
        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // UI 카운트 컴포넌트가 있는 엔티티 하나 획득
        var uiCountEntity = SystemAPI.GetSingletonEntity<UICountComponent>();

        // TriggerJob 생성 및 스케줄
        var job = new TriggerJob
        {
            MonsterGroup = SystemAPI.GetComponentLookup<MonsterData>(true),      // 몬스터 그룹 조회 (읽기전용)
            ECSPlayer = SystemAPI.GetComponentLookup<ECSPlayerData>(true),       // 플레이어 그룹 조회 (읽기전용)
            uiCountGroup = SystemAPI.GetComponentLookup<UICountComponent>(false),// UI 카운트 조회 (쓰기)
            uiCountEntity = uiCountEntity,                                       // 카운트 값을 업데이트할 단일 엔티티
            ECB = ecb
        };

        // Trigger 이벤트 잡을 스케줄링 (Dependency 연결)
        state.Dependency = job.Schedule(simulation, state.Dependency);
    }
}

// Trigger(겹침) 이벤트 잡
[BurstCompile]
struct TriggerJob: ITriggerEventsJob
{
    // 몬스터와 플레이어 등의 컴포넌트 존재 여부를 빠르게 조회하기 위한 Lookup
    [ReadOnly] public ComponentLookup<MonsterData> MonsterGroup;
    [ReadOnly] public ComponentLookup<ECSPlayerData> ECSPlayer;

    // 카운트 갱신을 위한 Lookup과 해당 엔티티
    public ComponentLookup<UICountComponent> uiCountGroup;
    public Entity uiCountEntity;

    // Entity에 컴포넌트를 추가/수정하기 위한 커맨드 버퍼
    public EntityCommandBuffer ECB;

    // Trigger(겹침) 이벤트 처리
    public void Execute(TriggerEvent te)
    {
        // 이벤트 A/B에 몬스터, 플레이어 컴포넌트가 있는지 확인
        bool aMonster = MonsterGroup.HasComponent(te.EntityA);
        bool bMonster = MonsterGroup.HasComponent(te.EntityB);
        bool aPlayer = ECSPlayer.HasComponent(te.EntityA);
        bool bPlayer = ECSPlayer.HasComponent(te.EntityB);

        // 몬스터와 플레이어가 겹쳤을 경우에만 처리
        if ((aMonster && bPlayer) || (aPlayer && bMonster))
        {
            // 몬스터가 누구인지 결정
            Entity monsterEntity = aMonster ? te.EntityA : te.EntityB;

            // 몬스터 엔티티를 Disabled로 만들어 죽은 처리
            ECB.AddComponent<Disabled>(monsterEntity);

            // KillCount 증가
            var killData = uiCountGroup[uiCountEntity];
            killData.KillCount++;
            uiCountGroup[uiCountEntity] = killData;
        }
    }
}

// (참고) 충돌(콜리젼, 물리접촉) 이벤트 잡, 현재 사용 안 함
//[BurstCompile]
//struct CollisionJob : ICollisionEventsJob
//{
//    [ReadOnly]
//    public ComponentLookup<MonsterData> MonsterGroup;
//
//    public EntityCommandBuffer ECB;
//
//    public void Execute(CollisionEvent ce)
//    {
//        Debug.Log(1);
//        bool a = MonsterGroup.HasComponent(ce.EntityA);
//        bool b = MonsterGroup.HasComponent(ce.EntityB);
//
//        if (a && b)
//        {
//            // 몬스터끼리 부딪히면 몬스터 데이터 비활성화 예시
//            ECB.SetComponentEnabled<MonsterData>(ce.EntityA, false);
//        }
//    }
//}
