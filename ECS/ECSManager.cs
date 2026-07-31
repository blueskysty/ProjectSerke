using Unity.Entities;
using UnityEngine;

// UI 카운트 데이터를 저장하는 컴포넌트 구조체
public struct UICountComponent: IComponentData
{
    public int KillCount;     // 플레이어가 처치한 몬스터 수
    public int MonsterCount;  // 현재 존재하는 몬스터 수
}

// ECS 월드가 시작될 때 UICountComponent를 가진 엔티티를 1개 생성
public class ECSManager : MonoBehaviour
{
    void Start()
    {
        // 디폴트 월드의 EntityManager를 가져옴
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // UICountComponent를 가지는 새 엔티티 생성
        var entity = entityManager.CreateEntity(typeof(UICountComponent));

        // 컴포넌트 초기값 설정: KillCount, MonsterCount를 0으로 초기화
        entityManager.SetComponentData(entity, new UICountComponent { KillCount = 0, MonsterCount = 0 });
    }
}
