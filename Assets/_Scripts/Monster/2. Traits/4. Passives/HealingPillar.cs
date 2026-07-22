using UnityEngine;
using System.Collections;
public class HealingPillar : MonoBehaviour, IPlayerAttackTarget
{
    public float maxHP = 50f;
    public float healAmount = 5f;
    public float healInterval = 3f;

    private float currentHP;
    private MonsterController bossController;

    //인터페이스 구현부
    public Transform TargetTransform => transform;
    public bool IsAlive => currentHP > 0;

    public void Setup(MonsterController targetBoss)
    {
        currentHP = maxHP;
        bossController = targetBoss;
        
        //생성 즉시 회복 루틴을 시작
        StartCoroutine(HealRoutine());
    }

    public void TakeDamage(float damage)
    {
        //이미 파괴된 상태면 로직을 무시
        if (currentHP <= 0) return;

        currentHP -= damage;

        //체력이 모두 소진되면 오브젝트를 파괴하여 회복을 멈춤
        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator HealRoutine()
    {
        //기둥 본체와 보스가 모두 살아있는 동안에만 주기적으로 회복
        while (IsAlive && bossController != null && bossController.IsAlive)
        {
            yield return new WaitForSeconds(healInterval);
            bossController.Heal(healAmount);
        }
    }
}