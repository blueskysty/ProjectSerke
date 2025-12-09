using UnityEngine;

public enum SkillType
{
    Damage, Heal, Buff
}

[CreateAssetMenu]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public SkillType skillType;
    public float cost;
    public float cooldown;

    [Header("Values")]
    public float power;
    public float duration;

    [Header("Effect Prefab")]
    public GameObject effectPrefab;   // 이펙트는 Addressable로 바뀌어도 무방

    // 실제 동작 정의
    public virtual void Activate(Player_Skill player)
    {
        Debug.Log($"{skillName} Activate");

        switch (skillType)
        {
            case SkillType.Damage:
                Debug.Log($"Damage {power}");
                break;

            case SkillType.Heal:
                player.playerStatus.HPRecovery(power);
                Debug.Log($"Heal  {power}");
                break;

            case SkillType.Buff:
                Debug.Log($"Buff  {power}");
                break;
        }
    }
}
