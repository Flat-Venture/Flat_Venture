using UnityEngine;

/// <summary>
/// 몬스터가 원거리 투사체에 맞았을 때 이를 반사하는 특성
/// </summary>
public class ProjectileReflectorTrait : MonsterTrait
{
    [Header("Reflector Settings")]
    [Tooltip("반사할 확률 (0~1)")]
    public float reflectChance = 0.5f;

    /// <summary>
    /// 몬스터가 데미지를 입을 때 호출되는 함수를 활용하여 반사 로직을 구현합니다
    /// </summary>
    public override void OnTakeDamage(float damage)
    {
        base.OnTakeDamage(damage);

        //확률적으로 반사 로직 실행
        if (Random.value <= reflectChance)
        {
            ReflectProjectile();
        }
    }

    private void ReflectProjectile()
    {
        //몬스터 위치에서 플레이어 방향으로 레이캐스트를 쏴서 반사할 투사체를 찾습니다
        Collider[] hits = Physics.OverlapSphere(transform.position, 2.0f);
        foreach (var hit in hits)
        {
            //투사체 컴포넌트인 PlayerBasicAttackProjectile을 찾습니다
            PlayerBasicAttackProjectile projectile = hit.GetComponent<PlayerBasicAttackProjectile>();
            
            if (projectile != null)
            {
                //Rigidbody의 속도를 반전시킵니다
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = -rb.linearVelocity;
                    Debug.Log("// 투사체가 반사되었습니다");
                }
            }
        }
    }
}