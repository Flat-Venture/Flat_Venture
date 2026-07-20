using UnityEngine;

/// <summary>
/// 직업별 플레이어의 변경되지 않는 원본 능력치 데이터입니다.
/// 플레이 중에는 이 에셋을 수정하지 않고 런타임 상태로 복사해서 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats_", menuName = "Flat Venture/NUH/Player Stats")]
public sealed class PlayerStatsData : ScriptableObject
{
    // 저장·직업 선택에서 사용할 안정적인 직업 식별자입니다.
    [Header("식별 정보")]
    [SerializeField] private string jobId = "Warrior";

    // 생존 관련 원본값: 최대 체력과 일반 피격 후 무적시간입니다.
    [Header("생존")]
    [Min(1f)]
    [SerializeField] private float maxHealth = 50f;

    [Min(0f)]
    [SerializeField] private float hitInvincibilityDuration = 0.3f;

    // CharacterController가 초당 이동할 월드 거리입니다.
    [Header("이동")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 5f;

    // 대시 1회의 시간·거리와 순차 충전 규칙에 필요한 원본값입니다.
    [Header("대시")]
    [Min(0.01f)] [SerializeField] private float dashDuration = 0.25f;
    [Min(0f)] [SerializeField] private float dashDistance = 4f;
    [Min(0.01f)] [SerializeField] private float dashRechargeCooldown = 2f;
    [Min(1)] [SerializeField] private int maxDashCharges = 2;

    // 자동/수동 근접 기본 공격의 피해·주기·판정 크기입니다.
    [Header("기본 공격")]
    [SerializeField] private BasicAttackType basicAttackType = BasicAttackType.Melee;

    [Min(0f)]
    [SerializeField] private float attackPower = 10f;

    [Min(0.01f)]
    [SerializeField] private float attacksPerSecond = 1f;

    [Min(0f)]
    [SerializeField] private float basicAttackRange = 1.5f;

    [Min(0.1f)]
    [SerializeField] private float basicAttackWidth = 1f;

    [Min(0.01f)]
    [SerializeField] private float basicAttackProjectileSpeed = 18f;

    // 전사 검기의 쿨타임, 선딜레이, 연발 수, 투사체 수치입니다.
    [Header("액티브 스킬")]
    [Min(0f)]
    [SerializeField] private float activeSkillCooldown = 10f;

    [Min(0f)]
    [SerializeField] private float activeSkillCastTime = 0.2f;

    [Min(0f)]
    [SerializeField] private float activeSkillDamage = 20f;

    [Min(1)]
    [SerializeField] private int activeSkillProjectileCount = 3;

    [Min(0f)]
    [SerializeField] private float activeSkillProjectileInterval = 0.15f;

    [Min(0.01f)]
    [SerializeField] private float activeSkillProjectileSpeed = 15f;

    [Min(0.1f)]
    [SerializeField] private float activeSkillProjectileWidth = 2f;

    [Min(0.1f)]
    [SerializeField] private float activeSkillRange = 15f;

    [Min(1)]
    [SerializeField] private int activeSkillMaxHitTargets = 10;

    // 외부에서는 읽기만 가능하게 공개해 SO 원본이 플레이 도중 바뀌지 않도록 합니다.
    public string JobId { get { return jobId; } }
    public float MaxHealth { get { return maxHealth; } }
    public float HitInvincibilityDuration { get { return hitInvincibilityDuration; } }
    public float MoveSpeed { get { return moveSpeed; } }
    public float DashDuration { get { return dashDuration; } }
    public float DashDistance { get { return dashDistance; } }
    public float DashRechargeCooldown { get { return dashRechargeCooldown; } }
    public int MaxDashCharges { get { return maxDashCharges; } }
    public float AttackPower { get { return attackPower; } }
    public float AttacksPerSecond { get { return attacksPerSecond; } }
    public BasicAttackType BasicAttackType { get { return basicAttackType; } }
    public float BasicAttackRange { get { return basicAttackRange; } }
    public float BasicAttackWidth { get { return basicAttackWidth; } }
    public float BasicAttackProjectileSpeed { get { return basicAttackProjectileSpeed; } }
    public float ActiveSkillCooldown { get { return activeSkillCooldown; } }
    public float ActiveSkillCastTime { get { return activeSkillCastTime; } }
    public float ActiveSkillDamage { get { return activeSkillDamage; } }
    public int ActiveSkillProjectileCount { get { return activeSkillProjectileCount; } }
    public float ActiveSkillProjectileInterval { get { return activeSkillProjectileInterval; } }
    public float ActiveSkillProjectileSpeed { get { return activeSkillProjectileSpeed; } }
    public float ActiveSkillProjectileWidth { get { return activeSkillProjectileWidth; } }
    public float ActiveSkillRange { get { return activeSkillRange; } }
    public int ActiveSkillMaxHitTargets { get { return activeSkillMaxHitTargets; } }

    /// <summary>
    /// 인스펙터에서 잘못된 음수나 0을 입력했을 때 안전한 최솟값으로 보정합니다.
    /// 에디터에서 값이 바뀔 때만 실행되며 런타임 강화 계산은 PlayerRuntimeState가 담당합니다.
    /// </summary>
    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        hitInvincibilityDuration = Mathf.Max(0f, hitInvincibilityDuration);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashDistance = Mathf.Max(0f, dashDistance);
        dashRechargeCooldown = Mathf.Max(0.01f, dashRechargeCooldown);
        maxDashCharges = Mathf.Max(1, maxDashCharges);
        attackPower = Mathf.Max(0f, attackPower);
        attacksPerSecond = Mathf.Max(0.01f, attacksPerSecond);
        basicAttackRange = Mathf.Max(0f, basicAttackRange);
        basicAttackWidth = Mathf.Max(0.1f, basicAttackWidth);
        basicAttackProjectileSpeed = Mathf.Max(0.01f, basicAttackProjectileSpeed);
        activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown);
        activeSkillCastTime = Mathf.Max(0f, activeSkillCastTime);
        activeSkillDamage = Mathf.Max(0f, activeSkillDamage);
        activeSkillProjectileCount = Mathf.Max(1, activeSkillProjectileCount);
        activeSkillProjectileInterval = Mathf.Max(0f, activeSkillProjectileInterval);
        activeSkillProjectileSpeed = Mathf.Max(0.01f, activeSkillProjectileSpeed);
        activeSkillProjectileWidth = Mathf.Max(0.1f, activeSkillProjectileWidth);
        activeSkillRange = Mathf.Max(0.1f, activeSkillRange);
        activeSkillMaxHitTargets = Mathf.Max(1, activeSkillMaxHitTargets);
    }
}
