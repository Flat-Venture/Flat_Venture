using UnityEngine;

namespace FlatVenture.NUH.Player.Combat
{
    /// <summary>플레이어 기본 공격을 독립적으로 시험하기 위한 최소 공격 대상 규격입니다.</summary>
    public interface IPlayerAttackTarget
    {
        /// <summary>거리 계산과 조준에 사용할 대상 Transform입니다.</summary>
        Transform TargetTransform { get; }
        /// <summary>죽은 대상을 다시 조준하거나 피해 주지 않기 위한 생존 여부입니다.</summary>
        bool IsAlive { get; }
        /// <summary>플레이어 공격이 계산한 최종 피해를 대상에 전달합니다.</summary>
        void TakeDamage(float damage);
    }
}
