using System.Collections.Generic;

/// <summary>
/// 하나의 콘텐츠 영역에서 순서대로 소비하는 재현 가능한 난수 스트림입니다.
/// </summary>
public interface IRandomStream
{
    /// <summary>RunSeed에서 독립 시드를 파생할 때 사용한 스트림 이름입니다.</summary>
    string Name { get; }
    /// <summary>이 스트림이 지금까지 소비한 원본 32비트 난수 개수입니다.</summary>
    ulong CallCount { get; }

    /// <summary>다음 MT19937 32비트 값을 한 개 소비합니다.</summary>
    uint NextUInt32();
    /// <summary>최솟값 포함, 최댓값 제외 정수를 편향 없이 선택합니다.</summary>
    int Range(int minimumInclusive, int maximumExclusive);
    /// <summary>최솟값 포함, 최댓값 제외 실수를 선택합니다.</summary>
    float Range(float minimumInclusive, float maximumExclusive);
    /// <summary>0~1 확률로 성공 여부를 결정합니다.</summary>
    bool Chance(float probability);
    /// <summary>가중치에 비례해 선택된 인덱스를 반환합니다.</summary>
    int WeightedIndex(IReadOnlyList<float> weights);
    /// <summary>후보 목록에서 하나를 균등 선택합니다.</summary>
    T Pick<T>(IReadOnlyList<T> candidates);
    /// <summary>후보와 같은 순서의 가중치를 사용해 하나를 선택합니다.</summary>
    T WeightedPick<T>(IReadOnlyList<T> candidates, IReadOnlyList<float> weights);
    /// <summary>전달한 목록 자체를 Fisher-Yates 방식으로 섞습니다.</summary>
    void Shuffle<T>(IList<T> values);
    /// <summary>현재 소비 위치를 이어갈 수 있는 상태 복사본을 만듭니다.</summary>
    RandomStreamState CaptureState();
    /// <summary>같은 RunSeed·이름·버전의 저장 상태를 복원합니다.</summary>
    void RestoreState(RandomStreamState savedState);
}
