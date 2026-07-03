using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Config: IComponentData
{
    public Entity Prefab;
    public float SpawnRadius;
    public int Spawncount;
    public uint RandomSeed;
}


public class ConfigAuthoring: MonoBehaviour
{
    public GameObject Prefab = null;
    public float SpawnRadius = 10f;
    public int Spawncount = 10;
    public uint RandomSeed = 10;

    class Baker: Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var data = new Config
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnRadius = authoring.SpawnRadius,
                Spawncount = authoring.Spawncount,
                RandomSeed = authoring.RandomSeed
            };

            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }
    }
}
