using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public static class MathExtensions
{
    public static float3 NextOnDisk(this ref Unity.Mathematics.Random self)
    {
        while (true)
        {
            var v = self.NextFloat2(-1, 1);
            if (math.length(v) <= 1)
                return math.float3(v.x, 0, v.y);
        }
    }

    public static quaternion NextYRotation(this ref Unity.Mathematics.Random self)
        => quaternion.RotateY(self.NextFloat(math.PI * 2));
}

[UpdateInGroup(typeof(InitializationSystemGroup))] 
public partial struct MonsterSpawnSystem: ISystem
{
    public void OnCreate(ref SystemState state)
        => state.RequireForUpdate<Config>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var instacnces = state.EntityManager.Instantiate(config.Prefab, config.Spawncount, Allocator.Temp);

        var rand = new Unity.Mathematics.Random(config.RandomSeed);
        foreach(var entity in instacnces)
        {
            var xform = SystemAPI.GetComponentRW<LocalTransform>(entity);
            var monster = SystemAPI.GetComponentRW<MonsterData>(entity);

            xform.ValueRW = LocalTransform.FromPositionRotation(rand.NextOnDisk() * config.SpawnRadius, rand.NextYRotation()); 
            monster.ValueRW = MonsterData.Random(rand.NextUInt());

        }
        state.Enabled = false;
    }
}
 
public partial struct MonsterMove: ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MonsterMoveJob
        {
            Elapsed = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}

partial struct MonsterMoveJob: IJobEntity
{
    public float Elapsed;

    // 전진하면서 회전하는 로직을 수행합니다.
    public void Execute(in MonsterData monsterData, ref LocalTransform xform)
    {
        var rot = quaternion.RotateY(monsterData.Speed * Elapsed);
        var fwd = xform.Forward();

        xform.Position += fwd * monsterData.Speed * Elapsed;
        xform.Rotation = math.mul(xform.Rotation, rot);
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MonsterReviveSystem: ISystem
{
    private double _nextReviveTime; // 다음 부활 시간을 저장

    public void OnUpdate(ref SystemState state)
    {
        // 현재 게임 시간이 설정된 부활 시간보다 작으면 대기
        if (SystemAPI.Time.ElapsedTime < _nextReviveTime)
            return;

        // 5초 뒤 시간 갱신
        _nextReviveTime = SystemAPI.Time.ElapsedTime + 5.0;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 'Disabled' 태그가 붙은 모든 몬스터를 찾습니다.
        // WithAll<Disabled>()는 Disabled 컴포넌트가 있는 엔티티만 골라냅니다.
        var query = SystemAPI.QueryBuilder().WithAll<MonsterData, Disabled>().Build();
        foreach (var entity in query.ToEntityArray(state.WorldUpdateAllocator))
        {
            // Disabled 태그를 제거하여 렌더링, 물리, 로직을 모두 활성화
            ecb.RemoveComponent<Disabled>(entity);

            // 부활 시 위치 초기화가 필요하다면 사용
            // ecb.SetComponent(entity, LocalTransform.FromPosition(0, 0, 0));
        }
    }
}