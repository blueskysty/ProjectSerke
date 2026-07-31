using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 프리팹을 한 곳에서 관리하고, 라벨 또는 이름(ID)로 조회할 수 있게 해주는 매니저 클래스.
/// 싱글톤 패턴을 사용한다.
/// </summary>
public class PrefabManager : MonoBehaviour
{
    /// <summary>
    /// 싱글톤 인스턴스. 중복 생성 방지.
    /// </summary>
    public static PrefabManager Instance { get; private set; }

    /// <summary>
    /// ID(프리팹 이름)으로 매핑된 GameObject 프리팹 데이터베이스.
    /// </summary>
    private readonly Dictionary<string, GameObject> _prefabDatabase = new();

    /// <summary>
    /// 프리팹이 모두 로드됐는지 여부.
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// 프리팹이 모두 로드되었을 때 호출되는 이벤트.
    /// </summary>
    public event Action OnPrefabsLoaded;

    /// <summary>
    /// 싱글톤 신설 및 Charactor 라벨 프리팹 초기 로드.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadPrefabsByLabel("Charactor");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지정한 Addressable 라벨을 가진 모든 프리팹을 비동기로 일괄 로드.
    /// </summary>
    /// <param name="labelName">Addressable 라벨명 (예: "Player", "Monster")</param>
    public void LoadPrefabsByLabel(string labelName)
    {
        // Addressables를 통해 labelName에 해당하는 모든 GameObject 프리팹을 로드
        Addressables.LoadAssetsAsync<GameObject>(labelName, prefab =>
        {
            if (prefab != null && !_prefabDatabase.ContainsKey(prefab.name))
            {
                _prefabDatabase.Add(prefab.name, prefab);
            }
        }).Completed += handle =>
        {
            // 로드 작업 완료 시 플래그 및 이벤트 호출
            IsLoaded = true;
            OnPrefabsLoaded?.Invoke();
        };
    }

    /// <summary>
    /// 프리팹의 이름(ID)으로 미리 로드된 프리팹을 반환한다.
    /// </summary>
    /// <param name="prefabId">프리팹의 이름(ID)</param>
    /// <returns>찾은 경우 원본 프리팹 GameObject, 못 찾으면 null</returns>
    public GameObject GetPrefab(string prefabId)
    {
        if (_prefabDatabase.TryGetValue(prefabId, out var prefab))
        {
            return prefab;
        }

        Debug.LogError($"[PrefabManager] Prefab Not Found: {prefabId}");
        return null;
    }
}