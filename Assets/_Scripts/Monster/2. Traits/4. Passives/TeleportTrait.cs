using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 체력이 일정 비율 이하로 떨어지면 무작위 위치로 순간이동하는 특성
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TeleportTrait : MonsterTrait
{
    [Header("Teleport Settings")]
    [Tooltip("순간이동이 발동할 체력 비율 (예: 0.33 = 33%)")]
    public float teleportHpRatio = 0.33f;
    [Tooltip("순간이동할 최대 거리")]
    public float teleportRadius = 10f;
    
    private NavMeshAgent agent;
    private bool hasTeleported = false;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        //살아있고, 아직 순간이동을 안 했으며, 컨트롤러가 연결되어 있을 때만 실행
        if (!hasTeleported && controller != null && controller.IsAlive)
        {
            //체력 비율 계산 (현재 체력 / 최대 체력)
            float hpRatio = controller.CurrentHP / controller.maxHP;
            
            if (hpRatio <= teleportHpRatio)
            {
                ExecuteTeleport();
            }
        }
    }

    private void ExecuteTeleport()
    {
        hasTeleported = true;
        
        //반경 내 랜덤한 위치 계산
        Vector3 randomDirection = Random.insideUnitSphere * teleportRadius;
        randomDirection += transform.position;

        //NavMesh 상의 유효한(갈 수 있는) 위치인지 확인 후 순간이동
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, teleportRadius, NavMesh.AllAreas))
        {
            if (agent != null)
            {
                agent.Warp(hit.position);
                Debug.Log($"<color=magenta>[Teleport]</color> {gameObject.name}이(가) 위협을 느껴 순간이동했습니다!");
                
                //TODO: 기획에 따라 체력 회복 기능이 필요하다면 아래 주석을 해제하고 컨트롤러에 Heal 함수를 추가하세요.
                //controller.Heal(controller.maxHP * 0.2f);
            }
        }
    }
}