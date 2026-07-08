using FlatVenture.NUH.Player.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>조준 방향과 기본 공격 상태를 표시하는 테스트 전용 UI/시각화입니다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PlayerAttackDebugView : MonoBehaviour
    {
        // 상태를 읽을 실제 기본 공격 모듈, 플레이어, 출력 Text입니다.
        [SerializeField] private PlayerBasicAttackController attackController;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        // 조준 방향을 월드에 선으로 표시하고 공격 순간 잠깐 색을 바꾸기 위한 상태입니다.
        private LineRenderer aimLine;
        private float attackFlashRemaining;

        /// <summary>LineRenderer의 점 수, 두께와 테스트 재질을 준비합니다.</summary>
        private void Awake()
        {
            aimLine = GetComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.05f;
            aimLine.endWidth = 0.05f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>공격 실행 이벤트를 구독합니다.</summary>
        private void OnEnable()
        {
            if (attackController != null)
                attackController.AttackPerformed += OnAttackPerformed;
        }

        /// <summary>중복 구독을 막기 위해 이벤트를 해제합니다.</summary>
        private void OnDisable()
        {
            if (attackController != null)
                attackController.AttackPerformed -= OnAttackPerformed;
        }

        /// <summary>매 프레임 조준선 위치·색상과 공격 상태 Text를 갱신합니다.</summary>
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

        /// <summary>공격이 발생했음을 짧은 빨간 조준선으로 표시합니다.</summary>
        private void OnAttackPerformed(Vector3 direction, bool manual, int hitCount)
        {
            attackFlashRemaining = 0.12f;
        }
    }
}
