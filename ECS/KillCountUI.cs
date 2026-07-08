using Unity.Entities;
using UnityEngine;
using TMPro;

public class KillCountUI: MonoBehaviour
{
    public TextMeshProUGUI killCountText;
    private EntityManager entityManager;

    private float lastUpdateTime;
    public float updateInterval = 0.2f; // 0.2초 주기

    void Start() => entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    void Update()
    {
        // 최적화: 0.2초마다 갱신
        if (Time.time - lastUpdateTime > updateInterval)
        {
            var count = entityManager.CreateEntityQuery(typeof(KillCountComponent)).GetSingleton<KillCountComponent>().Count;
            killCountText.text = $"Kills: {count}";
            lastUpdateTime = Time.time;
        }
    }
}