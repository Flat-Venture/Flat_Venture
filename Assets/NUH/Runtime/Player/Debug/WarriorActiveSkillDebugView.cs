using FlatVenture.NUH.Player.Skills.Warrior;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>검기 조준·시전·쿨타임 상태를 표시하는 테스트 전용 UI입니다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class WarriorActiveSkillDebugView : MonoBehaviour
    {
        // 검기 상태와 플레이어 위치를 읽고 UI에 출력할 참조입니다.
        [SerializeField] private WarriorActiveSkillController skill;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        // 우클릭 조준 중인 검기 방향을 표시할 선입니다.
        private LineRenderer aimLine;

        /// <summary>검기 조준선의 모양과 테스트 재질을 준비합니다.</summary>
        private void Awake()
        {
            aimLine = GetComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.08f;
            aimLine.endWidth = 0.08f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>조준선과 시전·쿨타임·풀 상태 Text를 매 프레임 갱신합니다.</summary>
        private void Update()
        {
            if (skill == null || player?.RuntimeState == null)
                return;

            aimLine.enabled = skill.IsAiming;
            if (skill.IsAiming)
            {
                Vector3 start = player.transform.position + (Vector3.up * 0.2f);
                aimLine.SetPosition(0, start);
                aimLine.SetPosition(1, start + (skill.AimDirection * 12f));
                aimLine.startColor = aimLine.endColor = Color.magenta;
            }

            if (output != null)
            {
                output.text =
                    $"Sword Wave\n" +
                    $"Aiming: {skill.IsAiming}\n" +
                    $"Casting: {skill.IsCasting}\n" +
                    $"Cooldown: {skill.CooldownRemaining:0.00}s\n" +
                    $"No Cooldown: {skill.IgnoreCooldown}\n" +
                    $"Pool: {skill.PoolActiveCount} active / {skill.PoolInactiveCount} inactive / {skill.PoolTotalCount} total";
            }
        }

        /// <summary>UI 버튼에서 검기 쿨타임 무시 상태를 전환합니다.</summary>
        public void ToggleNoCooldown()
        {
            if (skill != null)
                skill.ToggleIgnoreCooldown();
        }
    }
}
