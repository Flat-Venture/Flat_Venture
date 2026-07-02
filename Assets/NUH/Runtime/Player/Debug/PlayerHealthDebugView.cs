using FlatVenture.NUH.Player.State;
using FlatVenture.NUH.Player.Health;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>HP, 피격 무적, 사망을 검증하는 테스트 전용 UI입니다.</summary>
    public sealed class PlayerHealthDebugView : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealthController health;
        [SerializeField] private Text output;

        private void Update()
        {
            if (player == null || output == null || player.RuntimeState == null)
                return;

            PlayerRuntimeState state = player.RuntimeState;
            output.text =
                $"HP: {state.CurrentHealth:0.##}/{state.MaxHealth:0.##}\n" +
                $"Invincible: {state.IsInvincible} ({state.Invincibility})\n" +
                $"Dead: {state.IsDead}";
        }

        public void ApplyTestDamage()
        {
            if (health != null)
                health.TakeDamage(10f);
        }

        public void ToggleDebugInvincibility()
        {
            if (player?.RuntimeState == null)
                return;

            bool enable = (player.RuntimeState.Invincibility & InvincibilityReason.Debug) == 0;
            player.RuntimeState.SetInvincibility(InvincibilityReason.Debug, enable);
        }

        public void ResetPlayer()
        {
            if (player != null)
                player.ResetPlayer();
        }
    }
}
