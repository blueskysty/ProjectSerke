using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SkillControl : MonoBehaviour
{
    [SerializeField]
    private GameObject objectPrefab;
    private Queue<GameObject> pool;

    [SerializeField]
    private int maxObjectInstanceCount= 10;

    public static SkillControl instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        pool = new Queue<GameObject>();

        for (int i = 0; i < maxObjectInstanceCount; i++)
        {
            //GameObject go = Instantiate(objectPrefab);
            //go.SetActive(false);
            //pool.Enqueue(go);
            //go.GetComponent<NetworkObject>().Spawn();
        }
    }

    private void Start()
    {
    }
}
