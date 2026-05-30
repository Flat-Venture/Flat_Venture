using System.Collections;
using UnityEngine;

public class Monster3 : MonsterBase
{
    MonsterCharging mc;

    [Header("폭발 범위")]
    public float explosionRanege = 10f;
    public LayerMask Player;

    float timer = 0f;

    void Start()
    {
        mc = GetComponent<MonsterCharging>();
    }

    protected override void Idle()
    {
        agent.isStopped = true;

        if (player == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis <= detectRange)
        {
            state = Enums.MonsterState.Move;
            //anim.SetTrigger("Move");
        }
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

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
            timer = 0f; 

            if (mc != null) mc.StartCharge();

            //anim.SetTrigger("Attack");
        }
    }

    protected override void Attack()
    {
        if (player == null || md == null) return;

        // 자폭 중에는 제자리 고정
        agent.isStopped = true;
        agent.updateRotation = false;

        float dis = Vector3.Distance(transform.position, player.transform.position);

        timer += Time.deltaTime;
        if (timer < md.attackSpeed) return;

        //Debug.Log("자폭");
        Explode();
    }

    private void Explode()
    {
        //anim.SetTrigger("Explode");

        // 폭발 범위 내 플레이어 찾기
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRanege, Player);

        foreach (var hit in hits)
        {
            var hp = hit.GetComponent<Player>(); 
            if (hp != null)
            {
                hp.TakeDamage(md.attackDamage);
            }
        }

        if (mc != null)
        {
            mc.EndCharge();
        }

        state = Enums.MonsterState.Die;
        Die();
    }

    private void OnDrawGizmosSelected()
    {
        //감지 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // 폭발 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRanege);

        // 공격을 시작하는 범위
        if (md != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, md.attackRange);
        }
    }
}
