using System.Collections.Generic;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 확정된 런 시드에서 콘텐츠 영역별 난수 스트림을 제공하는 서비스입니다.
    /// </summary>
    public interface ISeedService
    {
        int RunSeed { get; }
        string FormattedRunSeed { get; }

        IRandomStream GetStream(string streamName);
        IReadOnlyList<RandomStreamState> CaptureAllStreamStates();
        void RestoreStreamStates(IReadOnlyList<RandomStreamState> savedStates);
    }
}
