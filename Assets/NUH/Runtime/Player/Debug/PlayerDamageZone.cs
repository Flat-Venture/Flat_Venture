using FlatVenture.NUH.Player.Health;
using UnityEngine;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>OnTriggerStay 이벤트로 반복 피해를 검증하는 테스트 전용 구역입니다.</summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDamageZone : MonoBehaviour
    {
        // 한 번에 줄 피해량과 같은 구역에서 다시 피해를 줄 최소 간격입니다.
        [Min(0f)] [SerializeField] private float damage = 10f;
        [Min(0.05f)] [SerializeField] private float repeatInterval = 0.5f;
        // Time.time이 이 값보다 커졌을 때 다음 피해를 시도할 수 있습니다.
        private float nextDamageTime;

        /// <summary>Trigger가 안정적으로 물리 이벤트를 받도록 Collider와 Rigidbody를 설정합니다.</summary>
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

        /// <summary>플레이어가 구역 안에 머무는 동안 정해진 간격으로 피해 이벤트를 전달합니다.</summary>
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
