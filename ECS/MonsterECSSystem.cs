using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;
using static UnityEngine.Rendering.STP;

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

    // X/Z �ݰ� ���� ������ ���� ��ġ(float3)�� ��ȯ�մϴ�.
    public static float3 CreateRandomSpawnPosition(this ref Unity.Mathematics.Random self, float radiusX, float radiusZ)
    {
        // 1. ���� ������� ���� �� ������ 1�� ���� ��ǥ ����
        var diskPoint = self.NextOnDisk();
        float3 candidatePos = new float3(diskPoint.x * radiusX, 0f, diskPoint.z * radiusZ);

        // 2. �ش� ��ġ�� NavMesh ������ Ȯ�� �� ����
        // (NavMesh API�� UnityEngine.Vector3�� ����ϹǷ� ����ȯ �ʿ�)
        if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 5, NavMesh.AllAreas))
        {
            // ���� ����� NavMesh ��ǥ ��ȯ
            return hit.position;
        }

        // 3. ���� �ֺ� maxNavMeshDistance ���� ���� NavMesh�� �ƿ� ���ٸ� 
        //    ����å���� 1�� ������ ���� ��ǥ ��ȯ (�Ǵ� �ʿ信 ���� ó��)
        return candidatePos;
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))] 
public partial struct MonsterSpawnSystem: ISystem
{
    private double timer; // ���� ��ȯ �ð��� ����

