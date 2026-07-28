using UnityEngine;

// 스킬 타입 정의
public enum SkillType
{
    Damage, Heal, Buff
}

public abstract class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public float cost;
    public float cooldown;
    public GameObject effectPrefab;
    public SkillEventChannelSO eventChannel;

    // 공통 실행 흐름
    public void Activate(Player_Skill player)
    {
        ExecuteEffect(player); // 자식이 구현한 고유 로직 실행
    }

    // 자식들이 알아서 구현할 효과 메서드
    protected abstract void ExecuteEffect(Player_Skill player);
}

[CreateAssetMenu(fileName = "New Damage Skill", menuName = "Skills/Damage Skill")]
public class DamageSkillData : SkillData
{
    [Header("Damage Settings")]
    public float damagePower;

    protected override void ExecuteEffect(Player_Skill player)
    {
        // 타겟에 데미지 전달 로직
        eventChannel?.RaiseEvent($"{skillName} Activate\nDamage {damagePower}");
    }
}

[CreateAssetMenu(fileName = "New Heal Skill", menuName = "Skills/Heal Skill")]
public class HealSkillData : SkillData
{
    [Header("Heal Settings")]
    public float healPower;

    protected override void ExecuteEffect(Player_Skill player)
    {
        // 힐 적용 로직
        player.playerStatus.HPRecovery(healPower);
        eventChannel?.RaiseEvent($"{skillName} Activate\nHeal {healPower}");
    }
}

[CreateAssetMenu(fileName = "New Buff Skill", menuName = "Skills/Buff Skill")]
public class BuffSkillData : SkillData
{
    [Header("Buff Settings")]
    public float buffPower;
    public float duration; // 버프 전용 변수

    protected override void ExecuteEffect(Player_Skill player)
    {
        // 버프 및 지속시간 적용 로직
        player.SPBuff(buffPower,duration);
        eventChannel?.RaiseEvent($"{skillName} Activate\nBuff {buffPower} (Duration: {duration}s)");
    }
}