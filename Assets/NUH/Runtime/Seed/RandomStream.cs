using System;
using System.Collections.Generic;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// MT19937 값을 순서대로 소비하는 기본 난수 스트림입니다.
    /// </summary>
    public sealed class RandomStream : IRandomStream
    {
        private const ulong UInt32ValueCount = 4294967296UL;
        private const double UInt32ValueCountAsDouble = 4294967296.0;
        private const float UInt24ValueCountAsFloat = 16777216f;

        private readonly MersenneTwister19937 generator;
        private readonly int runSeed;

        public string Name { get; private set; }
        public ulong CallCount { get; private set; }

        internal RandomStream(string name, uint seed, int runSeed)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(name));

            Name = name;
            this.runSeed = runSeed;
            generator = new MersenneTwister19937(seed);
        }

        public uint NextUInt32()
        {
            CallCount++;
            return generator.NextUInt32();
        }

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

        public bool Chance(float probability)
        {
            if (float.IsNaN(probability) || probability < 0f || probability > 1f)
                throw new ArgumentOutOfRangeException(nameof(probability), "확률은 0 이상 1 이하여야 합니다.");

            double normalized = NextUInt32() / UInt32ValueCountAsDouble;
            return normalized < probability;
        }

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

        public T Pick<T>(IReadOnlyList<T> candidates)
        {
            ValidateCandidates(candidates);
            return candidates[Range(0, candidates.Count)];
        }

        public T WeightedPick<T>(IReadOnlyList<T> candidates, IReadOnlyList<float> weights)
        {
            ValidateCandidates(candidates);
            if (weights == null)
                throw new ArgumentNullException(nameof(weights));

            if (candidates.Count != weights.Count)
                throw new ArgumentException("후보와 가중치의 개수가 같아야 합니다.", nameof(weights));

            return candidates[WeightedIndex(weights)];
        }

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

        private static void ValidateCandidates<T>(IReadOnlyList<T> candidates)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            if (candidates.Count == 0)
                throw new ArgumentException("후보 목록은 비어 있을 수 없습니다.", nameof(candidates));
        }
    }
}
