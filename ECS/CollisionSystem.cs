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
        state.RequireForUpdate<UICountComponent>();
    }


    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var uiCountEntity = SystemAPI.GetSingletonEntity<UICountComponent>();

        var job = new TriggerJob
        {
            MonsterGroup = SystemAPI.GetComponentLookup<MonsterData>(true),
            ECSPlayer = SystemAPI.GetComponentLookup<ECSPlayerData>(true),
            uiCountGroup = SystemAPI.GetComponentLookup<UICountComponent>(false),
            uiCountEntity = uiCountEntity, // �̸� �����ص� ī���� ��ƼƼ
            ECB = ecb
        };

        state.Dependency = job.Schedule(simulation, state.Dependency);
    }
}

// Ʈ���� �̺�Ʈ
[BurstCompile]
struct TriggerJob: ITriggerEventsJob
{
    [ReadOnly]    public ComponentLookup<MonsterData> MonsterGroup;
    [ReadOnly]    public ComponentLookup<ECSPlayerData> ECSPlayer;

    // KillCountComponent�� �����ϱ� ���� Lookup
    public ComponentLookup<UICountComponent> uiCountGroup;
    public Entity uiCountEntity; // ī��Ʈ�� ����� ��ƼƼ

    public EntityCommandBuffer ECB;

    public void Execute(TriggerEvent te)
    {
        bool aMonster = MonsterGroup.HasComponent(te.EntityA);
        bool bMonster = MonsterGroup.HasComponent(te.EntityB);

        bool aPlayer = ECSPlayer.HasComponent(te.EntityA);
        bool bPlayer = ECSPlayer.HasComponent(te.EntityB);

        if (( aMonster && bPlayer ) || ( aPlayer && bMonster ))
        {
            // ���� ��ƼƼ�� ã�Ƽ� ó��
            Entity monsterEntity = aMonster ? te.EntityA : te.EntityB;

            // ���͸� ���ϴ� (Disable)
            ECB.AddComponent<Disabled>(monsterEntity);

            //ųī��Ʈ ����
            var killData = uiCountGroup[uiCountEntity];
            killData.KillCount++;
            uiCountGroup[uiCountEntity] = killData;
        }
    }
}

// �ݸ��� �̺�Ʈ
//[BurstCompile]
//struct CollisionJob : ICollisionEventsJob
//{
//    [ReadOnly]
//    public ComponentLookup<MonsterData> MonsterGroup;

//    public EntityCommandBuffer ECB;

//    public void Execute(CollisionEvent ce)
//    {
//        Debug.Log(1);
//        bool a = MonsterGroup.HasComponent(ce.EntityA);
//        bool b = MonsterGroup.HasComponent(ce.EntityB);

//        if (a && b)
//        {
//            ECB.SetComponentEnabled<MonsterData>(ce.EntityA, false);
//        }
//    }
//}
