using System;

/// <summary>
/// 프로젝트의 재현 가능한 난수 생성을 담당하는 MT19937 구현입니다.
/// 알고리즘 버전 1의 일부이므로 기존 런의 재현성을 유지해야 한다면 구현을 변경하지 않습니다.
/// </summary>
internal sealed class MersenneTwister19937
{
    // MT19937이 다음 값을 만들기 위해 기억하는 32비트 상태 개수입니다.
    internal const int StateLength = 624;

    // 아래 상수는 MT19937 공식 알고리즘 값이며 변경하면 기존 시드 결과가 전부 달라집니다.
    private const int MiddleWord = 397;
    private const uint MatrixA = 0x9908B0DFu;
    private const uint UpperMask = 0x80000000u;
    private const uint LowerMask = 0x7FFFFFFFu;

    // 624개의 내부 상태와 다음에 읽을 배열 위치입니다.
    private readonly uint[] state = new uint[StateLength];
    private int index;

    /// <summary>정수 시드 하나로 624개 내부 상태를 초기화합니다.</summary>
    internal MersenneTwister19937(uint seed)
    {
        Initialize(seed);
    }

    /// <summary>필요하면 상태를 Twist한 뒤 Tempering을 적용한 다음 32비트 값을 반환합니다.</summary>
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

    /// <summary>외부에서 내부 배열을 바꾸지 못하도록 상태 배열을 새 배열로 복사합니다.</summary>
    internal uint[] CaptureState()
    {
        uint[] copy = new uint[StateLength];
        Array.Copy(state, copy, StateLength);
        return copy;
    }

    /// <summary>저장된 624개 상태와 다음 인덱스를 검증해 복원합니다.</summary>
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

    /// <summary>다음에 읽을 상태 배열 인덱스를 반환합니다.</summary>
    internal int CaptureIndex()
    {
        return index;
    }

    /// <summary>공식 MT19937 초기화 점화식으로 상태 배열을 채웁니다.</summary>
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

    /// <summary>624개 상태를 서로 섞어 다음 624개 난수의 기반 상태를 생성합니다.</summary>
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
