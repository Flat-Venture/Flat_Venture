using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DashBoss : BossBase
{
    public enum PatternState
    {
        None,
        JumpBack_Windup,
        JumpBack_Air,
        JumpBack_Recovery,

        Dash_Charge,
        Dash_Dashing,
        Dash_Recovery,

        Pull_Charge,
        Pull_Pulling,
        Pull_Recovery,
    }

    [Header("Distance Rules")]
    public float closeRange = 2.2f;     // 가까우면 무조건 백점프
    public float chaseStopRange = 4.5f; // 추적 멈추는 거리
    public float tolerance = 0f;

    [Header("Jump")]
    public float jumpBackWindup = 0.15f;
    public float jumpBackDuration = 0.35f;
    public float jumpBackRecovery = 0.2f;
    public float jumpBackDistance = 4f;
    public float jumpBackArcHeight = 1.2f;

    [Tooltip("플레이어가 가까워졌을 때 랜덤 회피 각도 범위")]
    public float randomJumpAngleRange = 160f;

    [Tooltip("점프 목표가 막혔을 때 재시도 횟수")]
    public int jumpTargetTries = 8;

    [Header("Dash")]
    public GameObject dashTelegraphPrefab;
    public LayerMask groundMask;
    public float dashWindup = 0.6f;
    public float dashTelegraphRadius = 2.5f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.5f;
    public float dashRecovery = 0.35f;
    public float dashHitRadius = 1.2f;
    public float dashStunDuration = 1.0f;
    public float dashDamageMultiplier = 1.0f;

    [Header("Pull")]
    public float pullRange = 8f;
    public float pullCooldown = 4f;
    public float pullChargeTime = 0.25f;
    public float pullDuration = 0.35f;
    public float pullStrength = 12f;
    public float pullAfterStun = 1.0f;

    [Header("Pull Effect")]
    public GameObject pullEffectPrefab;
    public float effectHeight = 0.05f; // 바닥에서 살짝 띄우기

    [Header("Pattern Random")]
    [Range(0f, 1f)] public float dashChance = 0.5f;

    [Header("Boss Stun")]
    public float selfStunDuration = 2.0f;

    float landingYOffset = 1f;

    PatternState pstate = PatternState.None;
    float nextPatternTime = 0f;

    // Jump
    Vector3 jumpStartPos;
    Vector3 jumpTargetPos;
    float phaseStartTime;

    // Dash
    Vector3 dashDir;
    float dashStartTime;
    bool dashHitDone;
    GameObject dashTelegraphInstance;

    // Pull
    float lastPull = -999f;
    bool pulling = false;

    // Pull 후 다음 패턴을 무조건 Dash로 강제
    bool forceDashNext = false;

    // Stun gage
    float currentStunGage = 0f;

    int playerLayer;

    protected override void Awake()
    {
        base.Awake();
        playerLayer = LayerMask.NameToLayer("Player");
    }

    protected override void Idle()
    {
        if (isStunned) return;

        agent.isStopped = true;
        if (player == null) return;

        if (Vector3.Distance(transform.position, player.transform.position) <= detectRange) state = Enums.MonsterState.Move;
    }

    protected override void Move()
    {
        if (isStunned) return;
        if (player == null || md == null) return;

        if (pstate != PatternState.None)
        {
            state = Enums.MonsterState.Attack;
            return;
        }

        float dis = Vector3.Distance(transform.position, player.transform.position);

        // 패턴 쿨타임 동안은 추적만
        if (Time.time < nextPatternTime)
        {
            Chase(dis);
            return;
        }

        // 가까우면 점프
        if (dis <= closeRange + tolerance)
        {
            StartRandomEvadeJump();
            return;
        }

        // Player 스턴 체크
        Player p = player.GetComponentInParent<Player>();
        bool playerStunned = (p != null && p.IsStunned);

        // 플레이어가 스턴 중이면 무조건 돌진
        if (playerStunned)
        {
            StartDashCharge();
            return;
        }

        // 그 외는 Pull / Dash 랜덤 (Pull은 쿨타임/거리 조건)
        bool canPull = (dis <= pullRange) && (Time.time >= lastPull + pullCooldown);

        // Pull이 불가능하면 무조건 Dash
        if (!canPull)
        {
            StartDashCharge();
            return;
        }

        // 랜덤 선택
        if (Random.value < dashChance)
            StartDashCharge();
        else
            StartPullCharge();
    }

    void Chase(float dis)
    {
        if (agent.enabled)
        {
            if (dis > chaseStopRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
            }
            else
            {
                agent.isStopped = true;
            }
        }

        state = Enums.MonsterState.Move;
    }

    protected override void Attack()
    {
        if (isStunned) return;

        switch (pstate)
        {
            case PatternState.JumpBack_Windup: HandleJumpBackWindup(); break;
            case PatternState.JumpBack_Air: HandleJumpBackAir(); break;
            case PatternState.JumpBack_Recovery: HandleJumpBackRecovery(); break;

            case PatternState.Dash_Charge: /* 코루틴이 처리 */ break;
            case PatternState.Dash_Dashing: HandleDashDashing(); break;
            case PatternState.Dash_Recovery: HandleDashRecovery(); break;

            case PatternState.Pull_Charge: /* 코루틴이 처리 */ break;
            case PatternState.Pull_Pulling: /* 코루틴이 처리 */ break;
            case PatternState.Pull_Recovery: HandlePullRecovery(); break;

            default:
                state = Enums.MonsterState.Move;
                break;
        }
    }

    #region Random Evade Jump
    void StartRandomEvadeJump()
    {
        PlaySfx("DashBossJump");

        StopAllCoroutines();

        // NavMesh 끊기
        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;

        pstate = PatternState.JumpBack_Windup;
        phaseStartTime = Time.time;

        jumpStartPos = transform.position;

        // 기본 기준: 플레이어 반대방향
        Vector3 baseDir = (transform.position - player.transform.position);
        baseDir.y = 0f;
        baseDir = baseDir.sqrMagnitude > 0.0001f ? baseDir.normalized : -transform.forward;

        bool found = false;

        for (int i = 0; i < jumpTargetTries; i++)
        {
            float ang = Random.Range(-randomJumpAngleRange, randomJumpAngleRange);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * baseDir;

            Vector3 cand = jumpStartPos + dir.normalized * jumpBackDistance;

            // 바닥 높이 맞추기
            if (Physics.Raycast(cand + Vector3.up * 5f, Vector3.down, out var hit, 30f, groundMask))
            {
                cand = hit.point;
                cand.y += landingYOffset;
            }

            // NavMesh 위 유효한 지점인지 체크
            if (NavMesh.SamplePosition(cand, out var navHit, 1.0f, NavMesh.AllAreas))
            {
                jumpTargetPos = navHit.position;
                jumpTargetPos.y += landingYOffset;
                found = true;
                break;
            }
        }

        // 못 찾으면 그냥 기준 방향으로라도
        if (!found)
        {
            jumpTargetPos = jumpStartPos + baseDir * jumpBackDistance;
            if (Physics.Raycast(jumpTargetPos + Vector3.up * 5f, Vector3.down, out var hit, 30f, groundMask))
            {
                jumpTargetPos = hit.point;
                jumpTargetPos.y += landingYOffset;
            }
        }

        state = Enums.MonsterState.Attack;
    }

    void HandleJumpBackWindup()
    {
        if (Time.time - phaseStartTime >= jumpBackWindup)
        {
            pstate = PatternState.JumpBack_Air;
            phaseStartTime = Time.time;
        }
    }

    void HandleJumpBackAir()
    {
        float t = Mathf.Clamp01((Time.time - phaseStartTime) / jumpBackDuration);

        Vector3 flat = Vector3.Lerp(jumpStartPos, jumpTargetPos, t);
        float arc = Mathf.Sin(t * Mathf.PI) * jumpBackArcHeight;

        float baseY = Mathf.Lerp(jumpStartPos.y, jumpTargetPos.y, t);
        transform.position = new Vector3(flat.x, baseY + arc, flat.z);

        if (t >= 1f)
        {
            pstate = PatternState.JumpBack_Recovery;
            phaseStartTime = Time.time;
        }
    }

    void HandleJumpBackRecovery()
    {
        if (Time.time - phaseStartTime >= jumpBackRecovery)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            EndPattern();
        }
    }
    #endregion

    #region Dash
    void StartDashCharge()
    {
        PlaySfx("DashBossDashCharge");

        StopAllCoroutines();

        agent.enabled = false;
        pstate = PatternState.Dash_Charge;
        state = Enums.MonsterState.Attack;

        Vector3 dir = (player.transform.position - transform.position);
        dir.y = 0f;
        dashDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        StartCoroutine(CoDashCharge());
    }

    IEnumerator CoDashCharge()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var hit, 10f, groundMask))
            pos = hit.point;

        if (dashTelegraphPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(dashDir, Vector3.up);
            dashTelegraphInstance = Instantiate(dashTelegraphPrefab, pos, rot);

            float d = dashTelegraphRadius * 2f;
            dashTelegraphInstance.transform.localScale = new Vector3(d, 1f, d);
        }

        yield return new WaitForSeconds(dashWindup);

        if (dashTelegraphInstance != null)
        {
            Destroy(dashTelegraphInstance);
            dashTelegraphInstance = null;
        }

        dashStartTime = Time.time;
        dashHitDone = false;

        PlaySfx("DashBossDash");

        pstate = PatternState.Dash_Dashing;
    }

    void HandleDashDashing()
    {
        if (Time.time - dashStartTime >= dashDuration)
        {
            pstate = PatternState.Dash_Recovery;
            phaseStartTime = Time.time;
            return;
        }

        transform.position += dashDir * dashSpeed * Time.deltaTime;

        if (!dashHitDone)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, dashHitRadius);
            foreach (var h in hits)
            {
                if (h.gameObject.layer != playerLayer) continue;
                Player p = h.GetComponentInParent<Player>();
                if (p == null) continue;

                p.TakeDamage(md.attackDamage * dashDamageMultiplier);
                p.Stun(dashStunDuration);

                PlaySfx("DashBossDashHit");

                dashHitDone = true;
                break;
            }
        }
    }

    void HandleDashRecovery()
    {
        if (Time.time - phaseStartTime >= dashRecovery)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            EndPattern();
        }
    }
    #endregion

    #region Pull
    void StartPullCharge()
    {
        PlaySfx("DashBossPull1");

        StopAllCoroutines();

        pstate = PatternState.Pull_Charge;
        state = Enums.MonsterState.Attack;

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        StartCoroutine(CoPull());
    }

    IEnumerator CoPull()
    {
        pulling = true;
        lastPull = Time.time;

        if (pullChargeTime > 0f) yield return new WaitForSeconds(pullChargeTime);

        GameObject effect = null;
        if (pullEffectPrefab != null)
        {
            Vector3 epos = transform.position;
            if (Physics.Raycast(epos + Vector3.up * 5f, Vector3.down, out var hit, 30f, groundMask)) epos = hit.point;

            epos.y += effectHeight;
            effect = Instantiate(pullEffectPrefab, epos, Quaternion.Euler(90f, 0f, 0f));
        }

        pstate = PatternState.Pull_Pulling;

        PlaySfx("DashBossPull2");

        Transform pt = player.transform;
        float t = 0f;

        while (t < pullDuration)
        {
            t += Time.deltaTime;

            Vector3 dir = (transform.position - pt.position);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                pt.position += dir * pullStrength * Time.deltaTime;
            }

            yield return null;
        }

        if (effect != null) Destroy(effect);

        // 풀링 후 스턴
        Player pcomp = player.GetComponentInParent<Player>();
        if (pcomp != null) pcomp.Stun(pullAfterStun);

        forceDashNext = true;

        pulling = false;

        pstate = PatternState.Pull_Recovery;
        phaseStartTime = Time.time;
    }

    void HandlePullRecovery()
    {
        if (Time.time - phaseStartTime >= 0.15f)
        {
            EndPattern();
        }
    }
    #endregion

    #region Stun Gauge
    public override void TakeDamage(float dmg)
    {
        base.TakeDamage(dmg);
        if (state == Enums.MonsterState.Die) return;

        PlaySfx("DashBossHit");

        if (md.stunGauge <= 0f) return;
        currentStunGage += md.stunGauge / 20f;

        if (currentStunGage >= md.stunGauge)
        {
            currentStunGage = 0f;
            StartCoroutine(CoSelfStun());
        }
    }

    protected override void Die()
    {
        PlaySfx("DashBossDie");
        base.Die();
    }

    IEnumerator CoSelfStun()
    {
        isStunned = true;
        StopAllCoroutines();

        if (dashTelegraphInstance != null)
        {
            Destroy(dashTelegraphInstance);
            dashTelegraphInstance = null;
        }

        agent.enabled = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(selfStunDuration);

        isStunned = false;
        agent.isStopped = false;
        nextPatternTime = Time.time + md.attackSpeed;
        state = Enums.MonsterState.Move;
    }
    #endregion

    void EndPattern()
    {
        pstate = PatternState.None;
        nextPatternTime = Time.time + md.attackSpeed;
        state = Enums.MonsterState.Move;
    }

    private void OnDisable()
    {
        if (dashTelegraphInstance != null) Destroy(dashTelegraphInstance);
    }
}
