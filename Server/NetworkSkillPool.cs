using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// 네트워크 오브젝트(스킬 등)의 생성/파괴 부하를 줄이기 위한 풀링 시스템입니다.
// INetworkPrefabInstanceHandler를 상속받아 유니티 NGO의 스폰 시스템과 연동됩니다.
public class NetworkSkillPool: NetworkBehaviour, INetworkPrefabInstanceHandler
{
    [SerializeField] private GameObject skillPrefab;    // 풀링할 스킬 프리팹
    [SerializeField] private int poolSize = 10;         // 초기 풀 생성 개수
    private Stack<NetworkObject> poolStack = new Stack<NetworkObject>();   

    public override void OnNetworkSpawn()
    {
        RegisterHandler();
    }

    // NGO 프리팹 핸들러에 이 클래스를 등록하여, 특정 프리팹의 스폰/해제 권한을 위임받습니다.
    private void RegisterHandler()
    {
        if (NetworkManager.Singleton != null && skillPrefab != null)
        {
            var handler = NetworkManager.Singleton.PrefabHandler;

            // 중복 등록 방지를 위해 기존 핸들러 제거 후 새로 등록
            handler.RemoveHandler(skillPrefab);
            handler.AddHandler(skillPrefab, this);

            // 이미 풀이 채워져 있다면 중단
            if (poolStack.Count > 0)
            {
                return;
            }

            // 설정된 크기만큼 초기 인스턴스 미리 생성 (Pre-warm)
            for (int i = 0; i < poolSize; i++)
            {
                poolStack.Push(CreateNewInstance());
            }
        }
    }

    // 새로운 스킬 인스턴스를 생성하고 초기 설정을 수행합니다.
    private NetworkObject CreateNewInstance()
    {
        GameObject go = Instantiate(skillPrefab);
        go.SetActive(false);

        // 서버 스킬 컴포넌트에 풀 참조 전달
        go.GetComponent<ServerSkill>().Setup_Network(this);
        NetworkObject no = go.GetComponent<NetworkObject>();

        return no;
    }

    // [INetworkPrefabInstanceHandler 인터페이스]
    // 서버에서 NetworkObject.Spawn() 호출 시, 실제로 오브젝트를 생성하는 대신 풀에서 꺼내 반환합니다.
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = poolStack.Count > 0 ? poolStack.Pop() : CreateNewInstance();

        netObj.transform.position = position;
        netObj.transform.rotation = rotation;
        netObj.gameObject.SetActive(true);
        return netObj;
    }

    // [INetworkPrefabInstanceHandler 인터페이스]
    // NetworkObject.Despawn() 호출 시, 오브젝트를 실제로 파괴하지 않고 다시 풀에 보관합니다.
    public void Destroy(NetworkObject networkObject)
    {
        networkObject.gameObject.SetActive(false);
        poolStack.Push(networkObject);
    }

    public override void OnDestroy()
    {
        // 오브젝트 파괴 시 핸들러 등록 해제 (메모리 누수 방지)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.PrefabHandler != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(skillPrefab);
        }
        base.OnDestroy();
    }

    // 오브젝트를 비활성화하고 풀 스택에 반환합니다. 중복 반환 방지 로직이 포함되어 있습니다.
    public void ReturnToPool(NetworkObject networkObject)
    {
        // 이미 풀에 포함되어 있는지 확인하여 예외 상황 방지
        if (poolStack.Contains(networkObject))
        {
            return;
        }

        networkObject.gameObject.SetActive(false);
        poolStack.Push(networkObject);
    }
}