using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;        
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct CollisionSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var job = new TriggerJob
        {
            MonsterGroup = SystemAPI.GetComponentLookup<MonsterData>(true),
            ECSPlayer = SystemAPI.GetComponentLookup<ECSPlayerData>(true),
            ECB = ecb
        };

        state.Dependency = job.Schedule(simulation, state.Dependency);
    }
}

// 트리거 이벤트
[BurstCompile]
struct TriggerJob: ITriggerEventsJob
{
    [ReadOnly]
    public ComponentLookup<MonsterData> MonsterGroup;
    [ReadOnly]
    public ComponentLookup<ECSPlayerData> ECSPlayer;

    public EntityCommandBuffer ECB;

    public void Execute(TriggerEvent te)
    {
        bool aMonster = MonsterGroup.HasComponent(te.EntityA);
        bool bMonster = MonsterGroup.HasComponent(te.EntityB);

        bool aPlayer = ECSPlayer.HasComponent(te.EntityA);
        bool bPlayer = ECSPlayer.HasComponent(te.EntityB);

        if (( aMonster && bPlayer ) || ( aPlayer && bMonster ))
        {
            // 몬스터 엔티티를 찾아서 처리
            Entity monsterEntity = aMonster ? te.EntityA : te.EntityB;

            // 몬스터를 끕니다 (Disable)
            ECB.AddComponent<Disabled>(monsterEntity);

            // 완전 삭제
            // ECB.DestroyEntity(monsterEntity);
        }
    }
}

// 콜리전 이벤트
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
