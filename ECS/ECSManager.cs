using Unity.Entities;
using UnityEngine;

public struct UICountComponent: IComponentData
{
    public int KillCount;
    public int MonsterCount;
}

public class ECSManager : MonoBehaviour
{
    void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 싱글톤으로 사용할 엔티티 생성
        var entity = entityManager.CreateEntity(typeof(UICountComponent));

        // 초기값 설정
        entityManager.SetComponentData(entity, new UICountComponent { KillCount = 0, MonsterCount = 0 });
    }
}
