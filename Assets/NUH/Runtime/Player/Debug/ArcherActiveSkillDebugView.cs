using FlatVenture.NUH.Player.Skills.Archer;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>궁수 관통 사격의 조준, 시전, 쿨타임, 풀 상태를 표시하는 테스트 전용 UI입니다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ArcherActiveSkillDebugView : MonoBehaviour
    {
        // 관통 사격 상태를 읽을 컨트롤러, 플레이어 위치, 출력 Text입니다.
        [SerializeField] private ArcherActiveSkillController skill;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        // 우클릭 조준 중 사거리와 방향을 보여줄 선입니다.
        private LineRenderer aimLine;

        /// <summary>조준선의 점 수, 두께, 테스트 재질을 준비합니다.</summary>
        private void Awake()
        {
            aimLine = GetComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.06f;
            aimLine.endWidth = 0.06f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>조준선과 상태 Text를 매 프레임 갱신합니다.</summary>
        private void Update()
        {
            if (skill == null || player?.RuntimeState == null)
                return;

            aimLine.enabled = skill.IsAiming;
            if (skill.IsAiming)
            {
                Vector3 start = player.transform.position + (Vector3.up * 0.2f);
                aimLine.SetPosition(0, start);
                aimLine.SetPosition(1, start + (skill.AimDirection * player.RuntimeState.ActiveSkillRange));
                aimLine.startColor = aimLine.endColor = Color.green;
            }

            if (output != null)
            {
                output.text =
                    $"Piercing Shot\n" +
                    $"Aiming: {skill.IsAiming}\n" +
                    $"Casting: {skill.IsCasting}\n" +
                    $"Cooldown: {skill.CooldownRemaining:0.00}s\n" +
                    $"No Cooldown: {skill.IgnoreCooldown}\n" +
                    $"Range: {player.RuntimeState.ActiveSkillRange:0.0}m / Damage: {player.RuntimeState.ActiveSkillDamage:0}\n" +
                    $"Pool: {skill.PoolActiveCount} active / {skill.PoolInactiveCount} inactive / {skill.PoolTotalCount} total";
            }
        }

        /// <summary>UI 버튼에서 관통 사격 쿨타임 무시 상태를 전환합니다.</summary>
        public void ToggleNoCooldown()
        {
            if (skill != null)
                skill.ToggleIgnoreCooldown();
        }
    }
}
