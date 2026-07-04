using UnityEngine;

public class MeleeAttackTrait : MonsterTrait
{
    [Header("Melee Attack Settings")]
    [Tooltip("공격 최대 사거리")]
    public float attackRange = 2.0f;
    [Tooltip("공격 쿨타임(초)")]
    public float attackCooldown = 1.5f;
    [Tooltip("공격력")]
    public float attackDamage = 10.0f;
    
    private float lastAttackTime;

    private void Update()
    {
        if (controller == null || controller.CurrentTarget == null) return;

        //타겟과의 거리 계산
        float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

        //사거리 내에 들어왔고, 쿨타임이 돌았으면 공격
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown) ExecutMeleeAttack();
    }

    private void ExecutMeleeAttack()
    {
        lastAttackTime = Time.time;
        Debug.Log($"<color=red>[Monster Attack]</color> {gameObject.name}이(가) {controller.CurrentTarget.name}에게 {attackDamage} 근접 피해를 입힘");
        
        // TODO: 플레이어 데미지 함수 호출 등
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
