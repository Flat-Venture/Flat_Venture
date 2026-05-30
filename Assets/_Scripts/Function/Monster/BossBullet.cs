using UnityEngine;

public class BossBullet : MonoBehaviour
{
    MonsterBase owner;
    Vector3 dir;
    float speed;

    bool willSplit;
    float splitDistance;
    int splitCount;
    Vector3 startPos;

    int playerLayer;

    [Header("Life / Range")]
    public float destroyDistance = 35f;   // 화면 밖으로 날아가면 회수
    public float maxLifeTime = 8f;        // 혹시라도 꼬이면 시간으로도 회수

    float lifeTimer;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    void OnEnable()
    {
        // 풀 재사용 시 상태 꼬임 방지
        owner = null;
        dir = Vector3.forward;
        speed = 0f;

        willSplit = false;
        splitDistance = 0f;
        splitCount = 0;

        startPos = transform.position;

        lifeTimer = 0f;
    }

    public void InitSimple(MonsterBase owner, Vector3 dir, float speed)
    {
        this.owner = owner;
        this.dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        this.speed = speed;

        willSplit = false;
        startPos = transform.position;

        lifeTimer = 0f;

        PlaySfx("BossBullet");
    }

    public void InitStraight(MonsterBase owner, Vector3 dir, float speed, float splitDistance, int splitCount)
    {
        InitSimple(owner, dir, speed);
        willSplit = true;
        this.splitDistance = splitDistance;
        this.splitCount = splitCount;
        startPos = transform.position;
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;

        // 수명/거리 제한
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifeTime)
        {
            ReturnToPool();
            return;
        }

        float sqrDist = (transform.position - startPos).sqrMagnitude;
        if (sqrDist >= destroyDistance * destroyDistance)
        {
            ReturnToPool();
            return;
        }

        // 분열 탄 처리
        if (willSplit && sqrDist >= splitDistance * splitDistance)
        {
            Split();
            ReturnToPool();
            return;
        }
    }

    void Split()
    {
        if (splitCount <= 0) return;

        if (PoolManager.Instance == null) return;

        float step = 360f / splitCount;
        for (int i = 0; i < splitCount; i++)
        {
            float ang = i * step;
            Vector3 d = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));

            BossBullet child = PoolManager.Instance.Get<BossBullet>(Enums.PoolType.BossBullet);
            if (child == null) return; // 풀 없으면 중단

            child.transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(d));
            child.InitSimple(owner, d, speed);

        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != playerLayer) return;

        Player p = other.GetComponent<Player>();
        if (p != null && owner != null && owner.md != null)
        {
            p.TakeDamage(owner.md.attackDamage);
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Return(Enums.PoolType.BossBullet, this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
