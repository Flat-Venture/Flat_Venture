using UnityEngine;

namespace FlatVenture.NUH.Player.Combat
{
    /// <summary>플레이어 기본 공격을 독립적으로 시험하기 위한 최소 공격 대상 규격입니다.</summary>
    public interface IPlayerAttackTarget
    {
        Transform TargetTransform { get; }
        bool IsAlive { get; }
        void TakeDamage(float damage);
    }
}
