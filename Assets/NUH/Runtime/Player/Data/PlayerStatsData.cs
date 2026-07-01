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

        [Header("액티브 스킬")]
        [Min(0f)]
        [SerializeField] private float activeSkillCooldown = 10f;

        [Min(0f)]
        [SerializeField] private float activeSkillCastTime = 0.2f;

        public string JobId => jobId;
        public float MaxHealth => maxHealth;
        public float HitInvincibilityDuration => hitInvincibilityDuration;
        public float MoveSpeed => moveSpeed;
        public float DashDuration => dashDuration;
        public float DashDistance => dashDistance;
        public float DashRechargeCooldown => dashRechargeCooldown;
        public int MaxDashCharges => maxDashCharges;
        public float AttackPower => attackPower;
        public float AttacksPerSecond => attacksPerSecond;
        public float BasicAttackRange => basicAttackRange;
        public float ActiveSkillCooldown => activeSkillCooldown;
        public float ActiveSkillCastTime => activeSkillCastTime;

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
            activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown);
            activeSkillCastTime = Mathf.Max(0f, activeSkillCastTime);
        }
    }
}
