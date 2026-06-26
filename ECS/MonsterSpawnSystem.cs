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

public partial struct MonsterRotation: ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MonsterRotationJob
        {
            Elapsed = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();

        Debug.Log("서브씬 몬스터회전");
    }
}

partial struct MonsterRotationJob: IJobEntity
{
    public float Elapsed;

    public void Execute(in MonsterData monsterData, ref LocalTransform xform)
    {
        var rot = quaternion.RotateY(monsterData.Speed * Elapsed);
        var fwd = xform.Forward();

        xform.Position += fwd * monsterData.Speed * Elapsed;
        xform.Rotation = math.mul(xform.Rotation, rot);
    }
}

//[UpdateInGroup(typeof(after))]
public partial struct BlowerSystem: ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new BlowerJob
        {
            Elapsed = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
        Debug.Log("서브씬 블로워");
    }
}