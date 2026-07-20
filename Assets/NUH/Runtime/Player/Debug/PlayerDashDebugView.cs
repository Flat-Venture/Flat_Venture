using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대시 충전과 무적 상태를 테스트 씬에서 확인하기 위한 표시입니다.
/// </summary>
public sealed class PlayerDashDebugView : MonoBehaviour
{
    // 대시와 무적 상태를 읽을 대상과 결과를 표시할 Text입니다.
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerLocomotionController locomotion;
    [SerializeField] private Text output;

    /// <summary>매 프레임 대시 충전·회복시간·진행 여부·무적 상태를 화면에 표시합니다.</summary>
    private void Update()
    {
        if (player == null || locomotion == null || output == null || player.RuntimeState == null)
        {
            return;
        }

        output.text =
            $"Dash: {locomotion.DashCharges}/{locomotion.MaxDashCharges}\n" +
            $"Recharge: {locomotion.DashRechargeRemaining:0.00}s\n" +
            $"Dashing: {locomotion.IsDashing}\n" +
            $"Invincible: {player.RuntimeState.IsInvincible}";
    }
}
