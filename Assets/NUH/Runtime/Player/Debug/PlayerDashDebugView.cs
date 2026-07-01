using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>
    /// 대시 충전과 무적 상태를 테스트 씬에서 확인하기 위한 표시입니다.
    /// </summary>
    public sealed class PlayerDashDebugView : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        private void Update()
        {
            if (player == null || output == null || player.RuntimeState == null)
            {
                return;
            }

            output.text =
                $"Dash: {player.DashCharges}/{player.MaxDashCharges}\n" +
                $"Recharge: {player.DashRechargeRemaining:0.00}s\n" +
                $"Dashing: {player.IsDashing}\n" +
                $"Invincible: {player.RuntimeState.IsInvincible}";
        }
    }
}
