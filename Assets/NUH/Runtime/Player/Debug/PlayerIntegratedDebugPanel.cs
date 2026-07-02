using FlatVenture.NUH.Player.Health;
using FlatVenture.NUH.Player.Movement;
using FlatVenture.NUH.Player.State;
using FlatVenture.NUH.Player.Skills.Warrior;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>플레이어 상태 확인과 런타임 능력치 조절을 한곳에서 수행하는 테스트 전용 패널입니다.</summary>
    public sealed class PlayerIntegratedDebugPanel : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerLocomotionController locomotion;
        [SerializeField] private PlayerHealthController health;
        [SerializeField] private WarriorSwordWaveController swordWave;
        [SerializeField] private Text output;

        private PlayerRuntimeState State
        {
            get
            {
                if (player == null)
                    return null;
                return player.RuntimeState;
            }
        }

        private void Update()
        {
            PlayerRuntimeState state = State;
            if (state == null || locomotion == null || output == null)
                return;

            output.text =
                $"[플레이어 통합 테스트]\n" +
                $"HP {state.CurrentHealth:0.#}/{state.MaxHealth:0.#} | 무적 {state.Invincibility}\n" +
                $"이속 {state.MoveSpeed:0.##} | 공격력 {state.AttackPower:0.##} | 공속 {state.AttacksPerSecond:0.##}\n" +
                $"기본공격 범위 {state.BasicAttackRange:0.##} | 폭 {state.BasicAttackWidth:0.##}\n" +
                $"대시 거리 {state.DashDistance:0.##} | 충전 {locomotion.DashCharges}/{state.MaxDashCharges}\n" +
                $"대시 회복 {state.DashRechargeCooldown:0.##}s | 남은 시간 {locomotion.DashRechargeRemaining:0.##}s\n" +
                $"검기 피해 {state.ActiveSkillDamage:0.##} | 속도 {state.ActiveSkillProjectileSpeed:0.##}\n" +
                $"검기 폭 {state.ActiveSkillProjectileWidth:0.##} | 시전 {state.ActiveSkillCastTime:0.##}s\n" +
                $"검기 쿨타임 {GetSwordWaveCooldown():0.##}s | 제거 {IsSwordWaveCooldownIgnored()}\n" +
                $"풀 {GetPoolActiveCount()} active / {GetPoolInactiveCount()} inactive";
        }

        public void Damage10()
        {
            if (health != null)
                health.TakeDamage(10f);
        }

        public void HealFull()
        {
            if (State != null)
                State.RestoreHealth();
        }

        public void ToggleInvincibility()
        {
            PlayerRuntimeState state = State;
            if (state == null)
                return;

            bool enable = (state.Invincibility & InvincibilityReason.Debug) == 0;
            state.SetInvincibility(InvincibilityReason.Debug, enable);
        }

        public void ResetPlayer()
        {
            if (player != null)
                player.ResetPlayer();
        }

        public void ToggleSwordWaveNoCooldown()
        {
            if (swordWave != null)
                swordWave.ToggleIgnoreCooldown();
        }

        public void MoveSpeedDown() { ChangeMoveSpeed(-1f); }
        public void MoveSpeedUp() { ChangeMoveSpeed(1f); }
        public void AttackPowerDown() { ChangeAttackPower(-5f); }
        public void AttackPowerUp() { ChangeAttackPower(5f); }
        public void AttackSpeedDown() { ChangeAttackSpeed(-0.1f); }
        public void AttackSpeedUp() { ChangeAttackSpeed(0.1f); }
        public void DashDistanceDown() { ChangeDashDistance(-0.5f); }
        public void DashDistanceUp() { ChangeDashDistance(0.5f); }
        public void DashCooldownDown() { ChangeDashCooldown(-0.25f); }
        public void DashCooldownUp() { ChangeDashCooldown(0.25f); }
        public void DashChargesDown() { ChangeDashCharges(-1); }
        public void DashChargesUp() { ChangeDashCharges(1); }
        public void SkillDamageDown() { ChangeSkillDamage(-5f); }
        public void SkillDamageUp() { ChangeSkillDamage(5f); }
        public void SkillSpeedDown() { ChangeSkillSpeed(-2f); }
        public void SkillSpeedUp() { ChangeSkillSpeed(2f); }
        public void SkillWidthDown() { ChangeSkillWidth(-0.25f); }
        public void SkillWidthUp() { ChangeSkillWidth(0.25f); }
        public void SkillCastTimeDown() { ChangeSkillCastTime(-0.1f); }
        public void SkillCastTimeUp() { ChangeSkillCastTime(0.1f); }

        private void ChangeMoveSpeed(float delta)
        {
            if (State != null) State.SetMoveSpeed(State.MoveSpeed + delta);
        }

        private void ChangeAttackPower(float delta)
        {
            if (State != null) State.SetAttackPower(State.AttackPower + delta);
        }

        private void ChangeAttackSpeed(float delta)
        {
            if (State != null) State.SetAttacksPerSecond(State.AttacksPerSecond + delta);
        }

        private void ChangeDashDistance(float delta)
        {
            if (State != null) State.SetDashDistance(State.DashDistance + delta);
        }

        private void ChangeDashCooldown(float delta)
        {
            if (State != null) State.SetDashRechargeCooldown(State.DashRechargeCooldown + delta);
        }

        private void ChangeDashCharges(int delta)
        {
            if (State != null && locomotion != null)
                locomotion.SetDebugMaxDashCharges(State.MaxDashCharges + delta);
        }

        private void ChangeSkillDamage(float delta)
        {
            if (State != null) State.SetActiveSkillDamage(State.ActiveSkillDamage + delta);
        }

        private void ChangeSkillSpeed(float delta)
        {
            if (State != null) State.SetActiveSkillProjectileSpeed(State.ActiveSkillProjectileSpeed + delta);
        }

        private void ChangeSkillWidth(float delta)
        {
            if (State != null) State.SetActiveSkillProjectileWidth(State.ActiveSkillProjectileWidth + delta);
        }

        private void ChangeSkillCastTime(float delta)
        {
            if (State != null) State.SetActiveSkillCastTime(State.ActiveSkillCastTime + delta);
        }

        private float GetSwordWaveCooldown()
        {
            return swordWave != null ? swordWave.CooldownRemaining : 0f;
        }

        private bool IsSwordWaveCooldownIgnored()
        {
            return swordWave != null && swordWave.IgnoreCooldown;
        }

        private int GetPoolActiveCount()
        {
            return swordWave != null ? swordWave.PoolActiveCount : 0;
        }

        private int GetPoolInactiveCount()
        {
            return swordWave != null ? swordWave.PoolInactiveCount : 0;
        }
    }
}