    public void OnCreate(ref SystemState state)
    {
        // Config �̱����� ������ ���� �ý����� �������� ����
        state.RequireForUpdate<Config>();
        timer = 0.0;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        return;
        
        var config = SystemAPI.GetSingleton<Config>();

        // 1. ���� ���忡 �����ϴ� ������ �� ������ �����ɴϴ�. (Query ���)
        int currentMonsterCount = SystemAPI.QueryBuilder().WithAll<MonsterData>().WithOptions(EntityQueryOptions.IncludeDisabledEntities).Build().CalculateEntityCount();

        // 2. ���� �̹� �ִ�ġ(MaxCount)�� �����ߴٸ� �� �̻� ��ȯ���� �ʰ� �����մϴ�.
        if (currentMonsterCount >= config.SpawncountMax)
        {
            return;
        }

        // 3. Ÿ�̸� ���� (�� ������ ��� �ð� �ջ�)
        timer += SystemAPI.Time.DeltaTime;
        

        // 4. 1�ʰ� ������ �� ��ȯ ���� ����
        if (timer >= 1.0)
        {
            timer -= 1.0; // ������ Ÿ�̸� ������ ���� 1�� ����

            // [�ٽ� ������ġ] �̹� ƽ�� �ִ� �� �������� ���� �Ǵ��� ��Ȯ�� ���
            // ��: ���� 950�����̰� �ִ밡 1000������, 100������ �䱸�ص� �� 50������ �����ǵ��� ����
            int allowedSpawnCount = config.SpawncountMax - currentMonsterCount;
            int spawnCountThisTick = math.min(config.Spawncount, allowedSpawnCount);

            // ���� �� �̻� ������ ������ ���ٸ� ���⼭ ��ŵ
            if (spawnCountThisTick <= 0)
            {
                return;
            }

            // UI ī��Ʈ ����
            if (SystemAPI.TryGetSingletonEntity<UICountComponent>(out var uiCountEntity))
            {
                var uiCountRW = SystemAPI.GetComponentRW<UICountComponent>(uiCountEntity);
                uiCountRW.ValueRW.MonsterCount += spawnCountThisTick;
            }

            // 5. ��ƼƼ �ϰ� ����
            var instances = state.EntityManager.Instantiate(config.Prefab, spawnCountThisTick, Allocator.Temp);

            // �Ź� �ٸ� �������� �������� �ð� ���� ������ �õ� �ʱ�ȭ
            var rand = new Unity.Mathematics.Random((uint)( SystemAPI.Time.ElapsedTime * 1000 ) + config.RandomSeed);

            Entity playerEntity = Entity.Null;
            if (SystemAPI.TryGetSingletonEntity<ECSPlayerData>(out var foundPlayer))
            {
                playerEntity = foundPlayer;
            }


            foreach (var entity in instances)
            {
                var xform = SystemAPI.GetComponentRW<LocalTransform>(entity);
                var monster = SystemAPI.GetComponentRW<MonsterData>(entity);

                // NextOnDisk()�� 2���� ���� ���� ������ ��(float2)�� ��ȯ�մϴ�. (x, y ��ǥ ����)
                // x���� SpawnRadiusX, z(�Ǵ� y)���� SpawnRadiusY�� ���� �����ݴϴ�.
                var spawnPos = rand.CreateRandomSpawnPosition( config.SpawnRadiusX, config.SpawnRadiusZ);
               
                xform.ValueRW = LocalTransform.FromPositionRotation(spawnPos, rand.NextYRotation());
                monster.ValueRW = MonsterData.Random(rand.NextUInt());

                // ����ִ� Ÿ�� ��ƼƼ ���� ����
                if (playerEntity != Entity.Null && SystemAPI.HasComponent<NavAgentComponent>(entity))
                {
                    var navAgent = SystemAPI.GetComponentRW<NavAgentComponent>(entity);
                    navAgent.ValueRW.targetEntity = playerEntity;
                }
            }
        }
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

    // �����ϸ鼭 ȸ���ϴ� ������ �����մϴ�.
    public void Execute(in MonsterData monsterData, ref LocalTransform xform)
    {
        var rot = quaternion.RotateY(monsterData.Speed * Elapsed);
        var fwd = xform.Forward();

        //xform.Position += fwd * monsterData.Speed * Elapsed;
        //xform.Rotation = math.mul(xform.Rotation, rot);
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MonsterReviveSystem: ISystem
{
    private double _nextReviveTime; // ���� ��Ȱ �ð��� ����

    public void OnCreate(ref SystemState state)
    {
        // Config ���� �̱����� ������ ������ ��ٸ����� ������ ����
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // ���� ���� �ð��� ������ ��Ȱ �ð����� ������ ���
        if (SystemAPI.Time.ElapsedTime < _nextReviveTime)
            return;

        // 5�� �� �ð� ����
        _nextReviveTime = SystemAPI.Time.ElapsedTime + 5.0;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // �Ź� �ٸ� �������� �������� �ð� ���� ������ �õ� �ʱ�ȭ
        var config = SystemAPI.GetSingleton<Config>();
        var rand = new Unity.Mathematics.Random((uint)( SystemAPI.Time.ElapsedTime * 1000 ) + config.RandomSeed);

        foreach (var (monster, agent, entity) in SystemAPI.Query<RefRO<MonsterData>, RefRW<NavAgentComponent>>()
                             .WithAll<Disabled>()
                             .WithEntityAccess())
        {
            // 1. Disabled �±� �����Ͽ� Ȱ��ȭ
            ecb.RemoveComponent<Disabled>(entity);

            // 2. ������ ���� ��ġ ���� �� ��ġ �̵�
            float3 spawnPos = rand.CreateRandomSpawnPosition(config.SpawnRadiusX, config.SpawnRadiusZ);
            ecb.SetComponent(entity, LocalTransform.FromPosition(spawnPos));

            // 3. ���� ��������Ʈ ���� ��ü ����
            ecb.SetBuffer<WaypointBuffer>(entity); // ���۸� �缳���Ͽ� ���� ������ �ʱ�ȭ

            // 4. NavAgent ���� �ʱ�ȭ (��� ���� ã���� 0f ����)
            var agentData = agent.ValueRO;
            agentData.nextPathCalculateTime = 0f;
            agentData.pathCalculated = false;
            ecb.SetComponent(entity, agentData);
        }
    }
}