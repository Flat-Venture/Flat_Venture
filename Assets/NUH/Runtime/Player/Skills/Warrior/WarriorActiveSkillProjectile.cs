using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>직선으로 이동하며 대상을 관통하고 벽에 닿으면 사라지는 전사 검기입니다.</summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class WarriorActiveSkillProjectile : MonoBehaviour
{
    // 여러 Collider가 있는 같은 대상이나 이미 관통한 대상을 중복 타격하지 않게 기록합니다.
    private readonly HashSet<IPlayerAttackTarget> hitTargets = new HashSet<IPlayerAttackTarget>();
    // 물리 프레임에서 MovePosition으로 이동시키는 Kinematic Rigidbody입니다.
    private Rigidbody body;
    // 발사한 플레이어의 Collider를 무시하기 위한 소유자 Transform입니다.
    private Transform owner;
    // Initialize 시 확정된 이동 방향과 런타임 능력치입니다.
    private Vector3 direction;
    private float speed;
    private float damage;
    private int maxHitTargets;
    // 풀에서 꺼낸 뒤 초기화가 끝났을 때만 FixedUpdate가 동작합니다.
    private bool initialized;
    // 같은 프레임의 여러 충돌이 중복 반환을 요청하지 못하게 막습니다.
    private bool releaseRequested;
    // 충돌하지 않아도 무한히 날아가지 않도록 제한하는 남은 수명입니다.
    private float lifetimeRemaining;
    // 직접 Destroy하지 않고 소유한 풀로 돌려보내기 위한 콜백입니다.
    private Action<WarriorActiveSkillProjectile> releaseHandler;

    /// <summary>Rigidbody와 Trigger Collider를 투사체용 설정으로 맞춥니다.</summary>
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        GetComponent<BoxCollider>().isTrigger = true;
    }

    /// <summary>
    /// 풀에서 재사용될 때마다 소유자·방향·속도·피해·크기·관통 수와 반환 콜백을 새로 설정합니다.
    /// 이전 사용에서 기록한 적중 목록도 여기서 비웁니다.
    /// </summary>
    public void Initialize(
        Transform projectileOwner,
        Vector3 moveDirection,
        float moveSpeed,
        float projectileDamage,
        float width,
        int maximumHitTargets,
        Action<WarriorActiveSkillProjectile> onRelease,
        float lifetime = 3f)
    {
        owner = projectileOwner;
        direction = moveDirection.normalized;
        speed = moveSpeed;
        damage = projectileDamage;
        maxHitTargets = Mathf.Max(1, maximumHitTargets);
        releaseHandler = onRelease;
        lifetimeRemaining = Mathf.Max(0.01f, lifetime);
        releaseRequested = false;
        hitTargets.Clear();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.localScale = new Vector3(width, 0.4f, 0.5f);
        initialized = true;
    }

    /// <summary>고정 물리 프레임마다 전진하고 지형 굴곡을 따라 높이를 보정합니다.</summary>
    private void FixedUpdate()
    {
        if (!initialized)
            return;

        lifetimeRemaining -= Time.fixedDeltaTime;
        if (lifetimeRemaining <= 0f)
        {
            RequestRelease();
            return;
        }

        Vector3 nextPosition = body.position + (direction * (speed * Time.fixedDeltaTime));
        FollowNearbyGround(ref nextPosition);
        body.MovePosition(nextPosition);
    }

    /// <summary>다음 위치 아래의 지면을 찾아 작은 높이 차이만 부드럽게 따라갑니다.</summary>
    private void FollowNearbyGround(ref Vector3 nextPosition)
    {
        Vector3 rayOrigin = nextPosition + (Vector3.up * 2f);
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 4f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;

        float desiredY = hit.point.y + 0.45f;
        if (Mathf.Abs(desiredY - nextPosition.y) <= 0.75f)
            nextPosition.y = desiredY;
    }

    /// <summary>대상은 관통 피해를 주고, 일반 벽 Collider에 닿으면 풀 반환을 요청합니다.</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.transform.IsChildOf(owner))
            return;

        IPlayerAttackTarget target = other.GetComponentInParent<IPlayerAttackTarget>();
        if (target != null && target.IsAlive)
        {
            if (hitTargets.Add(target))
            {
                target.TakeDamage(damage);
                if (hitTargets.Count >= maxHitTargets)
                    RequestRelease();
            }

            return;
        }

        if (!other.isTrigger)
            RequestRelease();
    }

    /// <summary>한 번만 반환 콜백을 호출하도록 보호하며 이동 처리를 즉시 중지합니다.</summary>
    private void RequestRelease()
    {
        if (releaseRequested)
            return;

        releaseRequested = true;
        initialized = false;
        releaseHandler?.Invoke(this);
    }

    /// <summary>풀에 들어갈 때 다음 사용에 남으면 안 되는 콜백과 적중 기록을 정리합니다.</summary>
    private void OnDisable()
    {
        initialized = false;
        releaseHandler = null;
        hitTargets.Clear();
    }
}
