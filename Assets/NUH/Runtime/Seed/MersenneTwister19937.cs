using System;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 프로젝트의 재현 가능한 난수 생성을 담당하는 MT19937 구현입니다.
    /// 알고리즘 버전 1의 일부이므로 기존 런의 재현성을 유지해야 한다면 구현을 변경하지 않습니다.
    /// </summary>
    internal sealed class MersenneTwister19937
    {
        internal const int StateLength = 624;

        private const int MiddleWord = 397;
        private const uint MatrixA = 0x9908B0DFu;
        private const uint UpperMask = 0x80000000u;
        private const uint LowerMask = 0x7FFFFFFFu;

        private readonly uint[] state = new uint[StateLength];
        private int index;

        internal MersenneTwister19937(uint seed)
        {
            Initialize(seed);
        }

        internal uint NextUInt32()
        {
            if (index >= StateLength)
                Twist();

            uint value = state[index];
            index++;

            value ^= value >> 11;
            value ^= (value << 7) & 0x9D2C5680u;
            value ^= (value << 15) & 0xEFC60000u;
            value ^= value >> 18;
            return value;
        }

        internal uint[] CaptureState()
        {
            uint[] copy = new uint[StateLength];
            Array.Copy(state, copy, StateLength);
            return copy;
        }

        internal void RestoreState(uint[] savedState, int savedIndex)
        {
            if (savedState == null)
                throw new ArgumentNullException(nameof(savedState));

            if (savedState.Length != StateLength)
                throw new ArgumentException("MT19937 상태 배열은 정확히 624개여야 합니다.", nameof(savedState));

            if (savedIndex < 0 || savedIndex > StateLength)
                throw new ArgumentOutOfRangeException(nameof(savedIndex));

            Array.Copy(savedState, state, StateLength);
            index = savedIndex;
        }

        internal int CaptureIndex()
        {
            return index;
        }

        private void Initialize(uint seed)
        {
            state[0] = seed;
            for (int i = 1; i < StateLength; i++)
            {
                uint previous = state[i - 1];
                state[i] = unchecked(1812433253u * (previous ^ (previous >> 30)) + (uint)i);
            }

            index = StateLength;
        }

        private void Twist()
        {
            for (int i = 0; i < StateLength; i++)
            {
                uint combined = (state[i] & UpperMask)
                    | (state[(i + 1) % StateLength] & LowerMask);
                uint next = state[(i + MiddleWord) % StateLength] ^ (combined >> 1);

                if ((combined & 1u) != 0u)
                    next ^= MatrixA;

                state[i] = next;
            }

            index = 0;
        }
    }
}
