using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FinalBoss : BossBase
{
    public enum BossPattern
    {
        Laser, Marks, SplitShot, PullBurst,
        SlamStun, ExecuteCharge, SummonAdds, JumpTop,
        BulletHell,
    }

    [Header("Player Find")]
    public string playerLayerName = "Player";
    int playerLayer = -1;

    Player playerComp;

    [Header("Boss Core")]
    public Transform topCenterPoint;
    public float patternIdle = 0.6f;
    public float executeChanceIfStunned = 0.8f;

    [Header("Laser")]
    public Transform firePoint2;
    [SerializeField] private Hovl_Laser laser;
    public float laserWindup = 0.6f;
    public float laserDuration = 2.5f;
    public float laserTick = 0.12f;
    public float laserReverseDuration = 1.2f;
    public float laserTurnDegPerSec = 120f;
    public float laserSpawnYOffset = 0.5f;

    [Header("Marks")]
    public int markCount = 3;
    public float markDelay = 1.0f;
    public float markRadius = 3.5f;
    public float markStun = 0.8f;
    public Vector2 markAreaMin = new Vector2(-10, -6);
    public Vector2 markAreaMax = new Vector2(10, 6);

    [Header("Teleport FX")]
    public GameObject teleportOutFx;
    public GameObject teleportInFx;
    public float teleportFxLife = 2f;
    public float teleportFxYOffset = 0.05f;
    public bool teleportFxUseWorldY0 = true;

    [Header("SplitShot")]
    public BossBullet bossBulletPrefab;
    public int splitChildCount = 10;
    public float splitDistance = 10f;
    public float bulletSpeed = 14f;

    [Header("PullBurst")]
    public float pullRange = 10f;
    public float pullDuration = 0.35f;
    public float pullStrength = 14f;
    public float pullExplosionRadius = 2.5f;
    public float pullStun = 0.6f;

    [Header("SlamStun")]
    public float slamRange = 6f;
    public float slamWindup = 0.8f;
    public float slamRadius = 5f;
    public float slamStun = 1.2f;

    [Header("Execute")]
    public float executeCharge = 2.0f;
    public float executeRadius = 4.5f;

    [Header("Summon")]
    public GameObject[] randomMonsterPrefabs;
    public int summonMin = 2;
    public int summonMax = 4;
    public Vector2 summonAreaMin = new Vector2(-9, -5);
    public Vector2 summonAreaMax = new Vector2(9, 5);

    [Header("Summon FX")]
    public GameObject summonFxPrefab;
    public float summonFxLife = 2f;

    [Header("BulletHell")]
    public float hellDuration = 8f;
    public float hellFireInterval = 0.08f;
    public int hellRingBullets = 60;
    public int hellFanBullets = 21;
    public float hellFanAngle = 110f;
    public float hellSpiralSpeed = 420f;

    [Header("Pattern FX Prefabs")]
    public GameObject fxMarks;
    public GameObject fxSlam;
    public GameObject fxExecute;
    public GameObject fxPull;

    [Header("FX Common")]
    public float fxYOffset = 0.05f;
    public float fxLifeExtra = 0.3f;
    public bool fxUseWorldY0 = true;

    bool runningPattern = false;
    float lastUltimate = -999f;
    public float ultimateCd = 18f;

    protected override void Awake()
    {
        base.Awake();
        playerLayer = LayerMask.NameToLayer(playerLayerName);

        //if (laser == null) laser = GetComponentInChildren<Hovl_Laser>(true);
        //if (laser != null) laser.EndFire();
    }

    protected override void Start()
    {
        if (laser == null) laser = GetComponentInChildren<Hovl_Laser>(true);
        if (laser != null) laser.EndFire();

        base.Start();

        FindPlayerByLayer();

        if (PoolManager.Instance != null && bossBulletPrefab != null)
            PoolManager.Instance.CreatePool(Enums.PoolType.BossBullet, bossBulletPrefab, 200);

        if (laser != null) laser.EndFire();
    }

    void EnsurePlayer()
    {
        if (player != null && playerComp != null) return;

        if (player != null && playerComp == null)
            playerComp = player.GetComponent<Player>();

        if (player == null || playerComp == null)
            FindPlayerByLayer();
    }

    void FindPlayerByLayer()
    {
        Player p = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (p != null && p.gameObject.layer == playerLayer)
        {
            playerComp = p;
            player = p.gameObject;
            return;
        }

        playerComp = null;
        player = null;
    }

    float PatternInterval()
    {
        if (md != null && md.attackSpeed > 0f) return md.attackSpeed;
        return patternIdle;
    }

    protected override void Idle()
    {
        EnsurePlayer();
        if (player == null) return;

        state = Enums.MonsterState.Move;
    }

    protected override void Move()
    {
        if (IsStunned) return;

        EnsurePlayer();
        if (player == null || md == null) return;

        if (agent != null)
        {
            //agent.isStopped = false;
            agent.updateRotation = true;
            //agent.SetDestination(player.transform.position);
        }

        if (!runningPattern && Vector3.Distance(transform.position, player.transform.position) <= md.attackRange)
            state = Enums.MonsterState.Attack;
    }

    protected override void Attack()
    {
        EnsurePlayer();
        if (runningPattern || player == null || playerComp == null || md == null) return;

        StartCoroutine(CoPatternLoop());
    }

    IEnumerator CoPatternLoop()
    {
        if (IsStunned) yield break;
        runningPattern = true;

        while (state != Enums.MonsterState.Die)
        {
            EnsurePlayer();
            if (player == null || playerComp == null) { runningPattern = false; yield break; }

            List<BossPattern> conditional = new List<BossPattern>();

            if (Vector3.Distance(transform.position, player.transform.position) <= slamRange)
                conditional.Add(BossPattern.SlamStun);

            if (playerComp.IsStunned)
                conditional.Add(BossPattern.ExecuteCharge);

            if (randomMonsterPrefabs != null && randomMonsterPrefabs.Length > 0)
                conditional.Add(BossPattern.SummonAdds);

            if (topCenterPoint != null)
                conditional.Add(BossPattern.JumpTop);

            bool canUltimate = (Time.time >= lastUltimate + ultimateCd);

            if (canUltimate && Random.value < 0.12f)
            {
                lastUltimate = Time.time;
                yield return StartCoroutine(Pattern_BulletHell());
                yield return new WaitForSeconds(PatternInterval());
                continue;
            }

            if (playerComp.IsStunned && Random.value < executeChanceIfStunned)
            {
                yield return StartCoroutine(Pattern_ExecuteCharge());
                yield return new WaitForSeconds(PatternInterval());
                continue;
            }

            if (conditional.Count > 0 && Random.value < 0.45f)
            {
                BossPattern picked = conditional[Random.Range(0, conditional.Count)];
                yield return StartCoroutine(RunPattern(picked));
                yield return new WaitForSeconds(PatternInterval());
                continue;
            }

            int r = Random.Range(0, 4);
            BossPattern normal =
                (r == 0) ? BossPattern.Laser :
                (r == 1) ? BossPattern.Marks :
                (r == 2) ? BossPattern.SplitShot :
                           BossPattern.PullBurst;

            yield return StartCoroutine(RunPattern(normal));
            yield return new WaitForSeconds(PatternInterval());
        }

        runningPattern = false;
        state = Enums.MonsterState.Move;
    }

    public override void TakeDamage(float dmg)
    {
        PlaySfx("FinalBossHit");
        base.TakeDamage(dmg);
    }

    protected override void Die()
    {
        PlaySfx("FinalBossDie");
        base.Die();
    }

    IEnumerator RunPattern(BossPattern p)
    {
        if (IsStunned) yield break;

        switch (p)
        {
            case BossPattern.Laser: yield return StartCoroutine(Pattern_Laser()); break;
            case BossPattern.Marks: yield return StartCoroutine(Pattern_Marks()); break;
            case BossPattern.SplitShot: yield return StartCoroutine(Pattern_SplitShot()); break;
            case BossPattern.PullBurst: yield return StartCoroutine(Pattern_PullBurst()); break;
            case BossPattern.SlamStun: yield return StartCoroutine(Pattern_SlamStun()); break;
            case BossPattern.ExecuteCharge: yield return StartCoroutine(Pattern_ExecuteCharge()); break;
            case BossPattern.SummonAdds: yield return StartCoroutine(Pattern_SummonAdds()); break;
            case BossPattern.JumpTop: yield return StartCoroutine(Pattern_JumpTop()); break;
        }
    }

    // ====== 패턴 ======

    IEnumerator Pattern_Laser()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || playerComp == null || md == null || laser == null) yield break;

        PlaySfx("FinalBossLaser");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        laser.tickInterval = laserTick;
        laser.damagePerTick = md.attackDamage;
        laser.stunPerTick = 0f;

        laser.BeginCharge();
        yield return new WaitForSeconds(laserWindup);

        laser.BeginFire();

        float t = 0f;
        while (t < laserDuration)
        {
            if (IsStunned) { laser.EndFire(); yield break; }

            t += Time.deltaTime;

            Vector3 aim = player.transform.position - transform.position;
            aim.y = 0f;

            if (aim.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(aim.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    target,
                    laserTurnDegPerSec * Time.deltaTime
                );
            }

            Vector3 origin = firePoint2 != null ? firePoint2.position : transform.position + Vector3.up * laserSpawnYOffset;
            Vector3 toPlayer = (player.transform.position - origin);
            toPlayer.y = 0f;

            Vector3 forward =
                (toPlayer.sqrMagnitude > 0.0001f)
                ? toPlayer.normalized
                : (firePoint2 != null ? firePoint2.forward : transform.forward);

            laser.UpdateAim(origin, forward);

            if (Random.value < 0.25f) playerComp.ReverseInput(laserReverseDuration);

            yield return null;
        }

        laser.EndFire();
        
        if (agent != null) agent.isStopped = false;
    }

    IEnumerator Pattern_Marks()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || md == null) yield break;

        PlaySfx("FinalBossMark1");

        if (agent != null) agent.isStopped = false;

        for (int i = 0; i < markCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(markAreaMin.x, markAreaMax.x),
                0f,
                Random.Range(markAreaMin.y, markAreaMax.y)
            );

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                pos = hit.position;

            GameObject fx = SpawnPatternFx(fxMarks, pos, markRadius, markDelay);

            StartCoroutine(CoMarkExplode(pos, markRadius, markDelay, markStun, fx));

            yield return new WaitForSeconds(0.18f);
        }

        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator CoMarkExplode(Vector3 pos, float radius, float delay, float stun, GameObject fx)
    {
        if (IsStunned) yield break;

        yield return new WaitForSeconds(delay);

        PlaySfx("FinalBossMark2");

        Collider[] hits = Physics.OverlapSphere(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject.layer != playerLayer) continue;

            Player p = hits[i].GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(md.attackDamage);
                p.Stun(stun);
                break;
            }
        }
    }

    IEnumerator Pattern_SplitShot()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || bossBulletPrefab == null) yield break;

        PlaySfx("FinalBossSplitShot");

        if (agent != null) agent.isStopped = true;

        Vector3 shootPos = firepoint != null ? firepoint.position : transform.position + transform.forward * 1.2f;
        Vector3 dir = (player.transform.position - shootPos);
        dir.y = 0f;
        dir = dir.normalized;

        BossBullet b = null;
        if (PoolManager.Instance != null) b = PoolManager.Instance.Get<BossBullet>(Enums.PoolType.BossBullet);

        if (b == null)
        {
            b = Instantiate(bossBulletPrefab, shootPos, Quaternion.LookRotation(dir));
        }
        else
        {
            b.transform.SetPositionAndRotation(shootPos, Quaternion.LookRotation(dir));
        }

        b.InitStraight(this, dir, bulletSpeed, splitDistance, splitChildCount);

        yield return new WaitForSeconds(0.4f);
        if (agent != null) agent.isStopped = false;
    }

    IEnumerator Pattern_PullBurst()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || playerComp == null || md == null) yield break;

        PlaySfx("FinalBossPull1");

        if (agent != null) agent.isStopped = true;

        if (Vector3.Distance(transform.position, player.transform.position) > pullRange)
        {
            if (agent != null) agent.isStopped = false;
            yield break;
        }

        float windup = 0.25f;

        yield return StartCoroutine(CoPullTelegraph(pullExplosionRadius, windup));
        
        yield return StartCoroutine(CoPull(player.transform, pullDuration, pullStrength));
        
        Collider[] hits = Physics.OverlapSphere(transform.position, pullExplosionRadius);

        PlaySfx("FinalBossPull2");

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject.layer != playerLayer) continue;

            Player p = hits[i].GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(md.attackDamage);
                p.Stun(pullStun);
                break;
            }
        }

        if (agent != null) agent.isStopped = false;
    }

    IEnumerator CoPull(Transform target, float duration, float strength)
    {
        if (IsStunned) yield break;

        CharacterController cc = target != null ? target.GetComponent<CharacterController>() : null;

        float t = 0f;
        while (t < duration && target != null)
        {
            if (IsStunned) yield break;

            t += Time.deltaTime;

            Vector3 dir = (transform.position - target.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) break;
            dir.Normalize();

            Vector3 delta = dir * strength * Time.deltaTime;

            if (cc != null) cc.Move(delta);
            else target.position += delta;

            yield return null;
        }
    }

    IEnumerator Pattern_SlamStun()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || playerComp == null || md == null) yield break;

        PlaySfx("FinalBossSlam1");

        if (agent != null) agent.isStopped = true;

        GameObject fx = SpawnPatternFx(fxSlam, transform.position, slamRadius, slamWindup);

        yield return new WaitForSeconds(slamWindup);

        PlaySfx("FinalBossSlam2");

        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject.layer != playerLayer) continue;

            Player p = hits[i].GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(md.attackDamage);
                p.Stun(slamStun);
                break;
            }
        }

        if (agent != null) agent.isStopped = false;
    }

    IEnumerator Pattern_ExecuteCharge()
    {
        if (IsStunned) yield break;

        EnsurePlayer();
        if (player == null || playerComp == null) yield break;

        if (!playerComp.IsStunned) yield break;

        PlaySfx("FinalBossExecute1");

        if (agent != null) agent.isStopped = true;

        Vector3 pos = player.transform.position;

        GameObject fx = SpawnPatternFx(fxExecute, pos, executeRadius, executeCharge);

        yield return new WaitForSeconds(executeCharge);

        PlaySfx("FinalBossExecute");

        float dis = Vector3.Distance(player.transform.position, pos);
        if (dis <= executeRadius) playerComp.TakeDamage(999999f);

        if (agent != null) agent.isStopped = false;
    }

    IEnumerator Pattern_SummonAdds()
    {
        if (IsStunned) yield break;
        if (randomMonsterPrefabs == null || randomMonsterPrefabs.Length == 0) yield break;

        PlaySfx("FinalBossSummon");

        if (agent != null) agent.isStopped = true;

        int count = Random.Range(summonMin, summonMax + 1);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(summonAreaMin.x, summonAreaMax.x), 0f,
                Random.Range(summonAreaMin.y, summonAreaMax.y)
            );

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) pos = hit.position;

            if (summonFxPrefab != null)
            {
                GameObject fx = Instantiate(summonFxPrefab, pos, Quaternion.identity);
                Destroy(fx, summonFxLife);
            }

            GameObject prefab = randomMonsterPrefabs[Random.Range(0, randomMonsterPrefabs.Length)];
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);

            MonsterBase mb = go.GetComponent<MonsterBase>();
            if (mb != null) mb.player = player;

            NavMeshAgent a = go.GetComponent<NavMeshAgent>();
            if (a != null)
            {
                a.enabled = true;
                a.isStopped = false;
                a.Warp(pos);
            }

            yield return new WaitForSeconds(0.08f);
        }

        if (agent != null) agent.isStopped = false;
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator Pattern_JumpTop()
    {
        if (IsStunned) yield break;
        if (player == null) yield break;

        PlaySfx("FinalBossTeleport");

        SpawnTeleportFx(teleportOutFx, transform.position);
        yield return new WaitForSeconds(0.15f);

        Vector3 pos = player.transform.position;

        if (agent != null)
        {
            agent.isStopped = true;

            if (agent.enabled && NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                transform.position = pos;
            }
        }
        else transform.position = pos;

        SpawnTeleportFx(teleportInFx, transform.position);

        yield return new WaitForSeconds(0.1f);

        if (agent != null) agent.isStopped = false;
    }

    IEnumerator Pattern_BulletHell()
    {
        if (IsStunned) yield break;

        PlaySfx("FinalBossTeleport");

        SpawnTeleportFx(teleportOutFx, transform.position);
        yield return new WaitForSeconds(0.15f);

        if (topCenterPoint != null)
        {
            if (agent != null) agent.isStopped = true;
            transform.position = topCenterPoint.position;
        }

        SpawnTeleportFx(teleportInFx, transform.position);
        yield return new WaitForSeconds(0.15f);

        float t = 0f;
        float fire = 0f;
        float spiralAngle = 0f;

        while (t < hellDuration * 0.34f)
        {
            if (IsStunned) yield break;

            t += Time.deltaTime;
            fire += Time.deltaTime;

            spiralAngle += hellSpiralSpeed * Time.deltaTime;

            if (fire >= hellFireInterval)
            {
                fire = 0f;
                // 여긴 발사 빈도가 너무 높아서 SFX 넣으면 난리남(원하면 따로 쿨다운 SFX로 처리)
                FireBulletAtAngle(spiralAngle);
                FireBulletAtAngle(spiralAngle + 180f);
                FireBulletAtAngle(spiralAngle + 90f);
                FireBulletAtAngle(spiralAngle + 270f);
            }
            yield return null;
        }

        FireRing(hellRingBullets);
        yield return new WaitForSeconds(0.55f);

        FireRing(hellRingBullets);
        yield return new WaitForSeconds(0.55f);

        EnsurePlayer();
        if (player != null)
        {
            Vector3 dir = (player.transform.position - transform.position);
            dir.y = 0f;
            float baseAng = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

            FireFan(baseAng, hellFanAngle, hellFanBullets);
            yield return new WaitForSeconds(0.45f);

            FireFan(baseAng + 25f, hellFanAngle, hellFanBullets);
        }

        if (agent != null) agent.isStopped = false;
        yield return new WaitForSeconds(0.4f);

    }

    // ===== BulletHell helpers =====

    void FireBulletAtAngle(float angleDeg)
    {
        if (bossBulletPrefab == null) return;

        Vector3 dir = new Vector3(Mathf.Cos(angleDeg * Mathf.Deg2Rad), 0f, Mathf.Sin(angleDeg * Mathf.Deg2Rad));
        Vector3 pos = firepoint != null ? firepoint.position : transform.position + Vector3.up * 1.0f;

        BossBullet b = null;
        if (PoolManager.Instance != null) b = PoolManager.Instance.Get<BossBullet>(Enums.PoolType.BossBullet);

        if (b == null)
        {
            b = Instantiate(bossBulletPrefab, pos, Quaternion.LookRotation(dir));
        }
        else
        {
            b.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        }

        b.InitSimple(this, dir, bulletSpeed);
    }

    void FireRing(int count)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++) FireBulletAtAngle(i * step);
    }

    void FireFan(float centerAngle, float fanAngle, int count)
    {
        if (count <= 1) { FireBulletAtAngle(centerAngle); return; }

        float start = centerAngle - fanAngle * 0.5f;
        float step = fanAngle / (count - 1);
        for (int i = 0; i < count; i++) FireBulletAtAngle(start + step * i);
    }

    IEnumerator CoPullTelegraph(float radius, float chargeTime)
    {
        if (IsStunned) yield break;

        SpawnPatternFx(fxPull, transform.position, radius, chargeTime);
        yield return new WaitForSeconds(chargeTime);
    }

    // ====== FX 유틸 ======

    GameObject SpawnPatternFx(GameObject prefab, Vector3 pos, float radius, float lifeTime)
    {
        if (prefab == null) return null;

        if (fxUseWorldY0) pos.y = 0f;
        pos.y += fxYOffset;

        Quaternion rot = Quaternion.identity;
        GameObject fx = Instantiate(prefab, pos, rot);

        float diameter = radius * 2f;
        fx.transform.localScale = new Vector3(
            diameter,
            fx.transform.localScale.y,
            diameter
        );

        Destroy(fx, lifeTime + fxLifeExtra);
        return fx;
    }

    void SpawnTeleportFx(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;

        if (teleportFxUseWorldY0) pos.y = 0f;
        pos.y += teleportFxYOffset;

        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx, teleportFxLife);
    }

}
