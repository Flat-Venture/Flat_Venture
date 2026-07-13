using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh의 길찾기 지능과 Rigidbody의 물리 엔진을 결합한 하이브리드 이동 부품
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class MovementTrait : MonsterTrait
{
    [Header("Movement Settings")]
    [Tooltip("타겟에게 다가갈 때 유지할 최소 거리")]
    public float stoppingDistance = 1.5f;       //MeleeAttack의 사거리보다 살짝 짧게 설정

    private NavMeshAgent agent;
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        //NavMeshAgent가 물리 엔진을 무시하고 강제로 순간이동하는 것을 방지
        //뇌(Agent)는 길만 찾고, 실제 이동은 근육(Rigidbody)이 담당
        agent.updatePosition = false;
        agent.updateRotation = false;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        
        //이동이 끝난 후 에이전트 활성화
        agent.enabled = true;

    }

    private void FixedUpdate()
    {
        //넉백을 당해 기절 상태라면 움직이지 않음
        if (controller == null || controller.CurrentTarget == null || controller.IsStunned) return;

        //Agent에게 플레이어의 위치를 알려주고 경로 계산 지시
        agent.SetDestination(controller.CurrentTarget.position);

        //타겟 방향으로 고개를 돌림
        FlipSprite(controller.CurrentTarget.position);

        //타겟과의 거리를 계산
        float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

        //아직 사거리에 도달하지 않았다면 다가감
        if (distance > stoppingDistance) MoveWithPhysics();
        
        //사거리 안이면 미끄러지지 않게 정지
        else rb.linearVelocity = Vector3.zero;

        //타격(넉백)을 받아 밀려났을 때 에이전트의 위치도 현재 위치로 동기화
        agent.nextPosition = rb.position;
    }

    private void MoveWithPhysics()
    {
        //에이전트가 계산한 다음 목적지를 향한 방향 벡터 계산
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;
        
        //2.5D 평면 이동이므로 위아래(Y축) 이동 방지
        direction.y = 0; 

        //Rigidbody를 통해 물리적으로 이동
        rb.linearVelocity = direction * controller.moveSpeed;

        //TODO: 애니메이터의 "IsMoving" 파라미터를 true로 설정하는 로직 추가
    }

    private void FlipSprite(Vector3 targetPosition)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = targetPosition.x < transform.position.x;
    }
}