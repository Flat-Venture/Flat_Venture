using UnityEngine;

/// <summary>
/// 원거리 공격 + 힐
/// 힐 대상이 있을 경우 공격상태를 힐 상태로 전환
/// 힐은 일정 주기마다 범위 내 아군 전체에게 적용
/// </summary>
public class Monster5 : MonsterBase
{
    [Header("Ranged Combat")]
    public float tolerance = 2f;

    float attackTimer = 0f;

    [Header("Heal (Aura)")]
    public float healRange = 6f;
    public float healPerSecond = 8f;
    public float healTickInterval = 0.25f;
    public bool includeSelf = false;
    public LayerMask monster;

    [Header("Heal Range Visual")]
    public GameObject healRangePrefab;
    public float healVisualYOffset = 0.05f;

    GameObject healRangeInstance;
    bool needHeal;

    float healTimer = 0f;

    protected override void Start()
    {
        base.Start();

        if(healRangePrefab != null)
        {
            healRangeInstance = Instantiate(healRangePrefab, transform.position, Quaternion.identity, transform);

            healRangeInstance.SetActive(false);
        }

    }
    

    protected override void Update()
    {
        if (isDef) return;

        needHeal = HasInjuredAllyInHealRange();

        switch (state)
        {
            case Enums.MonsterState.Idle: Idle(); break;
            case Enums.MonsterState.Move: Move(); break;
            case Enums.MonsterState.Attack: Attack(); break;
            case Enums.MonsterState.Die: Die(); break;
        }
    }


    void LateUpdate()
    {
        if(healRangeInstance != null && healRangeInstance.activeSelf) UpdateHealRangeVisual();
    }

    protected override void Idle()
    {
        agent.isStopped = true;

        if (player == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);
        if (dis <= detectRange)
        {
            state = Enums.MonsterState.Move;
        }
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

        // 힐 대상이 존재하면 공격을 멈추고 힐 상태로 전환
        if (HasInjuredAllyInHealRange())
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            state = Enums.MonsterState.Attack;

            if(state != Enums.MonsterState.Attack)
            {
                state = Enums.MonsterState.Attack;
                healTimer = 0f;
                attackTimer = 0f;
            }
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
            // 사정거리 내 진입 시 공격 상태로 전환
            agent.isStopped = true;
            agent.updateRotation = false;

            state = Enums.MonsterState.Attack;
            attackTimer = 0f;
            healTimer = 0f;
        }
    }

    protected override void Attack()
    {
        if (player == null || md == null) return;

        agent.isStopped = true;

        bool healing = HasInjuredAllyInHealRange();

        if (healRangeInstance != null) healRangeInstance.SetActive(healing);

        if(healing)
        {
            // 힐 우선: 주변에 체력이 감소한 아군이 있으면 힐만 수행

            healTimer += Time.deltaTime;
            if (healTimer >= healTickInterval)
            {
                float healThisTick = healPerSecond * healTickInterval;
                HealAlliesInRange(healThisTick);
                healTimer = 0f;

                // anim.SetTrigger("Heal");
            }

            float disToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (disToPlayer > md.attackRange + tolerance)
            {
                state = Enums.MonsterState.Move;
            }
            //Debug.Log("공격");
            return; // 힐 중일 때는 공격 로직을 수행하지 않음
        }

        // 공격 대상이 플레이어일 경우 원거리 공격 수행
        float dis = Vector3.Distance(transform.position, player.transform.position);

        if (dis > md.attackRange + tolerance)
        {
            state = Enums.MonsterState.Move;
            agent.updateRotation = true;
            attackTimer = 0f;
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer < md.attackSpeed) return;

        
        MonsterProjectile mp = GetMonsterProjectile();

        Vector3 shootPos = firepoint != null ? firepoint.position : transform.position;
        mp.transform.position = shootPos;

        Vector3 targetPos = player.transform.position;
        Vector3 dir = (targetPos - shootPos).normalized;

        if (dir.sqrMagnitude > 0.0001f)
            mp.transform.rotation = Quaternion.LookRotation(dir);

        mp.Init(this, dir);

        // anim.SetTrigger("Shoot");

        attackTimer = 0f;
    }

    bool HasInjuredAllyInHealRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, healRange, monster);

        for (int i = 0; i < hits.Length; i++)
        {
            MonsterBase ally = hits[i].GetComponentInParent<MonsterBase>();
            if (ally == null) continue;

            if (!includeSelf && ally == this) continue;
            if (ally.currentHp <= 0f) continue;

            float maxHp = GetMaxHp(ally);
            if (maxHp <= 0f) continue;

            if (ally.currentHp < maxHp) return true;
        }

        return false;
    }

    void HealAlliesInRange(float healAmount)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, healRange, monster);

        for (int i = 0; i < hits.Length; i++)
        {
            MonsterBase ally = hits[i].GetComponentInParent<MonsterBase>();
            if (ally == null) continue;

            if (!includeSelf && ally == this) continue;
            if (ally.currentHp <= 0f) continue;

            float maxHp = GetMaxHp(ally);
            if (maxHp <= 0f) continue;

            if (ally.currentHp >= maxHp) continue;

            ally.currentHp = Mathf.Min(ally.currentHp + healAmount, maxHp);
            //Debug.Log("힐");
        }
    }

    void UpdateHealRangeVisual()
    {
        if (healRangeInstance == null) return;

        // 위치
        Vector3 pos = transform.position;
        pos.y += healVisualYOffset;
        healRangeInstance.transform.position = pos;

        // 지름 = healRange * 2
        float diameter = healRange;
        healRangeInstance.transform.localScale = new Vector3(diameter, 1f, diameter);
    }


    float GetMaxHp(MonsterBase m)
    {
        if (m.md != null) return m.md.maxHp;

        return 100f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; 
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRange);

        Gizmos.color = Color.red;
        if (md != null) Gizmos.DrawWireSphere(transform.position, md.attackRange);
    }
}
