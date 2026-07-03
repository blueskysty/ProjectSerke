using UnityEngine;
using Unity.Entities;

public struct ECSPlayerData : IComponentData
{
    public float Speed;

    public static ECSPlayerData Random(uint seed)
    {
        return new ECSPlayerData
        {
            Speed = seed
        };
    }
}

public class ECSPlayer: MonoBehaviour
{
    public float speed = 10f;
}

class ECSPlayerBaker: Baker<ECSPlayer>
{
    public override void Bake(ECSPlayer authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ECSPlayerData
        {
            Speed = authoring.speed
        });
    }
}