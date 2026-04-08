using UnityEngine;
using TMPro;

public class UI_Status : MonoBehaviour
{
    public SkillEventChannelSO eventChannel;    // 스킬 효과를 화면에 표시하기 위한 이벤트 채널

    PlayerStatus status;    //player 스테이터스

    public TextMeshProUGUI uiHP;            //hp 스탯
    public TextMeshProUGUI uiSP;            //sp 스탯
    public TextMeshProUGUI uiSkillEffect;   //스킬 효과를 보여주기 위한 텍스트

    int lasthp = 0; // 마지막으로 표시된 hp 값, UI 업데이트 최적화 위해 사용
    int lastsp = 0; // 마지막으로 표시된 sp 값, UI 업데이트 최적화 위해 사용

    private void OnEnable()
    {
        // 누가 쏘든 중계소에 신호가 오면 UpdateUI 실행
        eventChannel.OnSkillUsed += UpdateSkillEffect;
    }

    private void OnDisable()
    {
        eventChannel.OnSkillUsed -= UpdateSkillEffect;
    }

    void UpdateSkillEffect(string message)
    {
        uiSkillEffect.text = message;
    }


    public void SetStatus(PlayerStatus _status)
    {
        status = _status;   //player 스테이터스 설정
        status.OnStatusChanged += UpdateUI; //player 스테이터스가 변경될 때마다 UI 업데이트
        UpdateUI(); //초기 UI 업데이트
    }

    void UpdateUI()
    {
        int hp = Mathf.CeilToInt(status.hpCurrent); // hpCurrent를 정수로 변환하여 UI에 표시
        int sp = Mathf.CeilToInt(status.spCurrent); // spCurrent를 정수로 변환하여 UI에 표시

        if (lasthp != hp)
        {
            uiHP.text = $"{hp} / {status.hpMax: 0}";    // hp가 변경되었을 때만 UI 업데이트
        }

        if (lastsp != sp)
        {
            uiSP.text = $"{sp} / {status.spMax: 0}";    // sp가 변경되었을 때만 UI 업데이트
        }
    }
}
