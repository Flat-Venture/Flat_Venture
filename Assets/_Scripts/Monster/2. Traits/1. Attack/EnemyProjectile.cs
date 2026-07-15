using UnityEngine;
using FlatVenture.NUH.Player.Health;

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
        //부딪힌 오브젝트(또는 그 부모)에 플레이어 체력 스크립트가 있는지 확인합니다.
        PlayerHealthController healthController = other.GetComponentInParent<PlayerHealthController>();
        
        //플레이어라면 데미지를 주고 즉시 총알 파괴
        if (healthController != null)
        {
            Debug.Log($"<color=red>[Projectile]</color> 플레이어에게 {damage} 데미지 명중!");
            healthController.TakeDamage(damage);
            ReturnToPool(); 
            return;
        }

        //플레이어가 아닌 벽이나 바닥 등 장애물에 맞았을 때도 파괴
        if (other.CompareTag("Player") || other.CompareTag("Environment"))
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