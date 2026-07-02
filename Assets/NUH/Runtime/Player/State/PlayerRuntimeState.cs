using System;
using FlatVenture.NUH.Player.Data;
using UnityEngine;

namespace FlatVenture.NUH.Player.State
{
    [Flags]
    public enum InvincibilityReason
    {
        None = 0,
        Dash = 1 << 0,
        HitGrace = 1 << 1,
        Debug = 1 << 2
    }

    /// <summary>
    /// 한 플레이 동안 변하는 플레이어 상태입니다. SO 원본을 복사하며 원본 에셋은 수정하지 않습니다.
    /// </summary>
    public sealed class PlayerRuntimeState
    {
        public PlayerStatsData Source { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public InvincibilityReason Invincibility { get; private set; }
        public bool IsInvincible { get { return Invincibility != InvincibilityReason.None; } }

        public float MaxHealth { get; private set; }
        public float HitInvincibilityDuration { get; private set; }
        public float MoveSpeed { get; private set; }
        public float DashDuration { get; private set; }
        public float DashDistance { get; private set; }
        public float DashRechargeCooldown { get; private set; }
        public int MaxDashCharges { get; private set; }
        public float AttackPower { get; private set; }
        public float AttacksPerSecond { get; private set; }
        public float BasicAttackRange { get; private set; }
        public float BasicAttackWidth { get; private set; }
        public float ActiveSkillCooldown { get; private set; }
        public float ActiveSkillCastTime { get; private set; }
        public float ActiveSkillDamage { get; private set; }
        public int ActiveSkillProjectileCount { get; private set; }
        public float ActiveSkillProjectileInterval { get; private set; }
        public float ActiveSkillProjectileSpeed { get; private set; }
        public float ActiveSkillProjectileWidth { get; private set; }
        public int ActiveSkillMaxHitTargets { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public event Action ResetCompleted;

        public void Initialize(PlayerStatsData source)
        {
            Source = source != null ? source : throw new ArgumentNullException(nameof(source));
            Reset();
        }

        public void Reset()
        {
            if (Source == null)
                throw new InvalidOperationException("PlayerStatsData를 먼저 설정해야 합니다.");

            MaxHealth = Source.MaxHealth;
            HitInvincibilityDuration = Source.HitInvincibilityDuration;
            MoveSpeed = Source.MoveSpeed;
            DashDuration = Source.DashDuration;
            DashDistance = Source.DashDistance;
            DashRechargeCooldown = Source.DashRechargeCooldown;
            MaxDashCharges = Source.MaxDashCharges;
            AttackPower = Source.AttackPower;
            AttacksPerSecond = Source.AttacksPerSecond;
            BasicAttackRange = Source.BasicAttackRange;
            BasicAttackWidth = Source.BasicAttackWidth;
            ActiveSkillCooldown = Source.ActiveSkillCooldown;
            ActiveSkillCastTime = Source.ActiveSkillCastTime;
            ActiveSkillDamage = Source.ActiveSkillDamage;
            ActiveSkillProjectileCount = Source.ActiveSkillProjectileCount;
            ActiveSkillProjectileInterval = Source.ActiveSkillProjectileInterval;
            ActiveSkillProjectileSpeed = Source.ActiveSkillProjectileSpeed;
            ActiveSkillProjectileWidth = Source.ActiveSkillProjectileWidth;
            ActiveSkillMaxHitTargets = Source.ActiveSkillMaxHitTargets;

            IsDead = false;
            Invincibility = InvincibilityReason.None;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            ResetCompleted?.Invoke();
        }

        public bool ApplyDamage(float amount)
        {
            if (IsDead || IsInvincible || amount <= 0f)
                return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (CurrentHealth > 0f)
                return true;

            IsDead = true;
            Died?.Invoke();
            return true;
        }

        public void SetInvincibility(InvincibilityReason reason, bool active)
        {
            if (active)
                Invincibility |= reason;
            else
                Invincibility &= ~reason;
        }

        public void SetMaxHealth(float value)
        {
            MaxHealth = Mathf.Max(1f, value);
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void RestoreHealth()
        {
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void SetMoveSpeed(float value) { MoveSpeed = Mathf.Max(0f, value); }
        public void SetDashDistance(float value) { DashDistance = Mathf.Max(0f, value); }
        public void SetMaxDashCharges(int value) { MaxDashCharges = Mathf.Clamp(value, 1, 10); }
        public void SetAttackPower(float value) { AttackPower = Mathf.Max(0f, value); }

        public void SetAttacksPerSecond(float value)
        {
            AttacksPerSecond = Mathf.Clamp(value, Source.AttacksPerSecond * 0.3f, Source.AttacksPerSecond * 1.7f);
        }

        public void SetDashRechargeCooldown(float value)
        {
            DashRechargeCooldown = Mathf.Clamp(
                value,
                Source.DashRechargeCooldown * 0.3f,
                Source.DashRechargeCooldown * 1.7f);
        }

        public void SetActiveSkillDamage(float value) { ActiveSkillDamage = Mathf.Max(0f, value); }
        public void SetActiveSkillProjectileSpeed(float value) { ActiveSkillProjectileSpeed = Mathf.Max(0.01f, value); }
        public void SetActiveSkillProjectileWidth(float value) { ActiveSkillProjectileWidth = Mathf.Max(0.1f, value); }
        public void SetActiveSkillCastTime(float value) { ActiveSkillCastTime = Mathf.Clamp(value, 0f, 5f); }
    }
}
