using System;
using System.Text;

/// <summary>
/// 런 시드와 스트림 이름을 플랫폼에 독립적인 32비트 시드로 조합합니다.
/// FNV-1a 처리 순서는 알고리즘 버전 1의 고정 규칙입니다.
/// </summary>
internal static class StableSeedHash
{
    // FNV-1a 32비트 공식 시작값과 소수입니다. 변경하면 모든 파생 스트림이 달라집니다.
    private const uint OffsetBasis = 2166136261u;
    private const uint Prime = 16777619u;

    /// <summary>
    /// RunSeed의 4바이트와 UTF-8 스트림 이름을 고정 순서로 FNV-1a 해시합니다.
    /// string.GetHashCode처럼 실행마다 달라질 수 있는 함수를 사용하지 않습니다.
    /// </summary>
    internal static uint Derive(int runSeed, string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(streamName));

        uint hash = OffsetBasis;
        hash = AddByte(hash, (byte)runSeed);
        hash = AddByte(hash, (byte)(runSeed >> 8));
        hash = AddByte(hash, (byte)(runSeed >> 16));
        hash = AddByte(hash, (byte)(runSeed >> 24));
        hash = AddByte(hash, 0xFF);

        byte[] nameBytes = Encoding.UTF8.GetBytes(streamName);
        for (int i = 0; i < nameBytes.Length; i++)
            hash = AddByte(hash, nameBytes[i]);

        return hash;
    }

    /// <summary>현재 해시에 바이트 하나를 FNV-1a 규칙으로 누적합니다.</summary>
    private static uint AddByte(uint hash, byte value)
    {
        return unchecked((hash ^ value) * Prime);
    }
}
