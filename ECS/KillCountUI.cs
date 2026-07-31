using Unity.Entities;
using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 처치 수(KillCount), 현재 몬스터 수(MonsterCount), FPS 정보를 UI에 표시하는 MonoBehaviour 클래스
/// </summary>
public class KillCountUI : MonoBehaviour
{
    
    public TextMeshProUGUI killCountText;       // 처치수 표시용 TextMeshProUGUI    
    public TextMeshProUGUI monsterCountText;    // 몬스터 수 표시용 TextMeshProUGUI    
    public TextMeshProUGUI fpsText;             // FPS 표시용 TextMeshProUGUI
    
    private EntityManager entityManager;        // ECS Entity 데이터 접근용 EntityManager
    
    private float accumulatedTime = 0f;         // FPS 계산용 누적 시간    
    private int frameCount = 0;                 // FPS 계산용 프레임 카운트    
    private float timeLeft = 0f;                // FPS/카운트 UI 업데이트 남은 시간    
    private float lastUpdateTime;               // 마지막 UI 갱신 시간    
    public float updateInterval = 0.2f;         // UI 데이터 갱신 간격(초), 기본값 0.2초

    /// <summary>
    /// 초기화 - EntityManager 할당
    /// </summary>
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    /// <summary>
    /// UI 데이터 업데이트
    /// </summary>
    void Update()
    {
        // KillCount/MonsterCount/FPS UI 갱신: updateInterval(기본 0.2초)마다 모두 함께 갱신
        timeLeft -= Time.unscaledDeltaTime;
        accumulatedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (timeLeft <= 0f)
        {
            // UICountComponent에서 킬 카운트, 몬스터 카운트 가져오기
            var uiCount = entityManager.CreateEntityQuery(typeof(UICountComponent)).GetSingleton<UICountComponent>();
            var killCount = uiCount.KillCount;
            var monsterCount = uiCount.MonsterCount;

            // FPS 계산
            float fps = frameCount / accumulatedTime;

            // 텍스트 갱신
            killCountText.SetText($"Kills: {killCount}");
            monsterCountText.SetText($"Monsters: {monsterCount}");
            fpsText.SetText("FPS: {0:F1}", fps);

            // FPS 수치에 따른 색상 변화 (50 이상=초록, 30~49=노랑, 30 미만=빨강)
            if (fps >= 50f)
                fpsText.color = Color.green;
            else if (fps >= 30f)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.red;

            // 누적값 초기화
            timeLeft = updateInterval;
            accumulatedTime = 0f;
            frameCount = 0;
        }
    }
}