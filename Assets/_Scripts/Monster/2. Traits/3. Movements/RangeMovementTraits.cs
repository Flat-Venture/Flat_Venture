using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 원거리 몬스터용 이동 파츠. 타겟과 일정 거리를 유지
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class RangedMovementTrait : MonsterTrait
{
    [Header("Ranged Movement Settings")]
    [Tooltip("이 거리 안으로 들어오면 이동을 멈추고 공격 대기")]
    public float keepDistance = 8f;

    [Tooltip("플레이어가 이 거리보다 가까워지면 뒤로 도망감 (카이팅)")]
    public float kiteDistance = 4f;

    private NavMeshAgent agent;
    private Rigidbody rb;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        //물리 엔진이 정상 작동하도록 강제 설정
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;

            //내장 정지 거리를 0으로 둬서 스크립트가 온전히 거리를 제어하게 함
            agent.stoppingDistance = 0f;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                //Y축 오차로 인한 땅 파묻힘을 방지하기 위해 살짝 위로 띄워서 안착
                transform.position = hit.position + (Vector3.up * 0.1f);
            }
            agent.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        //타겟이 없으면 멈춤
        if (controller == null || !controller.IsAlive || controller.IsStunned || controller.CurrentTarget == null)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }

        //공격 중(선딜/후딜)이라면 이동을 즉시 멈추고 제자리에 고정
        if (controller.IsAttacking)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;

            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            //공격 중일 때 타겟 방향으로 고개를 돌리도록 유지 (자연스러움)
            Vector3 attackLookDir = (controller.CurrentTarget.position - transform.position).normalized;
            attackLookDir.y = 0;

            if (attackLookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(attackLookDir), Time.fixedDeltaTime * 10f);

            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

            //항상 타겟을 바라보도록 회전
            Vector3 lookDir = (controller.CurrentTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.fixedDeltaTime * 10f);
            }

            //거리에 따른 행동 패턴
            if (distance > keepDistance)
            {
                agent.SetDestination(controller.CurrentTarget.position);
                MoveWithPhysics();
            }

            else if (distance < kiteDistance)
            {
                Vector3 directionAway = (transform.position - controller.CurrentTarget.position).normalized;
                Vector3 kiteTarget = transform.position + (directionAway * 3f);
                agent.SetDestination(kiteTarget);
                MoveWithPhysics();
            }

            else
            {
                //사거리 안이면 정지
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }

            //물리 위치와 길찾기 위치 동기화
            agent.nextPosition = rb.position;
        }
    }

    private void MoveWithPhysics()
    {
        //길찾기 연산 중에도 멈추지 않고 즉시 움직이도록 유도
        if (rb == null || agent == null) return;

        //다음 목적지를 향한 방향 벡터
        Vector3 direction = (agent.steeringTarget - rb.position).normalized;
        direction.y = 0;

        //Rigidbody를 통해 물리적으로 이동
        rb.linearVelocity = direction * controller.moveSpeed;

        //씬 뷰에서 몬스터가 어디로 가려고 하는지 빨간 선으로 보여줌
        Debug.DrawLine(transform.position, agent.steeringTarget, Color.red);
    }
}