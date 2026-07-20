using NUnit.Framework;
using UnityEngine;

/// <summary>SO 원본과 분리된 플레이어 런타임 상태·이벤트·경계값을 검증합니다.</summary>
public sealed class PlayerRuntimeStateTests
{
    // 각 테스트가 독립적으로 사용할 새 SO와 RuntimeState입니다.
    private PlayerStatsData source;
    private PlayerRuntimeState state;
    private int diedCount;
    private int resetCount;
    private int healthChangedCount;

    /// <summary>테스트 전 기본 SO 복사본과 이벤트 카운터를 준비합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        diedCount = 0;
        resetCount = 0;
        healthChangedCount = 0;
        source = ScriptableObject.CreateInstance<PlayerStatsData>();
        state = new PlayerRuntimeState();
        state.Initialize(source);
        state.Died += OnDied;
        state.ResetCompleted += OnResetCompleted;
        state.HealthChanged += OnHealthChanged;
    }

    /// <summary>테스트 후 이벤트를 해제하고 임시 SO를 파괴합니다.</summary>
    [TearDown]
    public void TearDown()
    {
        state.Died -= OnDied;
        state.ResetCompleted -= OnResetCompleted;
        state.HealthChanged -= OnHealthChanged;
        Object.DestroyImmediate(source);
    }

    // 런타임 값을 바꿔도 SO 원본이 변하지 않는지 확인합니다.
    [Test]
    public void Initialize_CopiesSourceValuesWithoutChangingSource()
    {
        float originalMoveSpeed = source.MoveSpeed;
        float originalAttackPower = source.AttackPower;

        state.SetMoveSpeed(originalMoveSpeed + 10f);
        state.SetAttackPower(originalAttackPower + 10f);

        Assert.That(state.Source, Is.SameAs(source));
        Assert.That(source.MoveSpeed, Is.EqualTo(originalMoveSpeed));
        Assert.That(source.AttackPower, Is.EqualTo(originalAttackPower));
        Assert.That(state.MoveSpeed, Is.Not.EqualTo(source.MoveSpeed));
        Assert.That(state.AttackPower, Is.Not.EqualTo(source.AttackPower));
    }

    // 궁수 기본 공격 방식과 투사체 속도가 SO에서 런타임 복사본으로 전달되는지 확인합니다.
    [Test]
    public void Initialize_CopiesProjectileBasicAttackSettings()
    {
        UnityEditor.SerializedObject sourceObject = new UnityEditor.SerializedObject(source);
        sourceObject.FindProperty("basicAttackType").enumValueIndex = (int)BasicAttackType.Projectile;
        sourceObject.FindProperty("basicAttackProjectileSpeed").floatValue = 21f;
        sourceObject.ApplyModifiedPropertiesWithoutUndo();

        state.Initialize(source);

        Assert.That(state.BasicAttackType, Is.EqualTo(BasicAttackType.Projectile));
        Assert.That(state.BasicAttackProjectileSpeed, Is.EqualTo(21f));
    }

    // 어떤 무적 사유든 켜져 있으면 피해가 거부되는지 확인합니다.
    [Test]
    public void ApplyDamage_InvincibleStateRejectsDamage()
    {
        state.SetInvincibility(InvincibilityReason.Debug, true);
        float healthBeforeDamage = state.CurrentHealth;

        bool applied = state.ApplyDamage(10f);

        Assert.That(applied, Is.False);
        Assert.That(state.CurrentHealth, Is.EqualTo(healthBeforeDamage));
        Assert.That(healthChangedCount, Is.EqualTo(0));
    }

    // 비트 플래그 한 개를 꺼도 다른 무적 사유가 유지되는지 확인합니다.
    [Test]
    public void InvincibilityReasons_ClearingOneReasonPreservesOtherReason()
    {
        state.SetInvincibility(InvincibilityReason.Dash, true);
        state.SetInvincibility(InvincibilityReason.HitGrace, true);

        state.SetInvincibility(InvincibilityReason.Dash, false);

        Assert.That(state.IsInvincible, Is.True);
        Assert.That(state.Invincibility, Is.EqualTo(InvincibilityReason.HitGrace));

        state.SetInvincibility(InvincibilityReason.HitGrace, false);
        Assert.That(state.IsInvincible, Is.False);
    }

    // 치명 피해가 사망 이벤트를 한 번만 발생시키는지 확인합니다.
    [Test]
    public void LethalDamage_RaisesDeathOnceAndBlocksFurtherDamage()
    {
        bool lethalApplied = state.ApplyDamage(state.MaxHealth);
        bool extraApplied = state.ApplyDamage(1f);

        Assert.That(lethalApplied, Is.True);
        Assert.That(extraApplied, Is.False);
        Assert.That(state.CurrentHealth, Is.EqualTo(0f));
        Assert.That(state.IsDead, Is.True);
        Assert.That(diedCount, Is.EqualTo(1));
        Assert.That(healthChangedCount, Is.EqualTo(1));
    }

    // Reset이 사망·무적·HP·능력치를 원본값으로 복구하는지 확인합니다.
    [Test]
    public void Reset_RestoresOriginalRuntimeState()
    {
        state.SetMoveSpeed(source.MoveSpeed + 3f);
        state.ApplyDamage(state.MaxHealth);
        state.SetInvincibility(InvincibilityReason.Debug, true);

        state.Reset();

        Assert.That(state.IsDead, Is.False);
        Assert.That(state.IsInvincible, Is.False);
        Assert.That(state.CurrentHealth, Is.EqualTo(source.MaxHealth));
        Assert.That(state.MaxHealth, Is.EqualTo(source.MaxHealth));
        Assert.That(state.MoveSpeed, Is.EqualTo(source.MoveSpeed));
        Assert.That(resetCount, Is.EqualTo(1));
    }

    // 공격속도 하한과 상한이 원본의 30%와 170%인지 확인합니다.
    [Test]
    public void AttackSpeed_IsClampedToMinusAndPlusSeventyPercent()
    {
        state.SetAttacksPerSecond(float.MinValue);
        Assert.That(state.AttacksPerSecond, Is.EqualTo(source.AttacksPerSecond * 0.3f).Within(0.0001f));

        state.SetAttacksPerSecond(float.MaxValue);
        Assert.That(state.AttacksPerSecond, Is.EqualTo(source.AttacksPerSecond * 1.7f).Within(0.0001f));
    }

    // 대시 충전 쿨타임 하한과 상한도 같은 규칙인지 확인합니다.
    [Test]
    public void DashCooldown_IsClampedToMinusAndPlusSeventyPercent()
    {
        state.SetDashRechargeCooldown(float.MinValue);
        Assert.That(state.DashRechargeCooldown, Is.EqualTo(source.DashRechargeCooldown * 0.3f).Within(0.0001f));

        state.SetDashRechargeCooldown(float.MaxValue);
        Assert.That(state.DashRechargeCooldown, Is.EqualTo(source.DashRechargeCooldown * 1.7f).Within(0.0001f));
    }

    // 나머지 Setter가 음수·과도한 값을 안전 범위로 제한하는지 확인합니다.
    [Test]
    public void RuntimeSetters_EnforceSafeMinimumAndMaximumValues()
    {
        state.SetMoveSpeed(-1f);
        state.SetDashDistance(-1f);
        state.SetMaxDashCharges(100);
        state.SetAttackPower(-1f);
        state.SetBasicAttackProjectileSpeed(-1f);
        state.SetActiveSkillDamage(-1f);
        state.SetActiveSkillProjectileSpeed(-1f);
        state.SetActiveSkillProjectileWidth(-1f);
        state.SetActiveSkillRange(-1f);
        state.SetActiveSkillCastTime(100f);

        Assert.That(state.MoveSpeed, Is.EqualTo(0f));
        Assert.That(state.DashDistance, Is.EqualTo(0f));
        Assert.That(state.MaxDashCharges, Is.EqualTo(10));
        Assert.That(state.AttackPower, Is.EqualTo(0f));
        Assert.That(state.BasicAttackProjectileSpeed, Is.EqualTo(0.01f));
        Assert.That(state.ActiveSkillDamage, Is.EqualTo(0f));
        Assert.That(state.ActiveSkillProjectileSpeed, Is.EqualTo(0.01f));
        Assert.That(state.ActiveSkillProjectileWidth, Is.EqualTo(0.1f));
        Assert.That(state.ActiveSkillRange, Is.EqualTo(0.1f));
        Assert.That(state.ActiveSkillCastTime, Is.EqualTo(5f));
    }

    private void OnDied()
    {
        diedCount++;
    }

    private void OnResetCompleted()
    {
        resetCount++;
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        healthChangedCount++;
    }
}
