using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static Enums;

public abstract class MonsterBase : MonoBehaviour
{
    [Header("공통 참조")]
    public GameObject player;
    public MonsterData md;

    protected Enums.MonsterState state;
    protected Animator anim;
    protected NavMeshAgent agent;

    [Header("공통 스탯")]
    public float currentHp = 100f;
    public float detectRange = 10f;

    [Header("Death FX")]
    public GameObject deathFxPrefab;
    public float deathFxLife = 2f;
    public float destroyDelay = 0.1f;

    [Header("Projectile")]
    public MonsterProjectile bulletPrefab;
    public Transform firepoint;

    protected PoolManager poolManager;
    Material mat;

    protected virtual bool UseBaseHitSfx => true;
    protected virtual bool UseBaseDieSfx => true;

    public bool isDef = false;
    bool isDie;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        state = Enums.MonsterState.Idle;

        mat = GetComponentInChildren<SpriteRenderer>().material;

        if (md != null && md.maxHp > 0f) currentHp = md.maxHp;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }

        if (Player.Instance != null) player = Player.Instance.gameObject;
    }

    protected virtual void Start()
    {
        poolManager = PoolManager.Instance;

        if (bulletPrefab != null && firepoint != null && poolManager != null)
        {
            poolManager.CreatePool<MonsterProjectile>(Enums.PoolType.MonsterProjectile, bulletPrefab, 20, null);
        }
    }

    protected virtual void Update()
    {
        if (isDef) return;

        switch (state)
        {
            case Enums.MonsterState.Idle: Idle(); break;
            case Enums.MonsterState.Move: Move(); break;
            case Enums.MonsterState.Attack: Attack(); break;
            case Enums.MonsterState.Die: Die(); break;
        }
    }

    protected abstract void Idle();
    protected abstract void Move();
    protected abstract void Attack();

    protected virtual void Die()
    {
        SetDie();
    }

    public virtual void TakeDamage(float dmg)
    {
        if (state == Enums.MonsterState.Die) return;

        BuffReceiver buff = GetComponent<BuffReceiver>();
        if (buff != null) dmg *= buff.DamageTakenMultiplier;

        if (UseBaseHitSfx)
        {
            PlaySfx("NomalHit");
        }

            currentHp -= dmg;

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            state = Enums.MonsterState.Die;
            Die();
        }
    }


    public virtual MonsterProjectile GetMonsterProjectile()
    {
        if (poolManager == null) poolManager = PoolManager.Instance;
        return poolManager != null ? poolManager.Get<MonsterProjectile>(Enums.PoolType.MonsterProjectile) : null;
    }

    public virtual void ReturnMonsterProjectile(MonsterProjectile mp)
    {
        if (poolManager == null) poolManager = PoolManager.Instance;
        if (poolManager != null) poolManager.Return(Enums.PoolType.MonsterProjectile, mp);
    }

    protected virtual void OnEnable()
    {
        isDie = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        if (md != null && md.maxHp > 0f) currentHp = md.maxHp;
        if (state == Enums.MonsterState.Die) state = Enums.MonsterState.Idle;
    }

    void SetDie()
    {
        if (isDie) return;
        isDie = true;

        if (agent != null) agent.enabled = false;

        GiveExpToPlayer();
        GiveGoldToPlayer();
        SetDieDissolve(destroyDelay);

        if (UseBaseDieSfx)
        {
            PlaySfx("NomalDie");
        }

        if (deathFxPrefab != null)
        {
            GameObject fx = Instantiate(deathFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, deathFxLife);
        }

        Destroy(gameObject, destroyDelay);
    }


    protected virtual void GiveExpToPlayer()
    {
        if (Player.Instance == null || md == null) return;

        PlayerExperience exp = Player.Instance.GetComponent<PlayerExperience>();
        if (exp != null) exp.ReceiveRawExp(md.dropExp);
    }

    protected virtual void GiveGoldToPlayer()
    {
        if (Player.Instance == null || md == null) return;

        PlayerGold gold = Player.Instance.GetComponent<PlayerGold>();
        if (gold != null) gold.ReceiveRawGold(md.dropGold);
    }

    void SetSpawnDissolve(float duration)
    {
        StartCoroutine(AnimateShader(1f, -1f, duration));
    }

    void SetDieDissolve(float duration)
    {
        StartCoroutine(AnimateShader(-1f, 1f, duration));
    }

    IEnumerator AnimateShader(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float value = Mathf.Lerp(start, end, t);
            mat.SetFloat("_DissolveAmount", value);
            yield return null;
        }
        mat.SetFloat("_DissolveAmount", end);
    }

    protected void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
