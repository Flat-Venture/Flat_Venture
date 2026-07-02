using FlatVenture.NUH.Player.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>조준 방향과 기본 공격 상태를 표시하는 테스트 전용 UI/시각화입니다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PlayerAttackDebugView : MonoBehaviour
    {
        [SerializeField] private PlayerBasicAttackController attackController;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        private LineRenderer aimLine;
        private float attackFlashRemaining;

        private void Awake()
        {
            aimLine = GetComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.05f;
            aimLine.endWidth = 0.05f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void OnEnable()
        {
            if (attackController != null)
                attackController.AttackPerformed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            if (attackController != null)
                attackController.AttackPerformed -= OnAttackPerformed;
        }

        private void Update()
        {
            if (attackController == null || player?.RuntimeState == null)
                return;

            Vector3 start = player.transform.position + (Vector3.up * 0.1f);
            Vector3 end = start + (attackController.AimDirection * player.RuntimeState.BasicAttackRange);
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, end);

            attackFlashRemaining = Mathf.Max(0f, attackFlashRemaining - Time.deltaTime);
            aimLine.startColor = aimLine.endColor = attackFlashRemaining > 0f
                ? Color.red
                : attackController.IsManualAim ? Color.cyan : Color.yellow;

            if (output != null)
            {
                output.text =
                    $"Mode: {(attackController.IsManualAim ? "Manual" : "Auto")}\n" +
                    $"Target: {attackController.CurrentTargetName}\n" +
                    $"Cooldown: {attackController.CooldownRemaining:0.00}s";
            }
        }

        private void OnAttackPerformed(Vector3 direction, bool manual, int hitCount)
        {
            attackFlashRemaining = 0.12f;
        }
    }
}
