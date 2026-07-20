using System.Collections.Generic;

/// <summary>
/// 확정된 런 시드에서 콘텐츠 영역별 난수 스트림을 제공하는 서비스입니다.
/// </summary>
public interface ISeedService
{
    /// <summary>이번 던전 런에서 변경하지 않는 숫자 시드입니다.</summary>
    int RunSeed { get; }
    /// <summary>앞자리 0을 포함한 사용자 표시용 6자리 시드입니다.</summary>
    string FormattedRunSeed { get; }

    /// <summary>이름에 해당하는 독립 스트림을 생성하거나 기존 인스턴스를 반환합니다.</summary>
    IRandomStream GetStream(string streamName);
    /// <summary>현재까지 만들어진 모든 스트림 상태를 이름순으로 복사합니다.</summary>
    IReadOnlyList<RandomStreamState> CaptureAllStreamStates();
    /// <summary>세이브 데이터에서 읽은 여러 스트림 상태를 복원합니다.</summary>
    void RestoreStreamStates(IReadOnlyList<RandomStreamState> savedStates);
}
