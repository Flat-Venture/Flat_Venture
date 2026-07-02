using FlatVenture.NUH.Player.Skills.Warrior;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>검기 조준·시전·쿨타임 상태를 표시하는 테스트 전용 UI입니다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class WarriorSwordWaveDebugView : MonoBehaviour
    {
        [SerializeField] private WarriorSwordWaveController skill;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text output;

        private LineRenderer aimLine;

        private void Awake()
        {
            aimLine = GetComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.08f;
            aimLine.endWidth = 0.08f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
        }

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

        public void ToggleNoCooldown() => skill?.ToggleIgnoreCooldown();
    }
}
