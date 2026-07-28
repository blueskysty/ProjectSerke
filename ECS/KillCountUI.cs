using Unity.Entities;
using UnityEngine;
using TMPro;

public class KillCountUI: MonoBehaviour
{
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI monsterCountText;
    private EntityManager entityManager;

    private float lastUpdateTime;
    public float updateInterval = 0.2f; // 0.2초 주기

    void Start() => entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    void Update()
    {
        // 최적화: 0.2초마다 갱신
        if (Time.time - lastUpdateTime > updateInterval)
        {
            var killCount = entityManager.CreateEntityQuery(typeof(UICountComponent)).GetSingleton<UICountComponent>().KillCount;
            var monsterCount = entityManager.CreateEntityQuery(typeof(UICountComponent)).GetSingleton<UICountComponent>().MonsterCount;
            killCountText.text = $"Kills: {killCount}";
            monsterCountText.text = $"Monsters: {monsterCount}";
            lastUpdateTime = Time.time;
        }
    }
}