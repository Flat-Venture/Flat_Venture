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
        // 플레이어 상태와 각 기능을 읽거나 테스트 버튼으로 조절하기 위한 참조입니다.
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerLocomotionController locomotion;
        [SerializeField] private PlayerHealthController health;
        [SerializeField] private WarriorActiveSkillController warriorActiveSkill;
        [SerializeField] private Text output;

        /// <summary>PlayerController가 준비한 현재 런타임 상태에 안전하게 접근합니다.</summary>
        private PlayerRuntimeState State
        {
            get
            {
                if (player == null)
                    return null;
                return player.RuntimeState;
            }
        }

        /// <summary>HP·능력치·대시·검기·풀 상태를 한 패널에 매 프레임 표시합니다.</summary>
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
                $"검기 쿨타임 {GetWarriorActiveSkillCooldown():0.##}s | 제거 {IsWarriorActiveSkillCooldownIgnored()}\n" +
                $"풀 {GetPoolActiveCount()} active / {GetPoolInactiveCount()} inactive";
        }

        // 아래 public 함수들은 씬의 UI Button Persistent Listener에 직접 연결됩니다.
        /// <summary>플레이어에게 테스트 피해 10을 줍니다.</summary>
        public void Damage10()
        {
            if (health != null)
                health.TakeDamage(10f);
        }

        /// <summary>현재 HP를 최대치까지 회복합니다.</summary>
        public void HealFull()
        {
            if (State != null)
                State.RestoreHealth();
        }

        /// <summary>Debug 사유의 무적만 독립적으로 전환합니다.</summary>
        public void ToggleInvincibility()
        {
            PlayerRuntimeState state = State;
            if (state == null)
                return;

            bool enable = (state.Invincibility & InvincibilityReason.Debug) == 0;
            state.SetInvincibility(InvincibilityReason.Debug, enable);
        }

        /// <summary>플레이어 위치와 모든 런타임 상태를 원본으로 초기화합니다.</summary>
        public void ResetPlayer()
        {
            if (player != null)
                player.ResetPlayer();
        }

        /// <summary>검기 쿨타임 무시 상태를 전환합니다.</summary>
        public void ToggleWarriorActiveSkillNoCooldown()
        {
            if (warriorActiveSkill != null)
                warriorActiveSkill.ToggleIgnoreCooldown();
        }

        // 증감 버튼은 같은 변경 함수를 재사용하며, 실제 안전 범위 제한은 PlayerRuntimeState가 담당합니다.
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

        /// <summary>검기 컨트롤러가 없을 때도 UI가 안전하게 0을 표시하도록 값을 읽습니다.</summary>
        private float GetWarriorActiveSkillCooldown()
        {
            return warriorActiveSkill != null ? warriorActiveSkill.CooldownRemaining : 0f;
        }

        private bool IsWarriorActiveSkillCooldownIgnored()
        {
            return warriorActiveSkill != null && warriorActiveSkill.IgnoreCooldown;
        }

        private int GetPoolActiveCount()
        {
            return warriorActiveSkill != null ? warriorActiveSkill.PoolActiveCount : 0;
        }

        private int GetPoolInactiveCount()
        {
            return warriorActiveSkill != null ? warriorActiveSkill.PoolInactiveCount : 0;
        }
    }
}
