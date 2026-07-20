using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 모듈의 상태를 표시하고 즉시 판정 가능한 검증 항목을 실행하는 테스트 전용 패널입니다.
/// </summary>
public sealed class PlayerModuleValidationPanel : MonoBehaviour
{
    // 자동 검증과 실시간 상태 표시에 필요한 플레이어 모듈 및 UI 참조입니다.
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerLocomotionController locomotion;
    [SerializeField] private PlayerHealthController health;
    [SerializeField] private PlayerBasicAttackController basicAttack;
    [SerializeField] private WarriorActiveSkillController warriorActiveSkill;
    [SerializeField] private Text liveStatusOutput;
    [SerializeField] private Text validationResultOutput;

    // 여러 검증 결과를 매번 문자열로 새로 만들지 않고 누적하는 버퍼와 집계값입니다.
    private readonly StringBuilder resultBuilder = new StringBuilder();
    private int passedCount;
    private int failedCount;

    /// <summary>PlayerController가 초기화한 런타임 상태에 안전하게 접근합니다.</summary>
    private PlayerRuntimeState State
    {
        get
        {
            if (player == null)
                return null;
            return player.RuntimeState;
        }
    }

    /// <summary>수동 테스트 중 확인할 HP·무적·대시·공격·검기·풀 상태를 표시합니다.</summary>
    private void Update()
    {
        PlayerRuntimeState state = State;
        if (state == null || liveStatusOutput == null)
            return;

        liveStatusOutput.text =
            $"[실시간 상태]\n" +
            $"HP {state.CurrentHealth:0.##}/{state.MaxHealth:0.##} | 사망 {state.IsDead} | 입력 {(inputReader != null && inputReader.enabled)}\n" +
            $"무적 {state.Invincibility} | 대시 {GetDashState()} | 충전 {GetDashCharges()}\n" +
            $"기본공격 쿨타임 {GetBasicAttackCooldown():0.###} | 수동조준 {GetManualAim()} | 대상 {GetTargetName()}\n" +
            $"검기 조준 {GetSkillAiming()} | 시전 {GetSkillCasting()} | 쿨타임 {GetSkillCooldown():0.###}\n" +
            $"검기 풀 Active {GetPoolActive()} / Inactive {GetPoolInactive()} / Total {GetPoolTotal()}";
    }

    /// <summary>즉시 판정할 수 있는 네 가지 검증을 순서대로 모두 실행합니다.</summary>
    public void RunAllChecks()
    {
        ClearResults();
        ExecuteDamageGraceCheck();
        ExecuteInvincibilityOverlapCheck();
        ExecuteDeathResetCheck();
        ExecuteBoundaryCheck();
        AppendSummary();
    }

    // 개별 public 함수는 해당 검증 버튼 하나에 연결됩니다.
    public void RunDamageGraceCheck()
    {
        ClearResults();
        ExecuteDamageGraceCheck();
        AppendSummary();
    }

    public void RunInvincibilityOverlapCheck()
    {
        ClearResults();
        ExecuteInvincibilityOverlapCheck();
        AppendSummary();
    }

    public void RunDeathResetCheck()
    {
        ClearResults();
        ExecuteDeathResetCheck();
        AppendSummary();
    }

    public void RunBoundaryCheck()
    {
        ClearResults();
        ExecuteBoundaryCheck();
        AppendSummary();
    }

    /// <summary>결과 문자열과 PASS/FAIL 집계를 비웁니다.</summary>
    public void ClearResults()
    {
        resultBuilder.Clear();
        passedCount = 0;
        failedCount = 0;
        RefreshResults();
    }

    /// <summary>첫 피해 직후 두 번째 피해가 HitGrace 무적으로 차단되는지 검사합니다.</summary>
    private void ExecuteDamageGraceCheck()
    {
        if (!EnsureReferences("피격 무적"))
            return;

        player.ResetPlayer();
        float damage = Mathf.Min(10f, State.MaxHealth * 0.25f);
        float expectedHealth = State.MaxHealth - damage;
        bool firstApplied = health.TakeDamage(damage);
        bool secondApplied = health.TakeDamage(damage);
        bool passed = firstApplied
            && !secondApplied
            && Mathf.Approximately(State.CurrentHealth, expectedHealth)
            && (State.Invincibility & InvincibilityReason.HitGrace) != 0;

        AppendResult("피격 직후 무적이 연속 피해를 차단", passed);
        player.ResetPlayer();
    }

    /// <summary>대시 무적을 해제해도 함께 켜진 피격 무적이 유지되는지 검사합니다.</summary>
    private void ExecuteInvincibilityOverlapCheck()
    {
        if (!EnsureReferences("무적 중첩"))
            return;

        player.ResetPlayer();
        State.SetInvincibility(InvincibilityReason.Dash, true);
        State.SetInvincibility(InvincibilityReason.HitGrace, true);
        State.SetInvincibility(InvincibilityReason.Dash, false);

        bool hitGracePreserved = State.IsInvincible
            && State.Invincibility == InvincibilityReason.HitGrace;

        State.SetInvincibility(InvincibilityReason.HitGrace, false);
        bool allCleared = !State.IsInvincible;
        AppendResult("대시 무적 해제 후 피격 무적이 유지", hitGracePreserved && allCleared);
        player.ResetPlayer();
    }

