using UnityEngine;

public class SkillManager: MonoBehaviour
{
    public SkillData[] skillDatas; // 인스펙터에서 드래그
    public UI_SkillIcon[] ui_skillicon; // 인스펙터에서 드래그
    private Skill[] skills;

    void Awake()
    {
        skills = new Skill[skillDatas.Length];

        for (int i = 0; i < skillDatas.Length; i++)
        {
            skills[i] = new Skill(skillDatas[i]);
            ui_skillicon[i].Setup(skills[i]);
        }
    }

    public void UseSkill(int index, GameObject player)
    {
        if (index < 0 || index >= skills.Length)
        {
            Debug.Log("스킬 없음");
            return;
        }

        skills[index].TryUse(player);
    }

    public float GetCooldownRemaining(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            return 0;
        }

        return skills[index].GetCooldownRemaining();
    }
}