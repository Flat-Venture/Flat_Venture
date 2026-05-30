using System.Collections;
using UnityEngine;

public class MonsterArcProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float gravity = 25f;
    public float destroyDistance = 50f;
    public LayerMask groundMask;

    [Header("Explosion")]
    public float explodeRadius = 4.5f;
    public float stunDuration = 2f;

    [Header("Telegraph")]
    public GroundTelegraph telegraphPrefab;
    public float delayMark = 1.2f;

    Vector3 velocity;
    Vector3 startPos;

    MonsterBase owner;
    public GameObject ExplosionFx;
    PoolManager pool;
    int playerLayer;

    bool landed = false; // 바닥에 닿았는지(한 번만)

    void Awake()
    {
        pool = PoolManager.Instance;
        playerLayer = LayerMask.NameToLayer("Player");
    }

    public void Init(MonsterBase owner, Vector3 from, Vector3 to, float arcHeight)
    {
        this.owner = owner;

        transform.position = from;
        startPos = from;

        landed = false;
        velocity = CalculateArcVelocity(from, to, arcHeight);

        PlaySfx("MonsterArcProjectile");
        gameObject.SetActive(true);
    }

    void Update()
    {
        // 바닥에 닿으면 더 이상 이동/중력 적용 금지
        if (landed) return;

        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        if ((transform.position - startPos).sqrMagnitude > destroyDistance * destroyDistance)
            ReturnToPool();
    }

    void LateUpdate()
    {
        if (landed) return;

        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.25f, groundMask))
        {
            landed = true;

            // 투사체를 바닥 위에 고정 
            Vector3 groundPoint = hit.point + Vector3.up * 0.02f;
            transform.position = groundPoint;

            // 이동 완전 정지
            velocity = Vector3.zero;

            StartCoroutine(CoChargeThenExplode(groundPoint));
        }
    }

    IEnumerator CoChargeThenExplode(Vector3 groundPoint)
    {
        GroundTelegraph tg = null;

        // 텔레그래프 생성
        if (telegraphPrefab != null)
        {
            tg = Instantiate(telegraphPrefab, groundPoint, Quaternion.Euler(0f, 0f, 0f));
            tg.Setup(explodeRadius, delayMark);
            tg.StartCharge();
        }

        // 차징 대기
        yield return new WaitForSeconds(delayMark);

        // 폭발 데미지 처리
        DoExplosionDamage();

        // 텔레그래프 제거
        if (tg != null)
        {
            tg.StopAndHide();
            Destroy(tg.gameObject);
        }

        // 투사체 풀 반환
        ReturnToPool();
    }

    void DoExplosionDamage()
    {
        if(ExplosionFx != null)
        {
            GameObject fx = Instantiate(ExplosionFx, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
        Collider[] hits = Physics.OverlapSphere(transform.position, explodeRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject.layer != playerLayer) continue;

            Player p = hits[i].GetComponent<Player>();
            if (p == null || owner == null || owner.md == null) continue;

            float dmg = owner.md.attackDamage;

            BuffReceiver buff = owner.GetComponent<BuffReceiver>();
            if (buff != null) dmg *= buff.AttackMultiplier;

            p.TakeDamage(dmg);
            p.Stun(stunDuration);
        }
    }

    Vector3 CalculateArcVelocity(Vector3 from, Vector3 to, float arcHeight)
    {
        float timeUp = Mathf.Sqrt(2f * arcHeight / gravity);
        float totalTime = Mathf.Max(0.15f, timeUp * 2f);

        Vector3 delta = to - from;
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);

        float vxz = deltaXZ.magnitude / totalTime;
        Vector3 dirXZ = (deltaXZ.sqrMagnitude > 0.0001f) ? deltaXZ.normalized : Vector3.zero;

        float vy = gravity * timeUp;

        return dirXZ * vxz + Vector3.up * vy;
    }

    void ReturnToPool()
    {
        pool.Return(Enums.PoolType.MonsterArcProjectile, this);
    }
    void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
