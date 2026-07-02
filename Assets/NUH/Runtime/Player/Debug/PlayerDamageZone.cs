using FlatVenture.NUH.Player.Health;
using UnityEngine;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>OnTriggerStay 이벤트로 반복 피해를 검증하는 테스트 전용 구역입니다.</summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDamageZone : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float damage = 10f;
        [Min(0.05f)] [SerializeField] private float repeatInterval = 0.5f;
        private float nextDamageTime;

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void OnTriggerStay(Collider other)
        {
            if (Time.time < nextDamageTime)
                return;

            PlayerHealthController health = other.GetComponentInParent<PlayerHealthController>();
            if (health == null)
                return;

            if (health.TakeDamage(damage))
                nextDamageTime = Time.time + repeatInterval;
        }
    }
}
