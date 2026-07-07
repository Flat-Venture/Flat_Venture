using System.Collections.Generic;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 하나의 콘텐츠 영역에서 순서대로 소비하는 재현 가능한 난수 스트림입니다.
    /// </summary>
    public interface IRandomStream
    {
        string Name { get; }
        ulong CallCount { get; }

        uint NextUInt32();
        int Range(int minimumInclusive, int maximumExclusive);
        float Range(float minimumInclusive, float maximumExclusive);
        bool Chance(float probability);
        int WeightedIndex(IReadOnlyList<float> weights);
        T Pick<T>(IReadOnlyList<T> candidates);
        T WeightedPick<T>(IReadOnlyList<T> candidates, IReadOnlyList<float> weights);
        void Shuffle<T>(IList<T> values);
        RandomStreamState CaptureState();
        void RestoreState(RandomStreamState savedState);
    }
}
