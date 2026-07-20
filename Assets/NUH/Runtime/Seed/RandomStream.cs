using System;
using System.Collections.Generic;

/// <summary>
/// MT19937 값을 순서대로 소비하는 기본 난수 스트림입니다.
/// </summary>
public sealed class RandomStream : IRandomStream
{
    // uint 전체 경우의 수와 실수 변환에 사용할 정확한 2의 거듭제곱 상수입니다.
    private const ulong UInt32ValueCount = 4294967296UL;
    private const double UInt32ValueCountAsDouble = 4294967296.0;
    private const float UInt24ValueCountAsFloat = 16777216f;

    // 이 스트림만 사용하는 독립 MT19937 인스턴스입니다.
    private readonly MersenneTwister19937 generator;
    // 다른 RunSeed 상태 복원을 막기 위해 스트림에도 원본 RunSeed를 보관합니다.
    private readonly int runSeed;

    public string Name { get; private set; }
    public ulong CallCount { get; private set; }

    /// <summary>파생 시드로 독립 MT19937을 만들고 스트림 이름을 고정합니다.</summary>
    internal RandomStream(string name, uint seed, int runSeed)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(name));

        Name = name;
        this.runSeed = runSeed;
        generator = new MersenneTwister19937(seed);
    }

    /// <summary>호출 횟수를 증가시키고 다음 원본 난수를 반환합니다.</summary>
    public uint NextUInt32()
    {
        CallCount++;
        return generator.NextUInt32();
    }

    /// <summary>
    /// Modulo 편향을 제거하기 위해 나머지 구간의 값을 거부하고 정수 범위로 변환합니다.
    /// 최솟값은 포함하고 최댓값은 제외합니다.
    /// </summary>
    public int Range(int minimumInclusive, int maximumExclusive)
    {
        if (minimumInclusive >= maximumExclusive)
            throw new ArgumentOutOfRangeException(nameof(maximumExclusive), "최댓값은 최솟값보다 커야 합니다.");

        ulong range = (ulong)((long)maximumExclusive - minimumInclusive);
        uint random;

        if (range == UInt32ValueCount)
        {
            random = NextUInt32();
        }
        else
        {
            uint range32 = (uint)range;
            uint rejectionThreshold = unchecked(0u - range32) % range32;
            do
            {
                random = NextUInt32();
            }
            while (random < rejectionThreshold);

            random %= range32;
        }

        long result = minimumInclusive + (long)random;
        return (int)result;
    }

    /// <summary>상위 24비트로 [0,1) 값을 만든 뒤 요청한 실수 범위로 변환합니다.</summary>
    public float Range(float minimumInclusive, float maximumExclusive)
    {
        if (float.IsNaN(minimumInclusive) || float.IsInfinity(minimumInclusive))
            throw new ArgumentOutOfRangeException(nameof(minimumInclusive));

        if (float.IsNaN(maximumExclusive) || float.IsInfinity(maximumExclusive))
            throw new ArgumentOutOfRangeException(nameof(maximumExclusive));

        if (minimumInclusive >= maximumExclusive)
            throw new ArgumentOutOfRangeException(nameof(maximumExclusive), "최댓값은 최솟값보다 커야 합니다.");

        float normalized = (NextUInt32() >> 8) / UInt24ValueCountAsFloat;
        double span = (double)maximumExclusive - minimumInclusive;
        float result = (float)(minimumInclusive + (span * normalized));
        if (result >= maximumExclusive)
            return minimumInclusive;

        return result;
    }

    /// <summary>
    /// 다음 난수를 [0,1)로 변환해 probability보다 작은지 판정합니다.
    /// 확률 0과 1도 호출 순서를 유지하기 위해 난수 하나를 소비합니다.
    /// </summary>
    public bool Chance(float probability)
    {
        if (float.IsNaN(probability) || probability < 0f || probability > 1f)
            throw new ArgumentOutOfRangeException(nameof(probability), "확률은 0 이상 1 이하여야 합니다.");

        double normalized = NextUInt32() / UInt32ValueCountAsDouble;
        return normalized < probability;
    }

    /// <summary>누적 가중치 구간 중 난수가 들어간 후보 인덱스를 반환합니다.</summary>
    public int WeightedIndex(IReadOnlyList<float> weights)
    {
        if (weights == null)
            throw new ArgumentNullException(nameof(weights));

        if (weights.Count == 0)
            throw new ArgumentException("가중치 목록은 비어 있을 수 없습니다.", nameof(weights));

        double totalWeight = 0.0;
        for (int i = 0; i < weights.Count; i++)
        {
            float weight = weights[i];
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                throw new ArgumentOutOfRangeException(nameof(weights), "가중치는 유한한 0 이상의 값이어야 합니다.");

            totalWeight += weight;
        }

        if (totalWeight <= 0.0)
            throw new ArgumentException("하나 이상의 가중치는 0보다 커야 합니다.", nameof(weights));

        double selection = (NextUInt32() / UInt32ValueCountAsDouble) * totalWeight;
        double accumulated = 0.0;
        int lastPositiveIndex = -1;

        for (int i = 0; i < weights.Count; i++)
        {
            float weight = weights[i];
            if (weight <= 0f)
                continue;

            lastPositiveIndex = i;
            accumulated += weight;
            if (selection < accumulated)
                return i;
        }

        return lastPositiveIndex;
    }

    /// <summary>균등한 정수 인덱스를 뽑아 후보 하나를 반환합니다.</summary>
    public T Pick<T>(IReadOnlyList<T> candidates)
    {
        ValidateCandidates(candidates);
        return candidates[Range(0, candidates.Count)];
    }

    /// <summary>WeightedIndex 결과와 같은 위치의 후보를 반환합니다.</summary>
    public T WeightedPick<T>(IReadOnlyList<T> candidates, IReadOnlyList<float> weights)
    {
        ValidateCandidates(candidates);
        if (weights == null)
            throw new ArgumentNullException(nameof(weights));

        if (candidates.Count != weights.Count)
            throw new ArgumentException("후보와 가중치의 개수가 같아야 합니다.", nameof(weights));

        return candidates[WeightedIndex(weights)];
    }

    /// <summary>마지막 원소부터 무작위 위치와 바꾸는 Fisher-Yates 셔플입니다.</summary>
    public void Shuffle<T>(IList<T> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        for (int i = values.Count - 1; i > 0; i--)
        {
            int selectedIndex = Range(0, i + 1);
            T temporary = values[i];
            values[i] = values[selectedIndex];
            values[selectedIndex] = temporary;
        }
    }

    /// <summary>현재 스트림을 정확한 위치에서 이어가기 위한 모든 상태를 복사합니다.</summary>
    public RandomStreamState CaptureState()
    {
        return new RandomStreamState
        {
            AlgorithmVersion = SeedService.SeedAlgorithmVersion,
            RunSeed = runSeed,
            StreamName = Name,
            GeneratorState = generator.CaptureState(),
            GeneratorIndex = generator.CaptureIndex(),
            CallCount = CallCount
        };
    }

    /// <summary>버전·RunSeed·이름을 검증한 뒤 MT 내부 상태와 호출 횟수를 복원합니다.</summary>
    public void RestoreState(RandomStreamState savedState)
    {
        if (savedState == null)
            throw new ArgumentNullException(nameof(savedState));

        if (savedState.AlgorithmVersion != SeedService.SeedAlgorithmVersion)
            throw new ArgumentException("현재 난수 알고리즘 버전과 저장된 버전이 다릅니다.", nameof(savedState));

        if (savedState.RunSeed != runSeed)
            throw new ArgumentException("다른 RunSeed에서 만든 스트림 상태는 복원할 수 없습니다.", nameof(savedState));

        if (!string.Equals(savedState.StreamName, Name, StringComparison.Ordinal))
            throw new ArgumentException("다른 이름의 스트림 상태는 복원할 수 없습니다.", nameof(savedState));

        generator.RestoreState(savedState.GeneratorState, savedState.GeneratorIndex);
        CallCount = savedState.CallCount;
    }

    /// <summary>Pick 계열 API가 null 또는 빈 후보로 호출되지 않게 공통 검사합니다.</summary>
    private static void ValidateCandidates<T>(IReadOnlyList<T> candidates)
    {
        if (candidates == null)
            throw new ArgumentNullException(nameof(candidates));

        if (candidates.Count == 0)
            throw new ArgumentException("후보 목록은 비어 있을 수 없습니다.", nameof(candidates));
    }
}
