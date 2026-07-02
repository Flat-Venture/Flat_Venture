using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player.Health
{
    /// <summary>피해, 피격 후 무적 시간과 사망 시 입력 차단을 담당합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerHealthController : MonoBehaviour
    {
        private PlayerController player;
        private PlayerInputReader inputReader;
        private float hitInvincibilityRemaining;

        private PlayerRuntimeState State { get { return player.RuntimeState; } }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
        }

        private void Start()
        {
            State.Died += OnDied;
            State.ResetCompleted += OnReset;
        }

        private void OnDestroy()
        {
            if (State == null)
                return;
            State.Died -= OnDied;
            State.ResetCompleted -= OnReset;
        }

        private void Update()
        {
            if (hitInvincibilityRemaining <= 0f)
                return;

            hitInvincibilityRemaining = Mathf.Max(0f, hitInvincibilityRemaining - Time.deltaTime);
            if (hitInvincibilityRemaining <= 0f)
                State.SetInvincibility(InvincibilityReason.HitGrace, false);
        }

        public bool TakeDamage(float amount)
        {
            if (!State.ApplyDamage(amount))
                return false;

            if (!State.IsDead && State.HitInvincibilityDuration > 0f)
            {
                hitInvincibilityRemaining = State.HitInvincibilityDuration;
                State.SetInvincibility(InvincibilityReason.HitGrace, true);
            }
            return true;
        }

        private void OnDied()
        {
            inputReader.enabled = false;
        }

        private void OnReset()
        {
            hitInvincibilityRemaining = 0f;
            State.SetInvincibility(InvincibilityReason.HitGrace, false);
        }
    }
}
