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
    public int RandomSeed = 10;

    private void Awake()
    {
        RandomSeed = UnityEngine.Random.Range(0, 100);
    }

    class Baker: Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var data = new Config
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnRadius = authoring.SpawnRadius,
                Spawncount = authoring.Spawncount,
                RandomSeed = (uint)UnityEngine.Random.Range(0, 100)
            };

            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }
    }
}
