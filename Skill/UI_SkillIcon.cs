using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillIcon : MonoBehaviour
{
    public Image cooldownMask;              // 스킬 쿨타임시 나타날 이미지
    public TextMeshProUGUI skillnametext;   // 스킬 이름

    private Skill skill; // 스킬 정보

    // 스킬 정보 입력
    public void Setup(Skill _skill)
    {
        cooldownMask.fillAmount = 0;

        // 스킬이 null인 경우 "None"으로 표시하고 종료
        if (_skill == null)
        {
            skillnametext.text = "None";
            return;
        }

        skill = _skill;
        skillnametext.text = _skill.skilldata.skillName;
    }

    private void Update()
    {
        if (skill == null)
        {
            return;
        }

        float remain = skill.GetCooldownRemaining();    //스킬사용 후 몇초 지났는지 확인
        float total = skill.skilldata.cooldown;         //쿨타임 시간

        // 쿨타임이 남아있다면 mask 이미지의 fillAmount를 남은 시간 비율로 설정, 그렇지 않으면 0으로 설정
        if (remain > 0)
        {
            cooldownMask.fillAmount = remain / total;
        }
        else
        {
            cooldownMask.fillAmount = 0;
        }
    }
}
