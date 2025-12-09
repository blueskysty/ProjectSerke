using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillIcon : MonoBehaviour
{
    public Image cooldownMask;
    public TextMeshProUGUI skillnametext;

    private Skill skill; // ← runtime skill instance

    public void Setup(Skill _skill)
    {
        cooldownMask.fillAmount = 0;

        //스킬이 없을때
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

        float remain = skill.GetCooldownRemaining();
        float total = skill.skilldata.cooldown;

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
