using UnityEngine;

/// <summary>
/// 최대 체력 비율 기반 즉시 회복 스킬
/// </summary>
[CreateAssetMenu(menuName = "Skills/Heal")]
public class Skill_Heal : SkillBase
{
    [Tooltip("레벨 1 기준 최대 체력 회복 비율 (0.2 = 20%)")]
    public float baseHealRatio = 0.2f;

    [Tooltip("레벨당 회복 비율 증가량")]
    public float healRatioPerLevel = 0.05f;

    public override void Execute(Player player, int skillLevel)
    {

        // 이미 풀피면 굳이 실행 안 해도 됨
        if (player.currentHp >= player.finalStats.maxHp)
            return;

        float healRatio = GetHealRatio(skillLevel);
        float healAmount = player.finalStats.maxHp * healRatio;

        // 이펙트 추가
        EffectManager.Instance.PlayEffect(
            Enums.EffectType.Skill_Heal,
            player.transform.position,
            Quaternion.identity,
            player.transform
            );

        // 사운드 추가
        SoundManager.Instance.PlaySFX("heal");

        player.Heal(healAmount);
    }

    private float GetHealRatio(int level)
    {
        return baseHealRatio + healRatioPerLevel * (level - 1);
    }
}
