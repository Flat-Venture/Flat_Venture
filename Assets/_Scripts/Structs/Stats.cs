using UnityEngine;

/// <summary>
/// 1. 스탯 기본 정보들
/// </summary>
[System.Serializable]
public class Stats
{
    public float maxHp;                 // 최대 체력
    public float maxMp;                 // 최대 마나
    public float mpRegen;               // 마나 재생 속도
    public float maxDashCount;          // 최대 대시 횟수
    public float dashRegen;             // 대시 재생 속도
    public float dashDistance;          // 대시 거리
    public float attackDamage;          // 공격력
    public float attackRange;           // 공격 범위
    public float attackSpeed;           // 공격 속도
    public float projectileCount;       // 투사체 개수
    public float projectileSpeed;       // 투사체 속도
    public float moveSpeed;             // 이동 속도
    public float criticalRate;          // 치명타 확률 (criChance)
    public float criticalDamage;        // 치명타 피해 (criDamage)
    public float shield;                // 보호막
    public float bonusExpRate;          // 추가 경험치량
    public float bonusGoldRate;         // 추가 획득 골드량
    public int luck;                    // 행운
    public float cooldownReduction;     // 쿨타임 감소
    public int reviveCount;             // 부활 횟수
    public int startItemCount;          // 처음 시작 아이템 개수

    public static Stats operator +(Stats a, Stats b)
    {
        // 둘 다 값이 없으면 null 반환
        if (a == null && b == null) return null;

        // a가 null 이면 b, b가 null이면 a 반환
        if (a == null) return b;
        if (b == null) return a;

        // 두 구조체의 값을 모두 더함
        Stats result = new Stats
        {
            maxHp = a.maxHp + b.maxHp,
            maxMp = a.maxMp + b.maxMp,
            mpRegen = a.mpRegen + b.mpRegen,
            maxDashCount = a.maxDashCount + b.maxDashCount,
            dashRegen = a.dashRegen + b.dashRegen,
            dashDistance = a.dashDistance + b.dashDistance,
            attackDamage = a.attackDamage + b.attackDamage,
            attackRange = a.attackRange + b.attackRange,
            attackSpeed = a.attackSpeed + b.attackSpeed,
            projectileCount = a.projectileCount + b.projectileCount,
            projectileSpeed = a.projectileSpeed + b.projectileSpeed,
            moveSpeed = a.moveSpeed + b.moveSpeed,
            criticalRate = a.criticalRate + b.criticalRate,
            criticalDamage = a.criticalDamage + b.criticalDamage,
            shield = a.shield + b.shield,
            bonusExpRate = a.bonusExpRate + b.bonusExpRate,
            bonusGoldRate = a.bonusGoldRate + b.bonusGoldRate,
            luck = a.luck + b.luck,
            cooldownReduction = a.cooldownReduction + b.cooldownReduction,
            reviveCount = a.reviveCount + b.reviveCount,
            startItemCount = a.startItemCount + b.startItemCount
        };

        return result;
    }

