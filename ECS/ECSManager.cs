using Unity.Entities;
using UnityEngine;

public struct KillCountComponent: IComponentData
{
    public int Count;
}

public class ECSManager : MonoBehaviour
{
    void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 싱글톤으로 사용할 엔티티 생성
        var entity = entityManager.CreateEntity(typeof(KillCountComponent));

        // 초기값 설정
        entityManager.SetComponentData(entity, new KillCountComponent { Count = 0 });
    }
}
