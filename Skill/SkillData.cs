using UnityEngine;

// 스킬 타입 정의
public enum SkillType
{
    Damage, Heal, Buff
}

[CreateAssetMenu]
public class SkillData : ScriptableObject
{
    public SkillEventChannelSO eventChannel; // 스킬 효과를 화면에 표시하기 위한 이벤트 채널

    [Header("Basic Info")]
    public string skillName;    // 스킬 이름
    public SkillType skillType; // 스킬 타입 (데미지, 힐, 버프)
    public float cost;          // 스킬 사용에 필요한 SP
    public float cooldown;      // 스킬 쿨타임

    [Header("Values")]
    public float power;         // 스킬 효과의 강도 (데미지 양, 회복 양, 버프 수치 등)
    public float duration;      // 버프 지속 시간 (버프 스킬에만 적용)

    [Header("Effect Prefab")]
    public GameObject effectPrefab; // 스킬 효과 프리팹

    // 스킬 활성화 메서드, player 객체를 받아서 스킬 효과 적용
    public virtual void Activate(Player_Skill player)
    {
        Debug.Log($"{skillName} Activate");

        switch (skillType)
        {
            case SkillType.Damage:
                eventChannel.RaiseEvent($"{skillName} Activate\n{skillType} {power}");
                break;

            case SkillType.Heal:
                player.playerStatus.HPRecovery(power);
                eventChannel.RaiseEvent($"{skillName} Activate\n{skillType} {power}");
                break;

            case SkillType.Buff:
                eventChannel.RaiseEvent($"{skillName} Activate\n{skillType} {power}");
                break;
        }
    }
}
