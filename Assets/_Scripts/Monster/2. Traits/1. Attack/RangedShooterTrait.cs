using UnityEngine;
using System.Collections.Generic;

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

    [Tooltip("투사체의 데미지")]
    public float projectileDamage = 10f;

    [Header("Shooter Settings")]
    [Tooltip("발사할 투사체(총알/마법 등) 프리팹")]
    public GameObject projectilePrefab;

    [Tooltip("투사체가 발사될 위치 (비워두면 몬스터 몸통 중앙에서 발사)")]
    public Transform firePoint;

    [Tooltip("투사체 날아가는 속도")]
    public float projectileSpeed = 12f;

    [Tooltip("미리 만들어둘 총알 개수(오브젝트 풀링)")]
    public int poolSize = 10;

    private float nextFireTime = 0f;
    private List<GameObject> projectilePool = new List<GameObject>();

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);

        //시작하자마자 바로 쏘지 않도록 첫 쿨타임 적용
        nextFireTime = Time.time + attackCooldown;

        //게임 시작 시 총알을 미리 만들어두고 끔
        if (projectilePrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject gameObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                
                //비활성화 상태로 대기
                gameObject.SetActive(false);
                projectilePool.Add(gameObject);
            }
        }
    }

    private void Update()
    {
        //몬스터가 살아있고, 스턴(넉백) 상태가 아니며, 쫓고 있는 타겟(플레이어)이 있을 때만 작동
        if (controller != null && controller.IsAlive && !controller.IsStunned && controller.CurrentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, controller.CurrentTarget.position);

            //거리 체크 조건 추가 및 쿨타임이 다 돌았으면 발사
            if (distanceToTarget <= attackRange && Time.time >= nextFireTime)
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
        
        //보관함에서 꺼져 있는 총알 찾기
        GameObject bullet = null;
        
        foreach (var b in projectilePool)
        {
            if (!b.activeInHierarchy)
            {
                bullet = b;
                break;
            }
        }

        //만약 설정한 풀 사이즈를 넘어서 쏠 경우에만 새로 생성 (안전 장치)
        if (bullet == null)
        {
            bullet = Instantiate(projectilePrefab);
            projectilePool.Add(bullet);
        }

        //발사 위치 설정 (firePoint가 설정되어 있으면 자신의 위치에서 살짝 위로)
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + (Vector3.up * 1f);

        //플레이어를 향하는 방향 벡터 계산
        Vector3 direction = (controller.CurrentTarget.position - spawnPosition).normalized;

        //땅과 평행하게 날아가도록 y축 고정 (포물선 필요시 해당 부분 수정)
        direction.y = 0;

        //투사체 위치 및 회전 설정 후 활성화 (Instantiate 대체)
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.LookRotation(direction);
        bullet.SetActive(true);

        //물리적인 힘(속도)을 가해 날려보냄
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direction * projectileSpeed;
        else Debug.LogWarning("투사체 프리팹에 Rigidbody가 없어 날아가지 않습니다");

        //투사체에 데미지 세팅
        EnemyProjectile projScript = bullet.GetComponent<EnemyProjectile>();
        if (projScript != null) projScript.SetupDamage(projectileDamage);

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