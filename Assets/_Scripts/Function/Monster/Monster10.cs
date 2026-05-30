using System.Collections;
using UnityEngine;

public class Monster10 : MonsterBase
{
    float timer = 0f;
    public float tolerance = 2f;

    [Header("Mark Bomb")]
    public GameObject telegraphPrefab;
    public float markDelay = 1.2f;        // 표식 생성 후 폭발까지 지연 시간
    public float explodeRadius = 3.5f;    // 폭발 반경
    public float stunDuration = 1.0f;     // 스턴 지속 시간
    public LayerMask groundMask;          // 바닥 레이어

    [Header("Damage")]
    public float damageMultiplier = 1f;   // 최종 피해량 = md.attackDamage * 배율

    bool casting = false;

    protected override void Idle()
    {
        agent.isStopped = true;
        if (player == null) return;

        if (Vector3.Distance(transform.position, player.transform.position) <= detectRange)
            state = Enums.MonsterState.Move;
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);

        if (dis > md.attackRange + tolerance)
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
        }
    }

    protected override void Attack()
    {
        if (player == null || md == null || casting) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis > md.attackRange + tolerance)
        {
            state = Enums.MonsterState.Move;
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer < md.attackSpeed) return;

        // 현재 플레이어 위치에 표식 생성
        Vector3 targetPos = player.transform.position;

        StartCoroutine(CoMarkExplode(targetPos));

        timer = 0f;
    }

    IEnumerator CoMarkExplode(Vector3 pos)
    {
        casting = true;

        if (telegraphPrefab == null) { casting = false; yield break; }

        // 바닥 보정 (groundMask 사용)
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out var hit, 20f, groundMask)) pos = hit.point;

        GameObject tg = Instantiate(telegraphPrefab, pos, Quaternion.Euler(0f, 0f, 0f));

        yield return new WaitForSeconds(markDelay);

        int playerMask = LayerMask.GetMask("Player");
        Collider[] hits = Physics.OverlapSphere(pos, explodeRadius, playerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Player p = hits[i].GetComponentInParent<Player>();
            if (p == null) continue;

            float dmg = md.attackDamage * damageMultiplier;
            p.TakeDamage(dmg);
            p.Stun(stunDuration);
        }

        if (tg != null) Destroy(tg);
        casting = false;
    }

}
