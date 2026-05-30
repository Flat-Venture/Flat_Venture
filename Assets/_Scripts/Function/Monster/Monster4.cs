using UnityEngine;
using System.Collections;

/// <summary>
/// 근접 몬스터 패턴2
/// </summary>
public class Monster4 : MonsterBase
{
    float timer = 0f;
    float duration = 5f;
    
    protected override void Idle()
    {
        agent.isStopped = true;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis <= detectRange)
        {
            state = Enums.MonsterState.Move;
            //anim.SetTrigger("Move");
        }
    }

    protected override void Move()
    {
        if (player == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);

        if (dis > md.attackRange)
        {
            //agent.isStopped = false;
            agent.updateRotation = true;
            //agent.SetDestination(player.transform.position);
        }
        else
        {
            agent.isStopped = true;
            agent.updateRotation = false;

            state = Enums.MonsterState.Attack;

            timer = md.attackSpeed;
        }
    }
    protected override void Attack()
    {
        if (isDef) return;
        //agent.isStopped = true;
        agent.updateRotation = false;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        
        if (dis > md.attackRange)
        {
            agent.updateRotation = true;
            state = Enums.MonsterState.Move;
            //anim.SetTrigger("Move");

            return;
        }

        StartCoroutine(Def());
        timer = 0f;
    }

    IEnumerator Def()
    {
        isDef = true;
        while(timer < duration)
        {
            timer += Time.deltaTime;
            state = Enums.MonsterState.Attack;
            //anim.SetTrigger("Attack");
            //Debug.Log("공격2");
        }
        yield return new WaitForSeconds(duration);
        isDef = false;
    }

    public override void TakeDamage(float dmg)
    {
        Vector3 dir = (player.transform.position - transform.position).normalized;

        float dot = Vector3.Dot(transform.forward, dir);

        if (dot > 0)  // 정면
        {
            float frontDmg = dmg * 0.2f;  // 80% 감소
            currentHp -= frontDmg;
        }

        else  // 후면
        {
            currentHp -= dmg;
        }

        if (currentHp <= 0) Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        if (md != null) Gizmos.DrawWireSphere(transform.position, md.attackRange);
    }
}

