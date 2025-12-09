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

    public bool CostCheck(float cost)
    {
        return cost >= skilldata.cost;
    }

    public void TryUse(Player_Skill player)
    {
        // 쿨타임 남아있다면 실행안됨
        if (!IsReady())
        {
            Debug.Log($"{skilldata.skillName} 쿨타임 남음");
            return;
        }

        //코스트 부족하면 실행 안됨
        if (!CostCheck(player.playerStatus.spCurrent))
        {
            Debug.Log($"{skilldata.skillName} 코스트 부족");
            return;
        }

        player.SPConsume(skilldata.cost);   // 코스트 사용
        skilldata.Activate(player);         // 스킬 사용
        lastUseTime = Time.time;            // 쿨타임
    }

    public float GetCooldownRemaining()
    {
        // 남아 있는 쿨타임 시간
        float end = lastUseTime + skilldata.cooldown;
        return Mathf.Max(0, end - Time.time);
    }
}
