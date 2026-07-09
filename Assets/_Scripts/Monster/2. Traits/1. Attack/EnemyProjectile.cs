using UnityEngine;

/// <summary>
/// 적의 투사체를 담당하며, 플레이어 피격 처리 및 스스로 풀(Pool)로 돌아가는 기능
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("총알이 날아갈 최대 수명 (초) - 안 맞고 계속 날아가면 메모리 낭비이므로")]
    public float lifeTime = 3f;
    
    private float damage;
    private Rigidbody rb;
    private float enableTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        //켜질 때마다 수명 타이머 초기화
        enableTime = Time.time;
    }

    private void Update()
    {
        //수명이 다 되면 알아서 비활성화 (풀로 돌아감)
        if (Time.time > enableTime + lifeTime)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// RangedShooterTrait에서 발사할 때 데미지를 세팅해줌
    /// </summary>
    public void SetupDamage(float attackDamage)
    {
        damage = attackDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        //플레이어와 충돌했는지 확인 (IPlayerAttackTarget 인터페이스 활용 가능)
        if (other.CompareTag("Player"))
        {
            Debug.Log($"<color=red>[Projectile]</color> 플레이어에게 {damage} 데미지 줌!");
            //TODO: 플레이어의 TakeDamage(damage) 함수 호출
            
            ReturnToPool();
        }

        //벽이나 바닥 등 다른 장애물에 맞았을 때도 비활성화
        else if (other.CompareTag("Environment") || other.CompareTag("Obstacle"))
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        //쏘는 쪽(RangedShooterTrait)에서 꺼져있는 총알을 찾아 다시 킴
        rb.linearVelocity = Vector3.zero; //남은 관성 제거
        gameObject.SetActive(false); 
    }
}