using System;
using System.Collections.Generic;

/// <summary>
/// 던전 입장 시 확정한 런 시드로 이름별 독립 난수 스트림을 제공합니다.
/// 인스턴스를 새로 만들기 전까지 런 시드는 변경되지 않습니다.
/// </summary>
public sealed class SeedService : ISeedService
{
    // 난수 결과 호환 규칙이 바뀔 때만 증가시키는 고정 버전입니다.
    public const int SeedAlgorithmVersion = 1;

    // 이름 비교는 문화권 영향을 받지 않는 Ordinal 규칙을 사용합니다.
    private readonly Dictionary<string, RandomStream> streams =
        new Dictionary<string, RandomStream>(StringComparer.Ordinal);

    public int RunSeed { get; private set; }
    public string FormattedRunSeed { get { return SeedValue.Format(RunSeed); } }

    /// <summary>던전 입장 시 확정한 시드를 검증하고 변경 불가능한 서비스로 만듭니다.</summary>
    public SeedService(int runSeed)
    {
        SeedValue.Validate(runSeed);
        RunSeed = runSeed;
    }

    /// <summary>
    /// RunSeed와 이름을 안정적으로 조합해 독립 스트림을 만들거나 이미 만든 스트림을 반환합니다.
    /// 같은 이름을 다시 요청하면 소비 위치가 이어집니다.
    /// </summary>
    public IRandomStream GetStream(string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(streamName));

        RandomStream stream;
        if (streams.TryGetValue(streamName, out stream))
            return stream;

        uint derivedSeed = StableSeedHash.Derive(RunSeed, streamName);
        stream = new RandomStream(streamName, derivedSeed, RunSeed);
        streams.Add(streamName, stream);
        return stream;
    }

    /// <summary>세이브 결과가 실행 순서에 흔들리지 않도록 스트림 이름순으로 상태를 복사합니다.</summary>
    public IReadOnlyList<RandomStreamState> CaptureAllStreamStates()
    {
        List<string> names = new List<string>(streams.Keys);
        names.Sort(StringComparer.Ordinal);

        List<RandomStreamState> states = new List<RandomStreamState>(names.Count);
        for (int i = 0; i < names.Count; i++)
            states.Add(streams[names[i]].CaptureState());

        return states;
    }

    /// <summary>중복 이름을 거부하고 각 저장 상태를 같은 이름의 스트림에 복원합니다.</summary>
    public void RestoreStreamStates(IReadOnlyList<RandomStreamState> savedStates)
    {
        if (savedStates == null)
            throw new ArgumentNullException(nameof(savedStates));

        HashSet<string> restoredNames = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < savedStates.Count; i++)
        {
            RandomStreamState savedState = savedStates[i];
            if (savedState == null)
                throw new ArgumentException("스트림 상태 목록에 null이 포함될 수 없습니다.", nameof(savedStates));

            if (!restoredNames.Add(savedState.StreamName))
                throw new ArgumentException("같은 이름의 스트림 상태가 중복되었습니다.", nameof(savedStates));

            IRandomStream stream = GetStream(savedState.StreamName);
            stream.RestoreState(savedState);
        }
    }
}
