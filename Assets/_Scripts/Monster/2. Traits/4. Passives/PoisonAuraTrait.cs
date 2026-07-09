using UnityEngine;
using FlatVenture.NUH.Player.Combat;

/// <summary>
/// 몬스터 주변에 독 장판을 생성하여 플레이어에게 지속 피해를 입히는 특성
/// </summary>a
public class PoisonAuraTrait : MonsterTrait
{
    [Header("Poison Aura Settings")]
    public float auraRadius = 3.5f;
    public float poisonDamage = 2f;
    public float tickRate = 1f;

    private float nextTickTime;

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.CurrentTarget == null) return;

        if (Time.time > nextTickTime)
        {
            float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

            //타겟이 독 장판 범위 안에 있다면
            if (distance <= auraRadius)
            {
                //플레이어 시스템에 맞춰 IPlayerAttackTarget 인터페이스로 데미지 전달
                IPlayerAttackTarget target = controller.CurrentTarget.GetComponentInParent<IPlayerAttackTarget>();
                
                if (target != null && target.IsAlive)
                {
                    Debug.Log($"<color=green>[Poison Aura]</color> 플레이어가 맹독에 중독되어 {poisonDamage}의 피해를 입습니다!");
                    target.TakeDamage(poisonDamage);
                }
            }

            nextTickTime = Time.time + tickRate;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
