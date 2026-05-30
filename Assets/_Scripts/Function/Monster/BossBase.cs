using System.Collections;
using UnityEngine;

public abstract class BossBase : MonsterBase, BossStatus
{
    [Header("Boss Info")]
    [SerializeField] int bossId = 0;
    [SerializeField] string bossNameOverride = "";

    [Header("Stun")]
    [SerializeField] protected float currentStun = 0f;
    [SerializeField] protected float stunDuration = 5f;
    [SerializeField] protected float stunGainPerHit = 3f; // 맞을 때마다 누적량

    [Header("Boss SFX")]
    [SerializeField] protected string bossHitSfx = "";
    [SerializeField] protected string bossDieSfx = "";

    protected float maxStun = 50f;
    protected bool isAlive = true;
    protected bool isStunned = false;

    // ===== BossStatus 구현 =====
    public int BossId => (md != null && md.mId != 0) ? md.mId : bossId;
    public string BossName => !string.IsNullOrEmpty(bossNameOverride) ? bossNameOverride : (md != null ? md.monsterName : "Boss");

    public float CurrentHp => currentHp;
    public float MaxHp => (md != null ? md.maxHp : 1f);

    public float CurrentStun => currentStun;
    public float MaxStun => maxStun;

    public bool IsAlive => isAlive;
    public bool IsStunned => isStunned;
    protected override bool UseBaseHitSfx => false;
    protected override bool UseBaseDieSfx => false;

    // UI 편의
    public float Hp01 => (MaxHp <= 0.0001f) ? 0f : Mathf.Clamp01(CurrentHp / MaxHp);
    public float Stun01 => (MaxStun <= 0.0001f) ? 0f : Mathf.Clamp01(CurrentStun / MaxStun);

    protected override void Awake()
    {
        base.Awake();

        if (md != null)
        {
            currentHp = md.maxHp;
            maxStun = (md.stunGauge > 0f) ? md.stunGauge : 50f;
        }
        else
        {
            maxStun = 50f;
        }

        isAlive = true;
        isStunned = false;
        currentStun = 0f;
    }

    public override void TakeDamage(float dmg)
    {
        if (!isAlive) return;

        base.TakeDamage(dmg);

        if (state == Enums.MonsterState.Die)
        {
            isAlive = false;
            return;
        }
        if (!string.IsNullOrEmpty(bossHitSfx))
        {
            PlaySfx(bossHitSfx);
        }
        
        if (!isStunned && maxStun > 0f)
        {
            currentStun = Mathf.Min(currentStun + stunGainPerHit, maxStun);

            if (currentStun >= maxStun)
            {
                StartCoroutine(CoStun());
            }
        }
    }
    protected override void Die()
    {
        if (!string.IsNullOrEmpty(bossDieSfx))
        {
            PlaySfx(bossDieSfx);
        }
        
        base.Die();
    }

    IEnumerator CoStun()
    {
        isStunned = true;

        currentStun = 0f;
        yield return new WaitForSeconds(stunDuration);
        currentStun = Mathf.Clamp(currentStun, 0f, maxStun);

        isStunned = false;
    }
}
