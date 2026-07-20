using System;
using UnityEngine;

[Flags]
public enum InvincibilityReason
{
    // 어떤 무적 사유도 없는 상태입니다.
    None = 0,
    // 대시가 진행되는 동안 적용되는 무적입니다.
    Dash = 1 << 0,
    // 피해를 받은 직후 짧게 적용되는 무적입니다.
    HitGrace = 1 << 1,
    // 테스트 UI에서 강제로 켜는 무적입니다.
    Debug = 1 << 2
}

/// <summary>
/// 한 플레이 동안 변하는 플레이어 상태입니다. SO 원본을 복사하며 원본 에셋은 수정하지 않습니다.
/// </summary>
public sealed class PlayerRuntimeState
{
    // 아래 속성들은 모두 SO를 직접 바꾸지 않고 현재 플레이에서만 사용하는 복사본입니다.
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
    public BasicAttackType BasicAttackType { get; private set; }
    public float BasicAttackRange { get; private set; }
    public float BasicAttackWidth { get; private set; }
    public float BasicAttackProjectileSpeed { get; private set; }
    public float ActiveSkillCooldown { get; private set; }
    public float ActiveSkillCastTime { get; private set; }
    public float ActiveSkillDamage { get; private set; }
    public int ActiveSkillProjectileCount { get; private set; }
    public float ActiveSkillProjectileInterval { get; private set; }
    public float ActiveSkillProjectileSpeed { get; private set; }
    public float ActiveSkillProjectileWidth { get; private set; }
    public float ActiveSkillRange { get; private set; }
    public int ActiveSkillMaxHitTargets { get; private set; }

    // UI와 각 기능 모듈이 상태 변화를 즉시 받을 수 있도록 제공하는 이벤트입니다.
    public event Action<float, float> HealthChanged;
    public event Action Died;
    public event Action ResetCompleted;

    /// <summary>원본 SO를 등록하고 첫 런타임 초기화를 수행합니다.</summary>
    public void Initialize(PlayerStatsData source)
    {
        Source = source != null ? source : throw new ArgumentNullException(nameof(source));
        Reset();
    }

    /// <summary>
    /// 원본 SO의 모든 값을 런타임 속성으로 다시 복사하고 생존 상태를 초기화합니다.
    /// 마지막에 ResetCompleted를 호출해 다른 플레이어 모듈도 자신의 상태를 비우게 합니다.
    /// </summary>
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
        BasicAttackType = Source.BasicAttackType;
        BasicAttackRange = Source.BasicAttackRange;
        BasicAttackWidth = Source.BasicAttackWidth;
        BasicAttackProjectileSpeed = Source.BasicAttackProjectileSpeed;
        ActiveSkillCooldown = Source.ActiveSkillCooldown;
        ActiveSkillCastTime = Source.ActiveSkillCastTime;
        ActiveSkillDamage = Source.ActiveSkillDamage;
        ActiveSkillProjectileCount = Source.ActiveSkillProjectileCount;
        ActiveSkillProjectileInterval = Source.ActiveSkillProjectileInterval;
        ActiveSkillProjectileSpeed = Source.ActiveSkillProjectileSpeed;
        ActiveSkillProjectileWidth = Source.ActiveSkillProjectileWidth;
        ActiveSkillRange = Source.ActiveSkillRange;
        ActiveSkillMaxHitTargets = Source.ActiveSkillMaxHitTargets;

        IsDead = false;
        Invincibility = InvincibilityReason.None;
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        ResetCompleted?.Invoke();
    }

    /// <summary>
    /// 피해를 적용할 수 있는 상태인지 검사한 뒤 HP를 감소시킵니다.
    /// 실제 피격 무적시간 관리는 PlayerHealthController가 담당합니다.
    /// </summary>
    /// <returns>피해가 실제로 적용되었으면 true입니다.</returns>
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

    /// <summary>
    /// 비트 플래그 방식으로 특정 무적 사유를 추가하거나 제거합니다.
    /// 한 사유를 제거해도 다른 사유가 남아 있으면 무적은 유지됩니다.
    /// </summary>
    public void SetInvincibility(InvincibilityReason reason, bool active)
    {
        if (active)
            Invincibility |= reason;
        else
            Invincibility &= ~reason;
    }

    /// <summary>최대 HP를 최소 1로 제한하고 현재 HP가 새 최대치를 넘지 않게 맞춥니다.</summary>
    public void SetMaxHealth(float value)
    {
        MaxHealth = Mathf.Max(1f, value);
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>현재 HP를 최대 HP까지 회복합니다.</summary>
    public void RestoreHealth()
    {
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    // 아래 Setter들은 디버그 UI나 추후 아이템 효과가 안전한 범위 안에서 값을 바꾸는 진입점입니다.
    public void SetMoveSpeed(float value) { MoveSpeed = Mathf.Max(0f, value); }
    public void SetDashDistance(float value) { DashDistance = Mathf.Max(0f, value); }
    public void SetMaxDashCharges(int value) { MaxDashCharges = Mathf.Clamp(value, 1, 10); }
    public void SetAttackPower(float value) { AttackPower = Mathf.Max(0f, value); }
    public void SetBasicAttackProjectileSpeed(float value) { BasicAttackProjectileSpeed = Mathf.Max(0.01f, value); }

    /// <summary>공격속도를 원본의 30%~170% 범위로 제한합니다.</summary>
    public void SetAttacksPerSecond(float value)
    {
        AttacksPerSecond = Mathf.Clamp(value, Source.AttacksPerSecond * 0.3f, Source.AttacksPerSecond * 1.7f);
    }

    /// <summary>대시 충전 쿨타임을 원본의 30%~170% 범위로 제한합니다.</summary>
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
    public void SetActiveSkillRange(float value) { ActiveSkillRange = Mathf.Max(0.1f, value); }
    public void SetActiveSkillCastTime(float value) { ActiveSkillCastTime = Mathf.Clamp(value, 0f, 5f); }
}
