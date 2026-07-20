using UnityEngine;

/// <summary>
/// 몬스터가 죽을 때 폭발하여 주변 플레이어에게 데미지를 주는 특성
/// </summary>
public class DeathExplosionTrait : MonsterTrait
{
    [Header("Explosion Settings")]
    [Tooltip("폭발 피해 범위")]
    public float explosionRadius = 3f;
    [Tooltip("폭발 시 플레이어가 입는 데미지")]
    public float explosionDamage = 20f;
    [Tooltip("폭발 이펙트 프리팹 (선택사항)")]
    public GameObject explosionEffectPrefab;

    /// <summary>
    /// MonsterController에서 몬스터 체력이 0이 될 때 호출해주는 함수
    /// </summary>
    public override void OnDeath()
    {
        base.OnDeath();
        Explode();
    }

    private void Explode()
    {
        Debug.Log($"<color=red>[Death Explosion]</color> {gameObject.name}이(가) 사망하며 폭발합니다!");

        //폭발 이펙트 생성 (설정되어 있을 경우)
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        //폭발 범위 내의 콜라이더들을 검사합니다
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            //플레이어 태그를 가졌다면 데미지 전달
            if (col.CompareTag("Player"))
            {
                PlayerHealthController healthController = col.GetComponentInParent<PlayerHealthController>();
                if (healthController != null)
                {
                    healthController.TakeDamage(explosionDamage);
                    Debug.Log($"<color=red>[Death Explosion]</color> 플레이어에게 {explosionDamage}의 폭발 피해를 주었습니다!");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 폭발 범위를 빨간색 원으로 보여줍니다
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}