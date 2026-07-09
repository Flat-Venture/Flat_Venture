using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 원거리 몬스터용 이동 파츠. 타겟과 일정 거리를 유지
/// </summary>
public class RangedMovementTrait : MonsterTrait
{
    [Header("Ranged Movement Settings")]
    [Tooltip("이 거리 안으로 들어오면 이동을 멈추고 공격 대기")]
    public float keepDistance = 8f;
    
    [Tooltip("플레이어가 이 거리보다 가까워지면 뒤로 도망감 (카이팅)")]
    public float kiteDistance = 4f;

    private NavMeshAgent agent; 

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = controller.moveSpeed;
    }

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.IsStunned || controller.CurrentTarget == null)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

        if (distance > keepDistance)
        {
            //너무 멀면 다가감
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(controller.CurrentTarget.position);
            }
        }
        else if (distance < kiteDistance)
        {
            //너무 가까우면 뒤로 도망감 (카이팅)
            Vector3 directionAway = (transform.position - controller.CurrentTarget.position).normalized;
            Vector3 kiteTarget = transform.position + (directionAway * 2f);
            
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(kiteTarget);
            }
        }
        else
        {
            //적당한 거리(사거리)면 멈춰서 공격 준비
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            
            //멈춰서 쏠 때 타겟을 자연스럽게 바라보도록 회전
            Vector3 lookDir = (controller.CurrentTarget.position - transform.position).normalized;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }
    }
}