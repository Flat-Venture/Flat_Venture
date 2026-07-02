using UnityEngine;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>
    /// OnTrigger 이벤트로 플레이어에게 반복 피해를 주는 테스트 전용 구역입니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDamageZone : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float damage = 10f;
        [Min(0.05f)] [SerializeField] private float repeatInterval = 0.5f;

        private float nextDamageTime;
        private PlayerController playerInside;

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
            if (Time.time < nextDamageTime) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            //playerInside = player;
            TryApplyDamage(player);
        }

        private void TryApplyDamage(PlayerController player)
        {
            if (player.TakeDamage(damage))
                nextDamageTime = Time.time + repeatInterval;
        }
    }
}
