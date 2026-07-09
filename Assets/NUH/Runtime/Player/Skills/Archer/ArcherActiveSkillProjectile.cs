using System;
using System.Collections.Generic;
using FlatVenture.NUH.Player.Combat;
using UnityEngine;

namespace FlatVenture.NUH.Player.Skills.Archer
{
    /// <summary>
    /// 궁수 액티브 스킬에 사용하는 긴 직선 관통 사격 투사체입니다.
    /// 적은 제한 없이 관통해 피해를 주고, 벽이나 최대 사거리에 닿으면 풀로 돌아갑니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcherActiveSkillProjectile : MonoBehaviour
    {
        // 같은 대상의 여러 Collider가 한 발에 중복 피해를 받지 않도록 기록합니다.
        private readonly HashSet<IPlayerAttackTarget> hitTargets = new HashSet<IPlayerAttackTarget>();
        // 빠른 투사체가 얇은 더미나 벽을 지나치지 않도록 이동 구간을 박스 캐스트로 검사합니다.
        private RaycastHit[] sweepResults = new RaycastHit[32];

        // 물리 이동과 현재 투사체 크기 판정을 담당하는 컴포넌트입니다.
        private Rigidbody body;
        private BoxCollider boxCollider;
        // 발사한 플레이어를 자신의 충돌 대상에서 제외하기 위해 보관합니다.
        private Transform owner;
        // 발사 순간 확정되는 이동 방향, 속도, 피해, 남은 사거리입니다.
        private Vector3 direction;
        private float speed;
        private float damage;
        private float remainingDistance;
        // BoxCast에 사용할 반 크기와 회전값입니다.
        private Vector3 halfExtents;
        private Quaternion castRotation;
        // 풀 반환을 한 번만 실행하기 위한 상태값입니다.
        private bool initialized;
        private bool releaseRequested;
        private Action<ArcherActiveSkillProjectile> releaseHandler;

        /// <summary>Rigidbody와 Collider를 풀링 투사체에 맞는 설정으로 초기화합니다.</summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        /// <summary>
        /// 풀에서 꺼낼 때마다 위치·방향·속도·피해·두께·최대 사거리·반환 콜백을 새로 설정합니다.
        /// </summary>
        public void Initialize(
            Transform projectileOwner,
            Vector3 spawnPosition,
            Vector3 moveDirection,
            float moveSpeed,
            float projectileDamage,
            float width,
            float maximumDistance,
            Action<ArcherActiveSkillProjectile> onRelease)
        {
            owner = projectileOwner;
            direction = moveDirection.normalized;
            speed = Mathf.Max(0.01f, moveSpeed);
            damage = Mathf.Max(0f, projectileDamage);
            remainingDistance = Mathf.Max(0.01f, maximumDistance);
            releaseHandler = onRelease;
            releaseRequested = false;
            hitTargets.Clear();

            castRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.SetPositionAndRotation(spawnPosition, castRotation);
            transform.localScale = new Vector3(Mathf.Max(0.1f, width), 0.25f, 0.9f);
            body.position = spawnPosition;

            halfExtents = new Vector3(transform.localScale.x * 0.5f, transform.localScale.y * 0.5f, transform.localScale.z * 0.5f);
            initialized = true;
        }

        /// <summary>이동할 구간 전체를 먼저 검사한 뒤 안전한 거리만큼 전진합니다.</summary>
        private void FixedUpdate()
        {
            if (!initialized)
                return;

            float moveDistance = Mathf.Min(speed * Time.fixedDeltaTime, remainingDistance);
            bool hitWall = SweepMovement(moveDistance, out float allowedDistance);
            if (allowedDistance > 0f)
                body.MovePosition(body.position + (direction * allowedDistance));

            remainingDistance -= moveDistance;
            if (hitWall || remainingDistance <= 0f)
                RequestRelease();
        }

        /// <summary>현재 위치부터 다음 위치까지의 모든 대상과 벽을 검사합니다.</summary>
        private bool SweepMovement(float moveDistance, out float allowedDistance)
        {
            allowedDistance = moveDistance;
            int hitCount = BoxCastAllGrowing(moveDistance);
            float nearestWallDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = sweepResults[i].collider;
                if (ShouldIgnoreCollider(hitCollider))
                    continue;

                IPlayerAttackTarget target = hitCollider.GetComponentInParent<IPlayerAttackTarget>();
                if (target != null)
                    continue;

                if (!hitCollider.isTrigger && sweepResults[i].distance < nearestWallDistance)
                    nearestWallDistance = sweepResults[i].distance;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = sweepResults[i].collider;
                if (ShouldIgnoreCollider(hitCollider) || sweepResults[i].distance > nearestWallDistance)
                    continue;

                IPlayerAttackTarget target = hitCollider.GetComponentInParent<IPlayerAttackTarget>();
                if (target == null || !target.IsAlive || !hitTargets.Add(target))
                    continue;

                target.TakeDamage(damage);
            }

            if (float.IsPositiveInfinity(nearestWallDistance))
                return false;

            allowedDistance = Mathf.Max(0f, nearestWallDistance);
            return true;
        }

        /// <summary>버퍼가 가득 차면 크기를 늘려 사거리 안의 많은 대상도 놓치지 않게 합니다.</summary>
        private int BoxCastAllGrowing(float moveDistance)
        {
            int hitCount;
            do
            {
                hitCount = Physics.BoxCastNonAlloc(
                    body.position,
                    halfExtents,
                    direction,
                    sweepResults,
                    castRotation,
                    moveDistance,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);

                if (hitCount < sweepResults.Length)
                    return hitCount;

                sweepResults = new RaycastHit[sweepResults.Length * 2];
            }
            while (sweepResults.Length <= 256);

            return hitCount;
        }

        /// <summary>Trigger 이벤트는 BoxCast가 놓칠 수 있는 시작 겹침 상황의 보조 판정으로 사용합니다.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || ShouldIgnoreCollider(other))
                return;

            IPlayerAttackTarget target = other.GetComponentInParent<IPlayerAttackTarget>();
            if (target != null)
            {
                if (target.IsAlive && hitTargets.Add(target))
                    target.TakeDamage(damage);
                return;
            }

            if (!other.isTrigger)
                RequestRelease();
        }

        /// <summary>자기 자신, 발사자, 이미 풀 반환이 시작된 Collider는 충돌 대상에서 제외합니다.</summary>
        private bool ShouldIgnoreCollider(Collider other)
        {
            return other == null
                || other == boxCollider
                || (owner != null && other.transform.IsChildOf(owner));
        }

        /// <summary>중복 반환을 막고 이 투사체를 소유한 풀에 돌려보냅니다.</summary>
        private void RequestRelease()
        {
            if (releaseRequested)
                return;

            releaseRequested = true;
            initialized = false;
            releaseHandler?.Invoke(this);
        }

        /// <summary>풀에 들어갈 때 이전 발사의 소유자, 콜백, 적중 기록을 비웁니다.</summary>
        private void OnDisable()
        {
            initialized = false;
            releaseRequested = false;
            owner = null;
            releaseHandler = null;
            hitTargets.Clear();
        }
    }
}
