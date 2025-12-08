using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillIcon : MonoBehaviour
{
    public Image cooldownMask;
    public TextMeshProUGUI skillnametext;

    private Skill skill; // ก็ runtime skill instance

    public void Setup(Skill _skill)
    {
        skill = _skill;

        skillnametext.text = _skill.skilldata.skillName;
        cooldownMask.fillAmount = 0;
    }

    private void Update()
    {
        if (skill == null)
            return;

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
