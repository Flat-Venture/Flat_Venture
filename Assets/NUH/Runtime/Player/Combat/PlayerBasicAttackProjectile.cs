using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>기본 원거리 공격에 공통으로 사용하는 투사체입니다. 프리팹 설정에 따라 단일 적중 또는 스플래시 피해를 처리합니다.</summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerBasicAttackProjectile : MonoBehaviour
{
    // 스플래시 반경이 0보다 크면 직접 적중 지점 주변의 다른 대상에게 추가 피해를 줍니다.
    [Header("스플래시")]
    [Min(0f)] [SerializeField] private float splashRadius;
    [Min(0f)] [SerializeField] private float splashDamageMultiplier = 0.5f;
    [SerializeField] private LayerMask splashTargetMask = ~0;

    // 스플래시 검색 중 배열 할당과 같은 대상 중복 피해를 막기 위한 재사용 컬렉션입니다.
    private Collider[] splashResults = new Collider[32];
    private RaycastHit[] sweepResults = new RaycastHit[16];
    private readonly HashSet<IPlayerAttackTarget> splashTargets = new HashSet<IPlayerAttackTarget>();
    // Kinematic 이동에 사용할 물리 본체입니다.
    private Rigidbody body;
    private BoxCollider projectileCollider;
    // 발사한 플레이어와 발사 순간 확정된 이동·피해 값입니다.
    private Transform owner;
    private Vector3 direction;
    private float speed;
    private float damage;
    private float remainingDistance;
    // 풀 반환을 한 번만 요청하기 위한 실행 상태입니다.
    private bool initialized;
    private bool releaseRequested;
    private Action<PlayerBasicAttackProjectile> releaseHandler;

    public float SplashRadius { get { return splashRadius; } }
    public float SplashDamageMultiplier { get { return splashDamageMultiplier; } }

    /// <summary>물리 본체와 Trigger Collider를 투사체 규칙에 맞게 설정합니다.</summary>
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        projectileCollider = GetComponent<BoxCollider>();
        projectileCollider.isTrigger = true;
    }

    /// <summary>풀에서 꺼낸 투사체에 이번 발사의 위치·방향·능력치·반환 함수를 전달합니다.</summary>
    public void Initialize(
        Transform projectileOwner,
        Vector3 spawnPosition,
        Vector3 moveDirection,
        float moveSpeed,
        float projectileDamage,
        float width,
        float maximumDistance,
        Action<PlayerBasicAttackProjectile> onRelease)
    {
        owner = projectileOwner;
        direction = moveDirection.normalized;
        speed = Mathf.Max(0.01f, moveSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        remainingDistance = Mathf.Max(0.01f, maximumDistance);
        releaseHandler = onRelease;
        releaseRequested = false;

        transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(direction, Vector3.up));
        transform.localScale = new Vector3(Mathf.Max(0.1f, width), 0.15f, 0.6f);
        body.position = spawnPosition;
        initialized = true;
    }

    /// <summary>고정 물리 프레임마다 전진하고 공격 사거리를 모두 이동하면 풀로 돌아갑니다.</summary>
    private void FixedUpdate()
    {
        if (!initialized)
            return;

        float distance = Mathf.Min(speed * Time.fixedDeltaTime, remainingDistance);
        bool hitSomething = SweepMovement(distance, out float allowedDistance);
        if (allowedDistance > 0f)
            body.MovePosition(body.position + (direction * allowedDistance));

        remainingDistance -= distance;
        if (hitSomething || remainingDistance <= 0f)
            RequestRelease();
    }

    /// <summary>이번 물리 프레임에 이동할 구간을 박스 캐스트로 검사해 빠른 투사체가 대상을 지나치지 않게 합니다.</summary>
    private bool SweepMovement(float moveDistance, out float allowedDistance)
    {
        allowedDistance = moveDistance;
        int hitCount = BoxCastAllGrowing(moveDistance);
        float nearestDistance = float.PositiveInfinity;
        IPlayerAttackTarget nearestTarget = null;
        bool nearestIsWall = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = sweepResults[i].collider;
            if (ShouldIgnoreCollider(hitCollider))
                continue;

            IPlayerAttackTarget target = hitCollider.GetComponentInParent<IPlayerAttackTarget>();
            if (target != null)
            {
                if (!target.IsAlive)
                    continue;

                if (sweepResults[i].distance < nearestDistance)
                {
                    nearestDistance = sweepResults[i].distance;
                    nearestTarget = target;
                    nearestIsWall = false;
                }

                continue;
            }

            if (!hitCollider.isTrigger && sweepResults[i].distance < nearestDistance)
            {
                nearestDistance = sweepResults[i].distance;
                nearestTarget = null;
                nearestIsWall = true;
            }
        }

        if (float.IsPositiveInfinity(nearestDistance))
            return false;

        allowedDistance = Mathf.Max(0f, nearestDistance);
        if (nearestTarget != null)
        {
            Vector3 explosionCenter = body.position + (direction * allowedDistance);
            nearestTarget.TakeDamage(damage);
            ApplySplashDamage(nearestTarget, explosionCenter);
        }

        return nearestTarget != null || nearestIsWall;
    }

    /// <summary>BoxCast 결과 버퍼가 가득 차면 크기를 늘려 많은 Collider도 놓치지 않게 합니다.</summary>
    private int BoxCastAllGrowing(float moveDistance)
    {
        int hitCount;
        Vector3 halfExtents = Vector3.Scale(projectileCollider.size, transform.localScale) * 0.5f;
        do
        {
            hitCount = Physics.BoxCastNonAlloc(
                body.position,
                halfExtents,
                direction,
                sweepResults,
                transform.rotation,
                moveDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            if (hitCount < sweepResults.Length)
                return hitCount;

            sweepResults = new RaycastHit[sweepResults.Length * 2];
        }
        while (sweepResults.Length <= 128);

        return hitCount;
    }

    /// <summary>첫 공격 대상에 피해를 주거나 벽에 닿으면 즉시 풀 반환을 요청합니다.</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || ShouldIgnoreCollider(other))
            return;

        IPlayerAttackTarget target = other.GetComponentInParent<IPlayerAttackTarget>();
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(damage);
            ApplySplashDamage(target, body.position);
            RequestRelease();
            return;
        }

        if (!other.isTrigger)
            RequestRelease();
    }

    /// <summary>자기 자신과 발사한 플레이어의 Collider는 투사체 충돌 검사에서 제외합니다.</summary>
    private bool ShouldIgnoreCollider(Collider other)
    {
        return other == null
            || other == projectileCollider
            || (owner != null && other.transform.IsChildOf(owner));
    }

    /// <summary>직접 적중 대상 주변의 다른 살아 있는 대상에게 설정된 비율의 스플래시 피해를 줍니다.</summary>
    private void ApplySplashDamage(IPlayerAttackTarget directTarget, Vector3 explosionCenter)
    {
        if (splashRadius <= 0f || splashDamageMultiplier <= 0f)
            return;

        int hitCount = OverlapSplashTargets(explosionCenter);
        splashTargets.Clear();
        float splashDamage = damage * splashDamageMultiplier;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = splashResults[i];
            if (hitCollider == null || (owner != null && hitCollider.transform.IsChildOf(owner)))
                continue;

            IPlayerAttackTarget splashTarget = hitCollider.GetComponentInParent<IPlayerAttackTarget>();
            if (splashTarget == null || splashTarget == directTarget || !splashTarget.IsAlive)
                continue;

            if (splashTargets.Add(splashTarget))
                splashTarget.TakeDamage(splashDamage);
        }
    }

    /// <summary>스플래시 범위 안 Collider가 버퍼를 가득 채우면 버퍼를 키워 다시 검색합니다.</summary>
    private int OverlapSplashTargets(Vector3 explosionCenter)
    {
        int hitCount;
        do
        {
            hitCount = Physics.OverlapSphereNonAlloc(
                explosionCenter,
                splashRadius,
                splashResults,
                splashTargetMask,
                QueryTriggerInteraction.Collide);

            if (hitCount < splashResults.Length)
                return hitCount;

            splashResults = new Collider[splashResults.Length * 2];
        }
        while (splashResults.Length <= 256);

        return hitCount;
    }

    /// <summary>중복 반환을 막고 이동을 정지한 뒤 이 투사체를 소유한 풀에 돌려보냅니다.</summary>
    private void RequestRelease()
    {
        if (releaseRequested)
            return;

        releaseRequested = true;
        initialized = false;
        releaseHandler?.Invoke(this);
    }

    /// <summary>재사용 전에 이전 발사의 소유자와 반환 함수를 제거합니다.</summary>
    private void OnDisable()
    {
        initialized = false;
        releaseRequested = false;
        owner = null;
        releaseHandler = null;
        splashTargets.Clear();
    }
}
