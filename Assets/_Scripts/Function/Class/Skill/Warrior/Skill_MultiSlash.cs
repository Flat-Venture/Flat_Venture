using System.Collections;
using UnityEngine;
using static Enums;

/// <summary>
/// 전방향 기본 공격범위 * 2 
/// 연속 3번 공격
/// </summary>
[CreateAssetMenu(menuName = "Skills/MultiSlash")]
public class Skill_MultiSlash : SkillBase
{
    [Header("Damage")]
    [Tooltip("레벨 1 기준 스킬 계수 (공격력에 곱해짐)")]
    public float baseDamageMultiplier = 1.5f;

    [Tooltip("레벨당 증가하는 계수")]
    public float damageMultiplierPerLevel = 0.2f;

    [Header("Attack Settings")]
    public float range = 4f;
    public float hitInterval = 0.4f;
    public int baseHitCount = 3;

    public LayerMask monsterLayer;

    public override void Execute(Player player, int skillLevel)
    {
        player.StartCoroutine(DoSlash(player, skillLevel));
    }

    private IEnumerator DoSlash(Player player, int skillLevel)
    {
        int hitCount = baseHitCount + (skillLevel - 1);
        float damageMultiplier = GetDamageMultiplier(skillLevel);

        for (int i = 0; i < hitCount; i++)
        {
            //이펙트 추가
            EffectManager.Instance.PlayEffect(
                EffectType.Skill_MultiSlash,
                Player.Instance.gameObject.transform.position,
                Quaternion.identity,
                null);
            EffectManager.Instance.PlayEffect(
                EffectType.Skill_MultiSlash,
                Player.Instance.gameObject.transform.position,
                Quaternion.Euler(0f, 180f, 0f),
                null);

            //애니메이션 추가
            player.animCtrl.ChangeState(PlayerAnimState.Attack);

            //사운드 추가
            SoundManager.Instance.PlaySFX("multiSlash");

            Collider[] monsters = Physics.OverlapSphere(
                player.transform.position,
                range,
                monsterLayer
            );

            foreach (var c in monsters)
            {
                MonsterBase m = c.GetComponent<MonsterBase>();
                if (m == null) continue;

                // ⭐ 타격 순간의 최종 공격력 기준
                float damage =
                    player.finalStats.attackDamage * damageMultiplier;

                m.TakeDamage(damage);
            }

            yield return new WaitForSeconds(hitInterval);
        }
    }

    private float GetDamageMultiplier(int level)
    {
        return baseDamageMultiplier
             + damageMultiplierPerLevel * (level - 1);
    }
}