    public static Stats operator *(Stats a, float multiplier)
    {
        if (a == null) return null;

        Stats result = new Stats
        {
            maxHp = a.maxHp * multiplier,
            maxMp = a.maxMp * multiplier,
            mpRegen = a.mpRegen * multiplier,
            maxDashCount = a.maxDashCount * multiplier,
            dashRegen = a.dashRegen * multiplier,
            dashDistance = a.dashDistance * multiplier,
            attackDamage = a.attackDamage * multiplier,
            attackRange = a.attackRange * multiplier,
            attackSpeed = a.attackSpeed * multiplier,
            projectileCount = a.projectileCount * multiplier,
            projectileSpeed = a.projectileSpeed * multiplier,
            moveSpeed = a.moveSpeed * multiplier,
            criticalRate = a.criticalRate * multiplier,
            criticalDamage = a.criticalDamage * multiplier,
            shield = a.shield * multiplier,
            bonusExpRate = a.bonusExpRate * multiplier,
            bonusGoldRate = a.bonusGoldRate * multiplier,
            luck = (int)(a.luck * multiplier),
            cooldownReduction = a.cooldownReduction * multiplier,
            reviveCount = (int)(a.reviveCount * multiplier),
            startItemCount = (int)(a.startItemCount * multiplier)
        };
        return result;
    }
    public static Stats operator *(Stats a, Stats b)
    {
        if (a == null || b == null) return null;

        return new Stats
        {
            maxHp = a.maxHp * b.maxHp,
            maxMp = a.maxMp * b.maxMp,
            mpRegen = a.mpRegen * b.mpRegen,
            maxDashCount = a.maxDashCount * b.maxDashCount,
            dashRegen = a.dashRegen * b.dashRegen,
            dashDistance = a.dashDistance * b.dashDistance,
            attackDamage = a.attackDamage * b.attackDamage,
            attackRange = a.attackRange * b.attackRange,
            attackSpeed = a.attackSpeed * b.attackSpeed,
            projectileCount = a.projectileCount * b.projectileCount,
            projectileSpeed = a.projectileSpeed * b.projectileSpeed,
            moveSpeed = a.moveSpeed * b.moveSpeed,
            criticalRate = a.criticalRate * b.criticalRate,
            criticalDamage = a.criticalDamage * b.criticalDamage,
            shield = a.shield * b.shield,
            bonusExpRate = a.bonusExpRate * b.bonusExpRate,
            bonusGoldRate = a.bonusGoldRate * b.bonusGoldRate,
            luck = a.luck * b.luck,
            cooldownReduction = a.cooldownReduction * b.cooldownReduction,
            reviveCount = (int)(a.reviveCount * b.reviveCount),
            startItemCount = (int)(a.startItemCount * b.startItemCount) 
        };
    }

    public static Stats CreateMultiplierDefault()
    {
        return new Stats
        {
            maxHp = 1f,
            maxMp = 1f,
            mpRegen = 1f,
            maxDashCount = 1f,
            dashRegen = 1f,
            dashDistance = 1f,
            attackDamage = 1f,
            attackRange = 1f,
            attackSpeed = 1f,
            projectileCount = 1f,
            projectileSpeed = 1f,
            moveSpeed = 1f,
            criticalRate = 1f,
            criticalDamage = 1f,
            shield = 1f,
            bonusExpRate = 1f,
            bonusGoldRate = 1f,
            luck = 1,
            cooldownReduction = 1f,
            reviveCount = 1,
            startItemCount = 1
        };
    }

    public static Stats operator /(Stats a, Stats b)
    {
        return new Stats
        {
            maxHp = a.maxHp / b.maxHp,
            maxMp = a.maxMp / b.maxMp,
            mpRegen = a.mpRegen / b.mpRegen,
            maxDashCount = a.maxDashCount / b.maxDashCount,
            dashRegen = a.dashRegen / b.dashRegen,
            dashDistance = a.dashDistance / b.dashDistance,
            attackDamage = a.attackDamage / b.attackDamage,
            attackRange = a.attackRange / b.attackRange,
            attackSpeed = a.attackSpeed / b.attackSpeed,
            projectileCount = a.projectileCount / b.projectileCount,
            projectileSpeed = a.projectileSpeed / b.projectileSpeed,
            moveSpeed = a.moveSpeed / b.moveSpeed,
            criticalRate = a.criticalRate / b.criticalRate,
            criticalDamage = a.criticalDamage / b.criticalDamage,
            shield = a.shield / b.shield,
            bonusExpRate = a.bonusExpRate / b.bonusExpRate,
            bonusGoldRate = a.bonusGoldRate / b.bonusGoldRate,
            luck = a.luck / b.luck,
            cooldownReduction = a.cooldownReduction / b.cooldownReduction,
            reviveCount = a.reviveCount / b.reviveCount,
            startItemCount = a.startItemCount / b.startItemCount
        };
    }
}
