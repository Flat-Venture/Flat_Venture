using UnityEngine;

/// <summary>
/// 레벨
/// 쿨타임
/// 스킬 사용
/// </summary>
[System.Serializable]
public class SkillRuntime
{
    public SkillBase skillBaseData;         //스킬 베이스 데이터
    public int currentLevel;                //스킬 현재 레벨

    private float lastUseTime;              //쿨타임용 마지막 실행 시간

    public SkillRuntime(SkillBase baseData)
    {
        skillBaseData = baseData;
        currentLevel = 1;
        lastUseTime = -999f;
    }

    public bool CanUse()
    {
        //쿨타임이 0 이하일 경우
        return GetCooldownRemaining() <= 0;
    }

    /// <summary>
    /// 스킬 사용
    /// </summary>
    /// <param name="player"></param>
    public void Use(Player player)
    {
        if (!CanUse()) return;              //쿨타임이거나, 상태이상일 때 사용 불가

        lastUseTime = Time.time;            //쿨타임 적용
        skillBaseData.Execute(player, currentLevel);
    }

    /// <summary>
    /// 스킬 레벨업
    /// </summary>
    public void LevelUp()
    {
        currentLevel++;
    }

    /// <summary>
    /// 스킬 쿨타임 적용 (쿨타임 감소 적용)
    /// cooldownReduction = 0.1f => 쿨타임 10% 감소
    /// 1 => 100% 감소
    /// </summary>
    /// <returns></returns>
    public float GetCooldownRemaining()
    {
        if (skillBaseData == null) return 0f;

        float baseCooldown = skillBaseData.cooldown;

        float cdr = Player.Instance.finalStats.cooldownReduction;
        cdr = Mathf.Clamp01(cdr);       //안전장치 0 ~ 1

        float finalCooldown = baseCooldown * (1f - cdr);

        float endTime = lastUseTime + finalCooldown;
        return Mathf.Max(0f, endTime - Time.time);
    }

    public float GetFinalCooldown()
    {
        float baseCooldown = skillBaseData.cooldown;
        float cdr = Player.Instance.finalStats.cooldownReduction;

        return baseCooldown * (1f - cdr);
    }

    public void ResetSkillLevel()
    {
        currentLevel = 1;
        lastUseTime = -999f;
    }
}
