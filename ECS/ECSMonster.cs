using UnityEngine;
using Unity.Entities;

public class ECSMonster: MonoBehaviour
{
    public float Health;
    public float Speed;
}

public struct MonsterData: IComponentData
{
    public float Health;
    public float Speed;

    public static MonsterData Random(uint seed)
    {
        var rand = new Unity.Mathematics.Random(seed);
        return new MonsterData
        {
            Health = rand.NextFloat(50, 150),
            Speed = rand.NextFloat(1, 10)
        };
    }
}

public class ECSMonsterBaker: MonoBehaviour
{
    public float Health = 100;
    public float Speed = 5;

    class baker: Baker<ECSMonster>
    {
        public override void Bake(ECSMonster authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MonsterData
            {
                Health = authoring.Health,
                Speed = authoring.Speed
            });
        }
    }
}