using UnityEngine;

public class SkillManager: MonoBehaviour
{
    [SerializeField]    private SkillBundleManager bundleManager;
    public SkillEventChannelSO eventChannel;    // 스킬 효과를 화면에 표시하기 위한 이벤트 채널
    
    public SkillData[] skillDatas;      // 캐릭터 스킬데이터
    public UI_SkillIcon[] ui_skillicon; // 스킬 아이콘
    private Skill[] skills;             // 스킬

    string[] skillArr = new string[] {"Damage1", "Damage2", "Heal Skill","Buff SP Rev"};

    void Awake()
    {
        skills = new Skill[ui_skillicon.Length];
    }

    private void Start()
    {
        SkillBundleManager.Instance.OnSkillLoaded += SetupSkills;
    }

    public void SetupSkills()
    {
        for (int i = 0; i < ui_skillicon.Length; i++)
        {
            // 1. 등록할 스킬 이름 목록(skillArr)의 범위를 벗어나거나 이름이 없으면 슬롯 비우기
            if (i >= skillArr.Length || string.IsNullOrEmpty(skillArr[i]))
            {
                skills[i] = null;
                ui_skillicon[i].Setup(null);
                continue;
            }

            // 2. 에셋번들에서 스킬 데이터(SkillData) 가져오기
            SkillData data = SkillBundleManager.Instance.GetSkill(skillArr[i]);

            if (data != null)
            {
                // 3. 가져온 SkillData로 Skill 객체 생성 후 UI 연결
                skills[i] = new Skill(data, this);
                ui_skillicon[i].Setup(skills[i]);
            }
            else
            {
                // 에셋번들에서 스킬 데이터를 찾지 못했을 경우
                Debug.LogWarning($"[{skillArr[i]}] 스킬 데이터를 에셋번들에서 찾을 수 없습니다.");
                skills[i] = null;
                ui_skillicon[i].Setup(null);
            }
        }
    }

    public SkillData UseSkillFromBundle(string skillName)
    {
        return bundleManager.GetSkill(skillName);
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