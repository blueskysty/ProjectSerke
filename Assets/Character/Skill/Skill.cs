using UnityEngine;

public class Skill
{
    public SkillData skilldata;
    private float lastUseTime;

    public Skill(SkillData data)
    {
        skilldata = data;
        lastUseTime = -999;
    }

    public bool IsReady()
    {
        return Time.time >= lastUseTime + skilldata.cooldown;
    }

    public void TryUse(GameObject player)
    {
        if (!IsReady())
        {
            Debug.Log($"{skilldata.skillName} 쿨타임 남음");
            return;
        }

        skilldata.Activate(player);
        lastUseTime = Time.time;
    }

    public float GetCooldownRemaining()
    {
        float end = lastUseTime + skilldata.cooldown;
        return Mathf.Max(0, end - Time.time);
    }
}
