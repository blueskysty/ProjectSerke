using Unity.Entities;
using UnityEngine;

public class NavAgentAuthoring : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float moveSpeed;

    public NavAgentAuthoring(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    private class AuthoringBaker : Baker<NavAgentAuthoring>
    {
        public override void Bake(NavAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new NavAgentComponent
            {                
                targetEntity = GetEntity(authoring.targetTransform, TransformUsageFlags.Dynamic),
                moveSpeed = authoring.moveSpeed
            });
            AddBuffer<WaypointBuffer>(entity);
        }
    }
}
