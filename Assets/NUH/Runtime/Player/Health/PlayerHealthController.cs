using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player.Health
{
    /// <summary>피해, 피격 후 무적 시간과 사망 시 입력 차단을 담당합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerHealthController : MonoBehaviour
    {
        // 상태·입력 모듈을 통해 HP 변화와 사망 입력 차단을 연결합니다.
        private PlayerController player;
        private PlayerInputReader inputReader;
        // 0보다 크면 피격 직후 무적시간이 진행 중입니다.
        private float hitInvincibilityRemaining;

        private PlayerRuntimeState State { get { return player.RuntimeState; } }

        /// <summary>같은 플레이어 오브젝트의 필수 참조를 캐시합니다.</summary>
        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
        }

        /// <summary>사망과 초기화 이벤트를 구독합니다.</summary>
        private void Start()
        {
            State.Died += OnDied;
            State.ResetCompleted += OnReset;
        }

        /// <summary>오브젝트 제거 시 상태 이벤트 구독을 해제합니다.</summary>
        private void OnDestroy()
        {
            if (State == null)
                return;
            State.Died -= OnDied;
            State.ResetCompleted -= OnReset;
        }

        /// <summary>게임 시간 기준으로 피격 무적시간을 감소시키고 종료 시 플래그를 해제합니다.</summary>
        private void Update()
        {
            if (hitInvincibilityRemaining <= 0f)
                return;

            hitInvincibilityRemaining = Mathf.Max(0f, hitInvincibilityRemaining - Time.deltaTime);
            if (hitInvincibilityRemaining <= 0f)
                State.SetInvincibility(InvincibilityReason.HitGrace, false);
        }

        /// <summary>런타임 상태에 피해 적용을 요청하고 생존했다면 피격 무적을 시작합니다.</summary>
        /// <returns>피해가 실제로 적용되었으면 true입니다.</returns>
        public bool TakeDamage(float amount)
        {
            if (!State.ApplyDamage(amount))
                return false;

            if (!State.IsDead && State.HitInvincibilityDuration > 0f)
            {
                hitInvincibilityRemaining = State.HitInvincibilityDuration;
                State.SetInvincibility(InvincibilityReason.HitGrace, true);
            }
            return true;
        }

        /// <summary>사망 즉시 PlayerInputReader를 꺼서 모든 플레이 입력을 차단합니다.</summary>
        private void OnDied()
        {
            inputReader.enabled = false;
        }

        /// <summary>재시작 시 남은 피격 무적시간과 플래그를 제거합니다.</summary>
        private void OnReset()
        {
            hitInvincibilityRemaining = 0f;
            State.SetInvincibility(InvincibilityReason.HitGrace, false);
        }
    }
}
