using System;
using System.Collections.Generic;
using FlatVenture.NUH.Player.Combat;
using UnityEngine;

namespace FlatVenture.NUH.Player.Skills.Warrior
{
    /// <summary>직선으로 이동하며 대상을 관통하고 벽에 닿으면 사라지는 전사 검기입니다.</summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WarriorSwordWaveProjectile : MonoBehaviour
    {
        private readonly HashSet<IPlayerAttackTarget> hitTargets = new HashSet<IPlayerAttackTarget>();
        private Rigidbody body;
        private Transform owner;
        private Vector3 direction;
        private float speed;
        private float damage;
        private int maxHitTargets;
        private bool initialized;
        private bool releaseRequested;
        private float lifetimeRemaining;
        private Action<WarriorSwordWaveProjectile> releaseHandler;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            GetComponent<BoxCollider>().isTrigger = true;
        }

        public void Initialize(
            Transform projectileOwner,
            Vector3 moveDirection,
            float moveSpeed,
            float projectileDamage,
            float width,
            int maximumHitTargets,
            Action<WarriorSwordWaveProjectile> onRelease,
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

        private void FollowNearbyGround(ref Vector3 nextPosition)
        {
            Vector3 rayOrigin = nextPosition + (Vector3.up * 2f);
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 4f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                return;

            float desiredY = hit.point.y + 0.45f;
            if (Mathf.Abs(desiredY - nextPosition.y) <= 0.75f)
                nextPosition.y = desiredY;
        }

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

        private void RequestRelease()
        {
            if (releaseRequested)
                return;

            releaseRequested = true;
            initialized = false;
            releaseHandler?.Invoke(this);
        }

        private void OnDisable()
        {
            initialized = false;
            releaseHandler = null;
            hitTargets.Clear();
        }
    }
}
