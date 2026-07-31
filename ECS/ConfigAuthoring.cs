using Unity.Entities;
using UnityEngine;

public struct Config: IComponentData
{
    public Entity Prefab;
    public float SpawnRadiusX;
    public float SpawnRadiusZ;
    public int SpawncountMax;
    public int Spawncount;
    public uint RandomSeed;
}


public class ConfigAuthoring: MonoBehaviour
{
    public GameObject Prefab = null;
    public float SpawnRadiusX = 78f;
    public float SpawnRadiusZ = 48f;
    public int SpawncountMax = 1000;
    public int Spawncount = 150;
    public uint RandomSeed = 10;

    class Baker: Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var data = new Config
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnRadiusX = authoring.SpawnRadiusX,
                SpawnRadiusZ = authoring.SpawnRadiusZ,
                SpawncountMax = authoring.SpawncountMax,
                Spawncount = authoring.Spawncount,
                RandomSeed = authoring.RandomSeed
            };

            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }
    }
}
