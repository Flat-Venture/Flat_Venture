using System;
using UnityEngine;

namespace FlatVenture.NUH.Player.Combat
{
    /// <summary>기본 원거리 공격에 공통으로 사용하는 단일 적중 투사체입니다.</summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerBasicAttackProjectile : MonoBehaviour
    {
        // Kinematic 이동에 사용할 물리 본체입니다.
        private Rigidbody body;
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

        /// <summary>물리 본체와 Trigger Collider를 투사체 규칙에 맞게 설정합니다.</summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            GetComponent<BoxCollider>().isTrigger = true;
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
            body.MovePosition(body.position + (direction * distance));
            remainingDistance -= distance;
            if (remainingDistance <= 0f)
                RequestRelease();
        }

        /// <summary>첫 공격 대상에 피해를 주거나 벽에 닿으면 즉시 풀 반환을 요청합니다.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || (owner != null && other.transform.IsChildOf(owner)))
                return;

            IPlayerAttackTarget target = other.GetComponentInParent<IPlayerAttackTarget>();
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage);
                RequestRelease();
                return;
            }

            if (!other.isTrigger)
                RequestRelease();
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
        }
    }
}
