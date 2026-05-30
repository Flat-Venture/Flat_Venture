using UnityEngine;

public class Monster9 : MonsterBase
{
    float timer = 0f;
    public float tolerance = 2f;

    [Header("Arc Throw")]
    public MonsterArcProjectile arcProjectilePrefab;
    public float arcHeight = 5f;

    PoolManager pool;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        pool = PoolManager.Instance;
        // 몬스터 투사체 풀 생성
        pool.CreatePool<MonsterArcProjectile>(Enums.PoolType.MonsterArcProjectile, arcProjectilePrefab, 5, null);
    }

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
        if (player == null || md == null) return;

        timer += Time.deltaTime;
        if (timer < md.attackSpeed) return;

        var proj = pool.Get<MonsterArcProjectile>(Enums.PoolType.MonsterArcProjectile);

        Vector3 shootPos = firepoint != null ? firepoint.position : transform.position;
        Vector3 targetPos = player.transform.position; 

        proj.Init(this, shootPos, targetPos, arcHeight);

        timer = 0f;
    }
}
