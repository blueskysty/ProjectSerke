using UnityEngine;

public class SkillManager: MonoBehaviour
{
    public SkillEventChannelSO eventChannel;    // 스킬 효과를 화면에 표시하기 위한 이벤트 채널

    public SkillData[] skillDatas;      // 캐릭터 스킬데이터
    public UI_SkillIcon[] ui_skillicon; // 스킬 아이콘
    private Skill[] skills;             // 스킬

    void Awake()
    {
        // 스킬 아이콘 개수만큼 생성
        skills = new Skill[ui_skillicon.Length];

        for (int i = 0; i < ui_skillicon.Length; i++)
        {
            // skillDatas가 부족하면 skillicon 슬롯 비우기
            if (i >= skillDatas.Length || skillDatas[i] == null)
            {
                skills[i] = null;                  
                ui_skillicon[i].Setup(null);
                continue;
            }

            // Skill 생성, UI 슬롯과 연결
            skills[i] = new Skill(skillDatas[i], this);
            ui_skillicon[i].Setup(skills[i]);
        }
    }

    public void UseSkill(int index, Player_Skill player)
    {
        // 스킬 정보가 없다면 실행 안됨
        if (index < 0 || index >= skills.Length || skills[index] == null)
        {
            eventChannel.RaiseEvent($"Skill{index+1} None");
            return;
        }

        skills[index].TryUse(player);
    }
}