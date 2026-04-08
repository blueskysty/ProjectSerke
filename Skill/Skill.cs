using System.Diagnostics.Tracing;
using UnityEngine;

public class Skill
{
    SkillManager skillManager; // 스킬 매니저 참조(스킬 사용 불가 매세지 띄우기 위해)
    public SkillData skilldata;
    private float lastUseTime;  // 마지막으로 스킬을 사용한 시간

    //스킬 정보 입력
    public Skill(SkillData data, SkillManager _skillManager)
    {
        skilldata = data;
        skillManager = _skillManager;
        lastUseTime = -999;
    }

    //스킬 사용 가능 확인
    public bool IsReady()
    {
        return Time.time >= lastUseTime + skilldata.cooldown;
    }

    //스킬 사용하는데 sp가 충분한지 확인
    public bool CostCheck(float sp)
    {
        return sp >= skilldata.cost;
    }

    public void TryUse(Player_Skill player)
    {
        // 쿨타임 중이라면 스킬 사용 안됨
        if (!IsReady())
        {
            skillManager.eventChannel.RaiseEvent($"{skilldata.skillName} CoolDown");
            return;
        }

        //sp 부족하면 사용 안됨
        if (!CostCheck(player.playerStatus.spCurrent))
        {
            skillManager.eventChannel.RaiseEvent($"{skilldata.skillName} Low sp");
            return;
        }

        player.SPConsume(skilldata.cost);   // sp 소모
        skilldata.Activate(player);         // 스킬 사용
        lastUseTime = Time.time;            // 스킬 사용 시간
    }

    //스킬 쿨타임이 얼마나 남았는지 확인
    public float GetCooldownRemaining()
    {        
        float end = lastUseTime + skilldata.cooldown;   // 스킬 사용 시간에 쿨타임 시간 더하기
        return Mathf.Max(0, end - Time.time);           // 쿨타임 끝나는 시간에 현재 시간을 빼서 0과 비교하여 큰 쪽을 return함(0이면 스킬 사용가능 아닐경우 스킬 쿨타임중)
    }
}
