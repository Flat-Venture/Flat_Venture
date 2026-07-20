using UnityEngine;

/// <summary>
/// 직업별 액티브 스킬 컨트롤러가 디버그 UI와 통합 테스트에 공통으로 공개하는 실행 상태입니다.
/// </summary>
public interface IPlayerActiveSkillStatus
{
    /// <summary>현재 조준 또는 마지막 시전에 사용할 XZ 방향입니다.</summary>
    Vector3 AimDirection { get; }

    /// <summary>다음 액티브 스킬을 사용할 수 있을 때까지 남은 게임 시간입니다.</summary>
    float CooldownRemaining { get; }

    /// <summary>우클릭을 누른 채 방향을 조정하는 중인지 나타냅니다.</summary>
    bool IsAiming { get; }

    /// <summary>선딜레이 또는 발사 Coroutine이 진행 중인지 나타냅니다.</summary>
    bool IsCasting { get; }

    /// <summary>테스트용으로 쿨타임을 무시하는 상태인지 나타냅니다.</summary>
    bool IgnoreCooldown { get; }

    /// <summary>현재 사용 중인 풀링 투사체 수입니다.</summary>
    int PoolActiveCount { get; }

    /// <summary>풀 안에서 대기 중인 투사체 수입니다.</summary>
    int PoolInactiveCount { get; }

    /// <summary>풀에서 관리하는 전체 투사체 수입니다.</summary>
    int PoolTotalCount { get; }

    /// <summary>테스트용 쿨타임 무시 상태를 켜거나 끕니다.</summary>
    void ToggleIgnoreCooldown();
}
