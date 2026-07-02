using UnityEngine;

namespace FlatVenture.NUH.Player.Data
{
    /// <summary>
    /// 직업별 플레이어의 변경되지 않는 원본 능력치 데이터입니다.
    /// 플레이 중에는 이 에셋을 수정하지 않고 런타임 상태로 복사해서 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats_", menuName = "Flat Venture/NUH/Player Stats")]
    public sealed class PlayerStatsData : ScriptableObject
    {
        [Header("식별 정보")]
        [SerializeField] private string jobId = "Warrior";

        [Header("생존")]
        [Min(1f)]
        [SerializeField] private float maxHealth = 50f;

        [Min(0f)]
        [SerializeField] private float hitInvincibilityDuration = 0.3f;

        [Header("이동")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 5f;

        [Header("대시")]
        [Min(0.01f)] [SerializeField] private float dashDuration = 0.25f;
        [Min(0f)] [SerializeField] private float dashDistance = 4f;
        [Min(0.01f)] [SerializeField] private float dashRechargeCooldown = 2f;
        [Min(1)] [SerializeField] private int maxDashCharges = 2;

        [Header("기본 공격")]
        [Min(0f)]
        [SerializeField] private float attackPower = 10f;

        [Min(0.01f)]
        [SerializeField] private float attacksPerSecond = 1f;

        [Min(0f)]
        [SerializeField] private float basicAttackRange = 1.5f;

        [Min(0.1f)]
        [SerializeField] private float basicAttackWidth = 1f;

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

        [Min(1)]
        [SerializeField] private int activeSkillMaxHitTargets = 10;

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
        public float BasicAttackRange { get { return basicAttackRange; } }
        public float BasicAttackWidth { get { return basicAttackWidth; } }
        public float ActiveSkillCooldown { get { return activeSkillCooldown; } }
        public float ActiveSkillCastTime { get { return activeSkillCastTime; } }
        public float ActiveSkillDamage { get { return activeSkillDamage; } }
        public int ActiveSkillProjectileCount { get { return activeSkillProjectileCount; } }
        public float ActiveSkillProjectileInterval { get { return activeSkillProjectileInterval; } }
        public float ActiveSkillProjectileSpeed { get { return activeSkillProjectileSpeed; } }
        public float ActiveSkillProjectileWidth { get { return activeSkillProjectileWidth; } }
        public int ActiveSkillMaxHitTargets { get { return activeSkillMaxHitTargets; } }

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
            activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown);
            activeSkillCastTime = Mathf.Max(0f, activeSkillCastTime);
            activeSkillDamage = Mathf.Max(0f, activeSkillDamage);
            activeSkillProjectileCount = Mathf.Max(1, activeSkillProjectileCount);
            activeSkillProjectileInterval = Mathf.Max(0f, activeSkillProjectileInterval);
            activeSkillProjectileSpeed = Mathf.Max(0.01f, activeSkillProjectileSpeed);
            activeSkillProjectileWidth = Mathf.Max(0.1f, activeSkillProjectileWidth);
            activeSkillMaxHitTargets = Mathf.Max(1, activeSkillMaxHitTargets);
        }
    }
}
