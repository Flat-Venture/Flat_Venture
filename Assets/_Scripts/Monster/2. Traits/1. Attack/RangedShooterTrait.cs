using UnityEngine;

/// <summary>
/// 원거리형 역할군을 위한 기본 특성 클래스
/// </summary>
public class RangedShooterTrait : MonsterTrait
{
    [Header("Attack Settings")]
    [Tooltip("원거리 공격 사거리 (이 거리 안에 플레이어가 들어와야 쏩니다)")]
    public float attackRange = 10f;

    [Tooltip("공격 쿨타임 (공격 속도)")]
    public float attackCooldown = 2.5f;

    [Header("Shooter Settings")]
    [Tooltip("발사할 투사체(총알/마법 등) 프리팹")]
    public GameObject projectilePrefab;

    [Tooltip("투사체가 발사될 위치 (비워두면 몬스터 몸통 중앙에서 발사)")]
    public Transform firePoint;

    [Tooltip("투사체 날아가는 속도")]
    public float projectileSpeed = 12f;

    private float nextFireTime = 0f;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);

        //시작하자마자 바로 쏘지 않도록 첫 쿨타임 적용
        nextFireTime = Time.time + attackCooldown;
    }

    private void Update()
    {
        //몬스터가 살아있고, 스턴(넉백) 상태가 아니며, 쫓고 있는 타겟(플레이어)이 있을 때만 작동
        if (controller != null && controller.IsAlive && !controller.IsStunned && controller.CurrentTarget != null)
        {
            //쿨타임이 다 돌았으면 발사
            if (Time.time >= nextFireTime)
            {
                ShootProjectile();

                //쿨타임 리셋
                nextFireTime = Time.time + attackCooldown; 
            }
        }
    }

    private void ShootProjectile()
    {
        //투사체가 설정되어 있지 않으면 발사하지 않음
        if (projectilePrefab == null) return;

        //발사 위치 설정 (firePoint가 설정되어 있으면 자신의 위치에서 살짝 위로)
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + (Vector3.up * 1f);

        //플레이어를 향하는 방향 벡터 계산
        Vector3 direction = (controller.CurrentTarget.position - spawnPosition).normalized;

        //땅과 평행하게 날아가도록 y축 고정 (포물선 필요시 해당 부분 수정)
        direction.y = 0;

        //투사체 생성 및 방향 바라보기
        GameObject project = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        //물리적인 힘(속도)을 가해 날려보냄
        Rigidbody rb = project.GetComponent<Rigidbody>();

        if (rb != null) rb.linearVelocity = direction * projectileSpeed;
        else Debug.LogWarning("투사체 프리팹에 Rigidbody가 없어 날아가지 않습니다");

        Debug.Log($"<color=cyan>[Ranged Attack]</color> {gameObject.name}이(가) 투사체를 발사했습니다");
    }

    /// <summary>
    /// 공격 범위 기즈모
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
