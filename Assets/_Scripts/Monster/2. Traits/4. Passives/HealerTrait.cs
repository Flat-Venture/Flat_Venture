using UnityEngine;

public class HealerTrait : MonsterTrait
{
    [Header("Heal Settings")]
    public float healRadius = 8f;
    public float healAmount = 15f;
    public float healCooldown = 6f;

    [Header("Targeting")]
    [Tooltip("아군 몬스터로 인식할 레이어 (예: Monster)")]
    public LayerMask allyLayer;

    private float nextHealTime;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        nextHealTime = Time.time + healCooldown;
    }

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.IsStunned) return;

        if (Time.time >= nextHealTime)
        {
            CastHeal();
            nextHealTime = Time.time + healCooldown;
        }
    }

    private void CastHeal()
    {
        Debug.Log($"<color=cyan>[Heal Cast]</color> {gameObject.name}이(가) 광역 힐을 시전합니다.");

        //LayerMask를 사용하여 'allyLayer'에 해당하는 오브젝트만 검사
        Collider[] colliders = Physics.OverlapSphere(transform.position, healRadius, allyLayer);
        foreach (Collider col in colliders)
        {
            MonsterController ally = col.GetComponent<MonsterController>();

            //자기 자신이거나 아군 몬스터라면 회복
            if (ally != null && ally.IsAlive)
            {
                Debug.Log($"<color=cyan>[Heal]</color> {ally.gameObject.name}의 체력이 {healAmount}만큼 회복되었습니다.");
                // TODO: ally.Heal(healAmount) 등 몬스터 체력 회복 함수 연결
            }
        }
    }

    /// <summary>
    /// 힐 범위 기즈모
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
