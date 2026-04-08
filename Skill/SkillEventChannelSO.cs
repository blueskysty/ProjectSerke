using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Skill Event Channel")]
public class SkillEventChannelSO: ScriptableObject
{
    // 모든 스킬이 공통으로 사용할 전광판
    public event Action<string> OnSkillUsed;

    public void RaiseEvent(string description)
    {
        OnSkillUsed?.Invoke(description);
    }
}
