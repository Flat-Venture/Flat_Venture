using FlatVenture.NUH.Player.State;
using FlatVenture.NUH.Player.Health;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>HP, 피격 무적, 사망을 검증하는 테스트 전용 UI입니다.</summary>
    public sealed class PlayerHealthDebugView : MonoBehaviour
    {
        // 상태 초기화, 피해 전달, 결과 출력을 연결하는 테스트용 참조입니다.
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealthController health;
        [SerializeField] private Text output;

        /// <summary>현재 HP·무적 사유·사망 여부를 매 프레임 표시합니다.</summary>
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

        /// <summary>UI 버튼에서 일반 피해 10을 적용합니다.</summary>
        public void ApplyTestDamage()
        {
            if (health != null)
                health.TakeDamage(10f);
        }

        /// <summary>다른 무적 사유와 독립적인 Debug 무적 플래그를 켜거나 끕니다.</summary>
        public void ToggleDebugInvincibility()
        {
            if (player?.RuntimeState == null)
                return;

            bool enable = (player.RuntimeState.Invincibility & InvincibilityReason.Debug) == 0;
            player.RuntimeState.SetInvincibility(InvincibilityReason.Debug, enable);
        }

        /// <summary>UI 버튼에서 플레이어 전체 초기화를 실행합니다.</summary>
        public void ResetPlayer()
        {
            if (player != null)
                player.ResetPlayer();
        }
    }
}
