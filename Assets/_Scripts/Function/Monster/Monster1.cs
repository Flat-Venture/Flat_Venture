using UnityEngine;

/// <summary>
/// 근접 몬스터 패턴1
/// </summary>
public class Monster1 : MonsterBase
{
    float timer = 0f;
    int playerLayer;

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
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(player.transform.position);
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
        agent.isStopped = true;
        agent.updateRotation = false;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis > md.attackRange)
        {
            agent.updateRotation = true;
            state = Enums.MonsterState.Move;
            //anim.SetTrigger("Move");

            return;
        }
        timer += Time.deltaTime;

        if (timer < md.attackSpeed) return;
        Debug.Log("공격1");
        //anim.SetTrigger("Attack");

        timer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == playerLayer)
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(this.md.attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        if (md != null) Gizmos.DrawWireSphere(transform.position, md.attackRange);
    }
}

