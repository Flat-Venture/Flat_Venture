using UnityEngine;

public class Monster7 : MonsterBase
{
    [Header("Ranged Combat")]
    public float tolerance = 2f;
    float attackTimer = 0f;

    [Header("Defense Buff")]
    public float buffRange = 6f;
    public float damageTakenMultiplier = 0.8f; // 0.8 = 받는 피해 20% 감소
    public float buffDuration = 3f;
    public float buffTickInterval = 0.5f;
    public bool includeSelf = false;
    public LayerMask monster;

    [Header("Heal Range Visual")]
    public GameObject buffRangePrefab;
    public float buffVisualYOffset = 0.05f;

    GameObject buffRangeInstance;

    float buffTimer = 0f;

    protected override void Start()
    {
        base.Start();

        if (buffRangePrefab != null)
        {
            buffRangeInstance = Instantiate(buffRangePrefab, transform.position, Quaternion.identity, transform);

            buffRangeInstance.SetActive(false);
        }

    }
    void LateUpdate()
    {
        if (buffRangeInstance != null && buffRangeInstance.activeSelf) UpdateHealRangeVisual();
    }
    protected override void Idle()
    {
        agent.isStopped = true;
        if (player == null) return;

        if (Vector3.Distance(transform.position, player.transform.position) <= detectRange) state = Enums.MonsterState.Move;
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

        if (HasBuffTargetInRange())
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            state = Enums.MonsterState.Attack;
            buffTimer = 0f; attackTimer = 0f;
            return;
        }

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
            buffTimer = 0f; attackTimer = 0f;
        }
    }

    protected override void Attack()
    {
        if (player == null || md == null) return;

        agent.isStopped = true;
        bool isBuff = HasBuffTargetInRange();
        
        if(buffRangeInstance != null) buffRangeInstance.SetActive(isBuff);

        if (isBuff)
        {
            buffTimer += Time.deltaTime;
            if (buffTimer >= buffTickInterval)
            {
                ApplyDefenseBuffToAllies();
                buffTimer = 0f;
                // anim.SetTrigger("Buff");
            }

            if (Vector3.Distance(transform.position, player.transform.position) > md.attackRange + tolerance)
                state = Enums.MonsterState.Move;
            //Debug.Log("공격");
            return;
        }
        
        // 공격
        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis > md.attackRange + tolerance)
        {
            state = Enums.MonsterState.Move;
            attackTimer = 0f;
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer < md.attackSpeed) return;

        MonsterProjectile mp = GetMonsterProjectile();
        Vector3 shootPos = firepoint != null ? firepoint.position : transform.position;
        mp.transform.position = shootPos;

        Vector3 dir = (player.transform.position - shootPos).normalized;
        if (dir.sqrMagnitude > 0.0001f) mp.transform.rotation = Quaternion.LookRotation(dir);

        mp.Init(this, dir);
        attackTimer = 0f;
    }

    bool HasBuffTargetInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, buffRange, monster);
        for (int i = 0; i < hits.Length; i++)
        {
            MonsterBase ally = hits[i].GetComponentInParent<MonsterBase>();
            if (ally == null) continue;
            if (!includeSelf && ally == this) continue;
            if (ally.currentHp <= 0f) continue;
            if (ally.GetComponentInParent<BuffReceiver>() == null) continue;
            return true;
        }
        return false;
    }

    void ApplyDefenseBuffToAllies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, buffRange, monster);
        for (int i = 0; i < hits.Length; i++)
        {
            MonsterBase ally = hits[i].GetComponentInParent<MonsterBase>();
            if (ally == null) continue;
            if (!includeSelf && ally == this) continue;
            if (ally.currentHp <= 0f) continue;

            BuffReceiver r = ally.GetComponentInParent<BuffReceiver>();
            if (r == null) continue;

            r.ApplyDefenseBuff(this, damageTakenMultiplier, buffDuration); // 소스별 중첩
            //Debug.Log("방어버프");
        }
    }
    void UpdateHealRangeVisual()
    {
        if (buffRangeInstance == null) return;

        // 위치
        Vector3 pos = transform.position;
        pos.y += buffVisualYOffset;
        buffRangeInstance.transform.position = pos;

        // 지름 = healRange * 2
        float diameter = buffRange;
        buffRangeInstance.transform.localScale = new Vector3(diameter, 1f, diameter);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, buffRange);

        Gizmos.color = Color.red;
        if (md != null) Gizmos.DrawWireSphere(transform.position, md.attackRange);
    }
}
