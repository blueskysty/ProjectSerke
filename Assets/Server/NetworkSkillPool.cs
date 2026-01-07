using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSkillPool: NetworkBehaviour, INetworkPrefabInstanceHandler
{
    [SerializeField] private GameObject skillPrefab;
    [SerializeField] private int poolSize = 10;
    private Stack<NetworkObject> poolStack = new Stack<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        RegisterHandler();
    }

    private void RegisterHandler()
    {
        if (NetworkManager.Singleton != null && skillPrefab != null)
        {
            var handler = NetworkManager.Singleton.PrefabHandler;

            handler.RemoveHandler(skillPrefab);

            handler.AddHandler(skillPrefab, this);

            Debug.Log($"<color=cyan>? [Pool]</color> {skillPrefab.name}");

            if (poolStack.Count > 0)
                return;


            for (int i = 0; i < poolSize; i++)
            {
                poolStack.Push(CreateNewInstance());
            }
        }
    }

    private NetworkObject CreateNewInstance()
    {
        GameObject go = Instantiate(skillPrefab);
        go.SetActive(false);
        NetworkObject no = go.GetComponent<NetworkObject>();

        return no;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = poolStack.Count > 0 ? poolStack.Pop() : CreateNewInstance();

        Debug.Log($"<color=yellow>[Pool]</color> {poolStack.Count})");

        netObj.transform.position = position;
        netObj.transform.rotation = rotation;
        netObj.gameObject.SetActive(true);
        return netObj;
    }

    public void Destroy(NetworkObject networkObject)
    {
        Debug.Log("<color=orange>[Pool]</color>");
        networkObject.gameObject.SetActive(false);
        poolStack.Push(networkObject);
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"<color=red>[Despawn]</color> {gameObject.name}");
        gameObject.SetActive(false);
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.PrefabHandler != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(skillPrefab);
        }
        base.OnDestroy();
    }
}