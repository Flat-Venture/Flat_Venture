using System;
using FlatVenture.NUH.Player.Data;
using UnityEngine;

namespace FlatVenture.NUH.Player.State
{
    [Flags]
    public enum InvincibilityReason
    {
        None = 0,
        Dash = 1 << 0,
        HitGrace = 1 << 1,
        Debug = 1 << 2
    }

    /// <summary>
    /// 한 플레이 동안 변하는 플레이어 상태입니다. ScriptableObject 원본과 분리됩니다.
    /// </summary>
    public sealed class PlayerRuntimeState
    {
        public PlayerStatsData Source { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public InvincibilityReason Invincibility { get; private set; }
        public bool IsInvincible => Invincibility != InvincibilityReason.None;

        public float MaxHealth => Source != null ? Source.MaxHealth : 0f;
        public float MoveSpeed => Source != null ? Source.MoveSpeed : 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public event Action ResetCompleted;

        public void Initialize(PlayerStatsData source)
        {
            Source = source != null
                ? source
                : throw new ArgumentNullException(nameof(source));

            Reset();
        }

        public void Reset()
        {
            if (Source == null)
            {
                throw new InvalidOperationException("PlayerStatsData를 먼저 설정해야 합니다.");
            }

            IsDead = false;
            Invincibility = InvincibilityReason.None;
            CurrentHealth = Source.MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            ResetCompleted?.Invoke();
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || IsInvincible || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth > 0f)
            {
                return;
            }

            IsDead = true;
            Died?.Invoke();
        }

        public void SetInvincibility(InvincibilityReason reason, bool active)
        {
            if (active)
                Invincibility |= reason;
            else
                Invincibility &= ~reason;
        }
    }
}