    /// <summary>사망 입력 차단과 재시작 후 모든 모듈 초기화를 함께 검사합니다.</summary>
    private void ExecuteDeathResetCheck()
    {
        if (!EnsureReferences("사망·재시작"))
            return;

        player.ResetPlayer();
        basicAttack.ApplyPostSkillCooldown();
        if (!warriorActiveSkill.IgnoreCooldown)
            warriorActiveSkill.ToggleIgnoreCooldown();

        bool lethalApplied = health.TakeDamage(State.MaxHealth);
        bool deathStateValid = lethalApplied && State.IsDead && !inputReader.enabled;

        player.ResetPlayer();
        bool resetStateValid = !State.IsDead
            && Mathf.Approximately(State.CurrentHealth, State.MaxHealth)
            && State.Invincibility == InvincibilityReason.None
            && inputReader.enabled
            && locomotion.DashCharges == State.MaxDashCharges
            && Mathf.Approximately(basicAttack.CooldownRemaining, 0f)
            && Mathf.Approximately(warriorActiveSkill.CooldownRemaining, 0f)
            && !warriorActiveSkill.IsCasting
            && !warriorActiveSkill.IsAiming
            && !warriorActiveSkill.IgnoreCooldown
            && warriorActiveSkill.PoolActiveCount == 0;

        AppendResult("사망 시 입력 차단 및 재시작 전체 초기화", deathStateValid && resetStateValid);
    }

    /// <summary>±70% 제한과 각 능력치의 안전 최솟값·최댓값을 검사합니다.</summary>
    private void ExecuteBoundaryCheck()
    {
        if (!EnsureReferences("능력치 경계값"))
            return;

        player.ResetPlayer();
        float sourceAttackSpeed = State.Source.AttacksPerSecond;
        float sourceDashCooldown = State.Source.DashRechargeCooldown;

        State.SetAttacksPerSecond(float.MinValue);
        bool attackMinimum = Mathf.Approximately(State.AttacksPerSecond, sourceAttackSpeed * 0.3f);
        State.SetAttacksPerSecond(float.MaxValue);
        bool attackMaximum = Mathf.Approximately(State.AttacksPerSecond, sourceAttackSpeed * 1.7f);

        State.SetDashRechargeCooldown(float.MinValue);
        bool dashMinimum = Mathf.Approximately(State.DashRechargeCooldown, sourceDashCooldown * 0.3f);
        State.SetDashRechargeCooldown(float.MaxValue);
        bool dashMaximum = Mathf.Approximately(State.DashRechargeCooldown, sourceDashCooldown * 1.7f);

        State.SetMoveSpeed(-1f);
        State.SetAttackPower(-1f);
        State.SetActiveSkillProjectileSpeed(-1f);
        State.SetActiveSkillProjectileWidth(-1f);
        State.SetActiveSkillCastTime(100f);
        bool safeLimits = Mathf.Approximately(State.MoveSpeed, 0f)
            && Mathf.Approximately(State.AttackPower, 0f)
            && Mathf.Approximately(State.ActiveSkillProjectileSpeed, 0.01f)
            && Mathf.Approximately(State.ActiveSkillProjectileWidth, 0.1f)
            && Mathf.Approximately(State.ActiveSkillCastTime, 5f);

        AppendResult(
            "공속·대시 쿨타임 ±70% 및 안전 경계값",
            attackMinimum && attackMaximum && dashMinimum && dashMaximum && safeLimits);
        player.ResetPlayer();
    }

    /// <summary>검증에 필요한 모든 인스펙터 참조와 RuntimeState가 준비됐는지 확인합니다.</summary>
    private bool EnsureReferences(string checkName)
    {
        bool valid = State != null
            && inputReader != null
            && locomotion != null
            && health != null
            && basicAttack != null
            && warriorActiveSkill != null;

        if (!valid)
            AppendResult(checkName + " 참조 연결", false);
        return valid;
    }

    /// <summary>항목 결과를 PASS/FAIL 한 줄로 기록하고 집계를 갱신합니다.</summary>
    private void AppendResult(string label, bool passed)
    {
        if (passed)
            passedCount++;
        else
            failedCount++;

        resultBuilder.Append(passed ? "[PASS] " : "[FAIL] ");
        resultBuilder.AppendLine(label);
        RefreshResults();
    }

    /// <summary>모든 항목 뒤에 전체 PASS/FAIL 개수를 덧붙입니다.</summary>
    private void AppendSummary()
    {
        resultBuilder.AppendLine();
        resultBuilder.Append("결과: PASS ");
        resultBuilder.Append(passedCount);
        resultBuilder.Append(" / FAIL ");
        resultBuilder.Append(failedCount);
        RefreshResults();
    }

    private void RefreshResults()
    {
        if (validationResultOutput != null)
            validationResultOutput.text = resultBuilder.ToString();
    }

    private string GetDashState() { return locomotion != null && locomotion.IsDashing ? "진행 중" : "대기"; }
    private int GetDashCharges() { return locomotion != null ? locomotion.DashCharges : 0; }
    private float GetBasicAttackCooldown() { return basicAttack != null ? basicAttack.CooldownRemaining : 0f; }
    private bool GetManualAim() { return basicAttack != null && basicAttack.IsManualAim; }
    private string GetTargetName() { return basicAttack != null ? basicAttack.CurrentTargetName : "None"; }
    private bool GetSkillAiming() { return warriorActiveSkill != null && warriorActiveSkill.IsAiming; }
    private bool GetSkillCasting() { return warriorActiveSkill != null && warriorActiveSkill.IsCasting; }
    private float GetSkillCooldown() { return warriorActiveSkill != null ? warriorActiveSkill.CooldownRemaining : 0f; }
    private int GetPoolActive() { return warriorActiveSkill != null ? warriorActiveSkill.PoolActiveCount : 0; }
    private int GetPoolInactive() { return warriorActiveSkill != null ? warriorActiveSkill.PoolInactiveCount : 0; }
    private int GetPoolTotal() { return warriorActiveSkill != null ? warriorActiveSkill.PoolTotalCount : 0; }
}
