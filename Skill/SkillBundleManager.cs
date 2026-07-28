using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SkillBundleManager : MonoBehaviour
{
    public static SkillBundleManager Instance { get; private set; }
    // 불러온 스킬들을 저장할 딕셔너리
    private Dictionary<string, SkillData> _skillDatabase = new Dictionary<string, SkillData>();
    
    public bool IsLoaded { get; private set; } = false;
    public event System.Action OnSkillLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllSkillsByLabel("Skill");            
        }
        else
        {
            Destroy(gameObject);
        }
    }

public void LoadAllSkillsByLabel(string labelName)
    {
        Addressables.LoadAssetsAsync<SkillData>(labelName, skillData =>
        {
            if (skillData != null && !_skillDatabase.ContainsKey(skillData.name))
            {
                Debug.Log(skillData.skillName);
                _skillDatabase.Add(skillData.skillName, skillData);
            }
        }).Completed += handle =>
        {
            // 💡 2. 로드가 전부 완료되면 플래그를 변경하고 이벤트를 실행!
            IsLoaded = true;
            OnSkillLoaded?.Invoke(); 
        };
    }

    // 이름으로 스킬 데이터 가져오기
    public SkillData GetSkill(string skillName)
    {
        if (_skillDatabase.TryGetValue(skillName, out SkillData skill))
        {
            return skill;
        }

        Debug.LogWarning($"스킬을 찾을 수 없습니다: {skillName}");
        return null;
    }
}