using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Moving,
    Attacking
}

public struct ECSPlayerData : IComponentData
{
    public float Speed;
    public PlayerState State;

    public static ECSPlayerData Random(uint seed)
    {
        return new ECSPlayerData
        {
            Speed = seed,
            State= PlayerState.Idle
        };
    }
}

public struct MoveInput : IComponentData
{
    public float3 Direction; 
    public bool IsJumpPressed;
}

public class  PlayerGameObjectPrefab : IComponentData
{
    public GameObject Object;
}

public class PlayerAnimatorReference: IComponentData
{
    public Animator Animator;
}

public class PlayerAuthoring: MonoBehaviour
{
    public GameObject Object;
    public float speed = 10f;

    public class PlayerGameObjectPrefabBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ECSPlayerData
            {
                Speed = authoring.speed
            });

            AddComponent(entity, new MoveInput
            {
                Direction = float3.zero,
                IsJumpPressed = false
            });

            AddComponent(entity, new NavMeshTracker
            {
                LastValidPosition = float3.zero,
                TimeOutOfNavmesh = 0f,
                CheckTimer = 0f,
                HasValidPosition = false
            });

            AddComponentObject(entity, new PlayerGameObjectPrefab
            {
                Object = authoring.Object
            });
        }
    }
}