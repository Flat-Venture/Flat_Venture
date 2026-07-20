using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Test_09 씬에 실제 진입해 플레이어 컴포넌트 사이의 연결을 검증합니다.</summary>
public sealed class PlayerModuleIntegrationTests
{
    // 모든 통합 테스트가 같은 검증 씬을 사용합니다.
    private const string ValidationScenePath =
        "Assets/_Scenes/NUH/Test_09_PlayerValidation.unity";

    // PlayMode에서 찾아 사용할 실제 씬 컴포넌트 참조입니다.
    private PlayerController player;
    private PlayerInputReader inputReader;
    private PlayerLocomotionController locomotion;
    private PlayerHealthController health;
    private PlayerBasicAttackController basicAttack;
    private WarriorActiveSkillController warriorActiveSkill;

    /// <summary>검증 씬을 열고 PlayMode로 전환한 뒤 플레이어 모듈을 찾습니다.</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.OpenScene(ValidationScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        player = Object.FindFirstObjectByType<PlayerController>();
        inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
        locomotion = Object.FindFirstObjectByType<PlayerLocomotionController>();
        health = Object.FindFirstObjectByType<PlayerHealthController>();
        basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
        warriorActiveSkill = Object.FindFirstObjectByType<WarriorActiveSkillController>();
    }

    /// <summary>각 테스트가 끝나면 EditMode로 돌아와 다음 테스트를 독립 실행합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (EditorApplication.isPlaying)
            yield return new ExitPlayMode();
    }

    // 필수 컴포넌트와 기본 공격 활성 상태가 씬에 올바르게 저장됐는지 확인합니다.
    [UnityTest]
    public IEnumerator ValidationScene_LoadsEveryRequiredPlayerModule()
    {
        Assert.That(player, Is.Not.Null);
        Assert.That(inputReader, Is.Not.Null);
        Assert.That(locomotion, Is.Not.Null);
        Assert.That(health, Is.Not.Null);
        Assert.That(basicAttack, Is.Not.Null);
        Assert.That(warriorActiveSkill, Is.Not.Null);
        Assert.That(basicAttack.enabled, Is.True);
        Assert.That(player.RuntimeState, Is.Not.Null);
        Assert.That(EditorApplication.isPlaying, Is.True);
        yield return null;
    }

    // 실제 프레임에서 피격 무적이 즉시 피해를 막고 설정 시간 후 해제되는지 확인합니다.
    [UnityTest]
    public IEnumerator HitInvincibility_BlocksImmediateDamageAndExpiresByGameTime()
    {
        player.ResetPlayer();
        float damage = 10f;
        float expectedAfterFirstHit = player.RuntimeState.MaxHealth - damage;

        bool firstApplied = health.TakeDamage(damage);
        bool immediateApplied = health.TakeDamage(damage);

        Assert.That(firstApplied, Is.True);
        Assert.That(immediateApplied, Is.False);
        Assert.That(player.RuntimeState.CurrentHealth, Is.EqualTo(expectedAfterFirstHit).Within(0.001f));

        Stopwatch timeout = Stopwatch.StartNew();
        while (player.RuntimeState.IsInvincible && timeout.Elapsed.TotalSeconds < 2.0)
            yield return null;

        timeout.Stop();
        Assert.That(player.RuntimeState.IsInvincible, Is.False);

        bool delayedApplied = health.TakeDamage(damage);
        Assert.That(delayedApplied, Is.True);
        Assert.That(player.RuntimeState.CurrentHealth, Is.EqualTo(expectedAfterFirstHit - damage).Within(0.001f));
    }

    // 사망 후 ResetPlayer가 입력·대시·공격·검기·풀 상태를 모두 복구하는지 확인합니다.
    [UnityTest]
    public IEnumerator ResetPlayer_ClearsDeathCooldownDashAndSkillRuntimeState()
    {
        player.ResetPlayer();
        basicAttack.ApplyPostSkillCooldown();
        if (!warriorActiveSkill.IgnoreCooldown)
            warriorActiveSkill.ToggleIgnoreCooldown();

        bool lethalApplied = health.TakeDamage(player.RuntimeState.MaxHealth);

        Assert.That(lethalApplied, Is.True);
        Assert.That(player.RuntimeState.IsDead, Is.True);
        Assert.That(inputReader.enabled, Is.False);
        Assert.That(basicAttack.CooldownRemaining, Is.GreaterThan(0f));
        Assert.That(warriorActiveSkill.IgnoreCooldown, Is.True);

        player.ResetPlayer();
        yield return null;

        Assert.That(player.RuntimeState.IsDead, Is.False);
        Assert.That(player.RuntimeState.CurrentHealth, Is.EqualTo(player.RuntimeState.MaxHealth));
        Assert.That(player.RuntimeState.IsInvincible, Is.False);
        Assert.That(inputReader.enabled, Is.True);
        Assert.That(locomotion.DashCharges, Is.EqualTo(player.RuntimeState.MaxDashCharges));
        Assert.That(basicAttack.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
        Assert.That(warriorActiveSkill.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
        Assert.That(warriorActiveSkill.IgnoreCooldown, Is.False);
        Assert.That(warriorActiveSkill.IsCasting, Is.False);
        Assert.That(warriorActiveSkill.IsAiming, Is.False);
        Assert.That(warriorActiveSkill.PoolActiveCount, Is.EqualTo(0));
    }
}
