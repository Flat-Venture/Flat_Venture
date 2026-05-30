using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Hovl_Laser : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerTick = 30f;          // 틱당 데미지
    public float tickInterval = 0.12f;         // BossController의 laserTick과 맞추기
    public float stunPerTick = 0f;             // 필요하면 0.0f보다 크게
    public LayerMask playerMask;               // Player 레이어(또는 Player 콜라이더 레이어)
    public float hitRadius = 0.35f;            // 레이저 두께 판정 (0.2~0.6 추천)

    [Header("Visual")]
    public GameObject HitEffect;
    public float HitOffset = 0;
    public bool useLaserRotation = false;

    [Header("Block (Wall)")]
    public LayerMask blockMask;                // 벽/오브젝트 막힘 레이어 (반드시 설정!)
    public float MaxLength = 18f;

    [Header("Texture Tiling")]
    public float MainTextureLength = 1f;
    public float NoiseTextureLength = 1f;

    [Header("Charge Visual")]
    public float chargeWidthMultiplier = 0.3f; // 차지 중 얇게(선택)
    float baseWidth = -1f;

    LineRenderer Laser;
    Vector4 Length = new Vector4(1, 1, 1, 1);

    ParticleSystem[] Effects;
    ParticleSystem[] Hit;

    bool isCharging;
    bool isFiring;

    Vector3 origin;
    Vector3 forward;

    float tickTimer;

    void Awake()
    {
        Laser = GetComponent<LineRenderer>();
        Effects = GetComponentsInChildren<ParticleSystem>(true);

        if (HitEffect != null)
            Hit = HitEffect.GetComponentsInChildren<ParticleSystem>(true);

        Laser.enabled = false;
        Laser.positionCount = 2;
        Laser.useWorldSpace = true;

        baseWidth = Laser.startWidth; // 라인렌더러 기본 굵기 기억
    }

    void Update()
    {
        if (!Laser.enabled) return;

        // 텍스처 타일링(머티리얼이 있을 때만)
        if (Laser.material != null)
        {
            Laser.material.SetTextureScale("_MainTex", new Vector2(Length[0], Length[1]));
            Laser.material.SetTextureScale("_Noise", new Vector2(Length[2], Length[3]));
        }

        // 라인/히트이펙트 갱신 + 막힌 끝점 계산
        Vector3 end = UpdateLineAndHitFx();

        // 발사 중이 아니면 판정 안함
        if (!isFiring) return;

        // 틱 데미지
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            TryHitPlayer(end);
        }
    }

    /* ================== BossController 호환 API ================== */

    public void BeginCharge()
    {
        isCharging = true;
        isFiring = false;
        tickTimer = 0f;

        Laser.enabled = true;

        // 차지 중에는 얇게
        if (baseWidth > 0f)
            SetWidth(baseWidth * chargeWidthMultiplier);

        // 이펙트 재생
        if (Effects != null)
        {
            foreach (var ps in Effects)
                if (!ps.isPlaying) ps.Play();
        }

        // 차지 중에는 히트 파티클은 보통 끔
        StopHitParticles();
    }

    public void BeginFire()
    {
        isCharging = false;
        isFiring = true;
        tickTimer = 0f;

        Laser.enabled = true;

        // 발사 굵기 복구
        if (baseWidth > 0f)
            SetWidth(baseWidth);
    }

    public void EndFire()
    {
        isCharging = false;
        isFiring = false;
        tickTimer = 0f;

        Laser.enabled = false;

        if (Effects != null)
        {
            foreach (var ps in Effects)
                if (ps.isPlaying) ps.Stop();
        }

        StopHitParticles();
    }

    // BossController에서 firePoint 기준으로 갱신해줄 것
    public void UpdateAim(Vector3 origin, Vector3 forward)
    {
        this.origin = origin;
        this.forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
    }

    /* ================== 내부 ================== */

    // 라인/히트이펙트를 갱신하고, "막힘을 반영한 end"를 반환
    Vector3 UpdateLineAndHitFx()
    {
        Laser.SetPosition(0, origin);

        Vector3 end = origin + forward * MaxLength;

        bool blocked = Physics.Raycast(origin, forward, out RaycastHit hit, MaxLength, blockMask);
        if (blocked) end = hit.point;

        Laser.SetPosition(1, end);

        // 거리 기반 타일링
        float dist = Vector3.Distance(origin, end);
        Length[0] = MainTextureLength * dist;
        Length[2] = NoiseTextureLength * dist;

        // 히트 이펙트 위치
        if (HitEffect != null)
        {
            if (blocked)
            {
                HitEffect.transform.position = hit.point + hit.normal * HitOffset;

                if (useLaserRotation)
                    HitEffect.transform.rotation = Quaternion.LookRotation(forward);
                else
                    HitEffect.transform.LookAt(hit.point + hit.normal);

                // 발사 중에만 히트 파티클
                if (isFiring) PlayHitParticles();
                else StopHitParticles();
            }
            else
            {
                HitEffect.transform.position = end;
                StopHitParticles();
            }
        }

        return end;
    }

    // ✅ 벽에 막히면 end까지만 판정하도록 "클립된 길이"를 사용
    void TryHitPlayer(Vector3 clippedEnd)
    {
        float clippedDistance = Vector3.Distance(origin, clippedEnd);

        // 레이저 길이가 너무 짧으면 스킵
        if (clippedDistance <= 0.01f) return;

        // SphereCast로 두께 있는 판정
        Ray ray = new Ray(origin, forward);

        if (Physics.SphereCast(ray, hitRadius, out RaycastHit phit, clippedDistance, playerMask))
        {
            Player p = phit.collider.GetComponentInParent<Player>();
            if (p != null)
            {
                p.TakeDamage(damagePerTick);
                if (stunPerTick > 0f) p.Stun(stunPerTick);
            }
        }
    }

    void SetWidth(float w)
    {
        Laser.startWidth = w;
        Laser.endWidth = w;
    }

    void PlayHitParticles()
    {
        if (Hit == null) return;
        foreach (var ps in Hit)
            if (!ps.isPlaying) ps.Play();
    }

    void StopHitParticles()
    {
        if (Hit == null) return;
        foreach (var ps in Hit)
            if (ps.isPlaying) ps.Stop();
    }
}
