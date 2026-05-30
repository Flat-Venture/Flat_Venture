using UnityEngine;
using static Enums;


public class Monster8 : MonsterBase
{
    [Header("Jump Attack")]
    public float jumpMinDistance = 2.5f;   // 너무 가까우면 점프하지 않음
    public float jumpMaxDistance = 6.0f;   // 이 거리 이내일 때 점프 시도
    public float windupTime = 0.25f;       // 점프 전 준비 시간
    public float airTime = 0.45f;          // 공중 이동 시간
    public float landingRecovery = 0.25f;  // 착지 후 경직 시간
    public float landingHitRadius = 1.8f;  // 착지 공격 판정 반경
    public float jumpArcHeight = 1.5f;     // 점프 포물선 높이

    float timer = 0f;
    JumpPhase phase = JumpPhase.None;

    Vector3 jumpStartPos;
    Vector3 jumpTargetPos;   // 점프 시작 시 목표 좌표 저장
    float phaseStartTime;

    int playerLayer;

    protected override void Awake()
    {
        base.Awake();
        playerLayer = LayerMask.NameToLayer("Player");
    }

    protected override void Idle()
    {
        agent.isStopped = true;

        if (player == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis <= detectRange)
        {
            state = Enums.MonsterState.Move;
        }
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);

        // 점프 공격 조건: 지정된 거리 범위 내
        if (dis <= jumpMaxDistance && dis >= jumpMinDistance)
        {
            agent.isStopped = true;
            agent.updateRotation = false;

            state = Enums.MonsterState.Attack;
            phase = JumpPhase.Windup;
            phaseStartTime = Time.time;

            jumpStartPos = transform.position;
            jumpTargetPos = player.transform.position;

            return;
        }

        // 점프 조건이 아니면 일반 이동 / 추적
        if (dis > md.attackRange)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(player.transform.position);
        }
        else
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }
    }

    protected override void Attack()
    {
        if (player == null || md == null) return;

        // Attack 상태에서는 점프 단계별 로직 처리
        switch (phase)
        {
            case JumpPhase.Windup:
                HandleWindup();
                break;
            case JumpPhase.Air:
                HandleAir();
                break;
            case JumpPhase.LandRecovery:
                HandleLandRecovery();
                break;
            default:
                // 예외 처리
                phase = JumpPhase.None;
                state = Enums.MonsterState.Move;
                break;
        }
    }

    void HandleWindup()
    {
        //agent.isStopped = true;
        agent.enabled = false;

        // anim.SetTrigger("Jump");

        if (Time.time - phaseStartTime >= windupTime)
        {
            phase = JumpPhase.Air;
            phaseStartTime = Time.time;
        }
    }

    void HandleAir()
    {
        float t = Mathf.Clamp01((Time.time - phaseStartTime) / airTime);
        Vector3 targetPos = new Vector3(jumpTargetPos.x, jumpStartPos.y, jumpTargetPos.z);

        // 목표는 jumpTargetPos 고정 (중간에 플레이어 위치는 갱신하지 않음)
        Vector3 flatPos = Vector3.Lerp(jumpStartPos, targetPos, t);

        // 포물선 이동 (중간에 가장 높고 시작/끝은 낮음)
        float arc = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
        Vector3 pos = new Vector3(flatPos.x, flatPos.y + arc, flatPos.z);

        transform.position = pos;

        if (t >= 1f)
        {
            // 착지
            DoLandingHitOnce();

            phase = JumpPhase.LandRecovery;
            phaseStartTime = Time.time;

        }
    }

    void HandleLandRecovery()
    {
        if (Time.time - phaseStartTime >= landingRecovery)
        {
            // NavMesh 재활성화
            agent.enabled = true;
            agent.isStopped = false;
            agent.updateRotation = true;

            phase = JumpPhase.None;
            state = Enums.MonsterState.Move;
        }
    }

    void DoLandingHitOnce()
    {
        // 착지 시 주변에 플레이어가 반경 안에 있으면 피해 적용
        Collider[] hits = Physics.OverlapSphere(transform.position, landingHitRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject.layer != playerLayer) continue;

            Player p = hits[i].GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(md.attackDamage);
                break;
            }
            //Debug.Log("공격");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        if (md != null) Gizmos.DrawWireSphere(transform.position, md.attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpMaxDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, landingHitRadius);
    }

}
