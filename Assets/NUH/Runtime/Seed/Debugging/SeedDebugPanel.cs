using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시드 생성, 직접 입력, 복사와 스트림 재현을 확인하는 테스트 전용 패널입니다.
/// </summary>
public sealed class SeedDebugPanel : MonoBehaviour
{
    // 버튼 한 번에 표시할 난수 수와 화면을 넘치지 않게 하는 출력 제한입니다.
    private const int ValuesPerRequest = 5;
    private const int MaximumOutputLength = 5000;
    private const int MaximumOutputLines = 14;

    // 사용자가 입력할 시드·스트림 이름과 상태·난수 결과 Text입니다.
    [SerializeField] private InputField seedInput;
    [SerializeField] private InputField streamNameInput;
    [SerializeField] private Text statusOutput;
    [SerializeField] private Text streamOutput;

    // 출력 문자열, 현재 확정된 서비스와 표시 중인 줄 수입니다.
    private readonly StringBuilder outputBuilder = new StringBuilder();
    private SeedService seedService;
    private int outputLineCount;

    public int CurrentRunSeed
    {
        get
        {
            if (seedService == null)
                return -1;
            return seedService.RunSeed;
        }
    }

    /// <summary>시드 입력에 숫자만 들어가게 하고 기본 스트림 이름을 준비합니다.</summary>
    private void Awake()
    {
        if (seedInput != null)
        {
            seedInput.characterLimit = SeedValue.DigitCount;
            seedInput.onValidateInput = ValidateSeedCharacter;
        }

        if (streamNameInput != null && string.IsNullOrWhiteSpace(streamNameInput.text))
            streamNameInput.text = SeedStreamNames.Shop;
    }

    /// <summary>테스트 씬이 시작되면 첫 무작위 시드를 자동 생성합니다.</summary>
    private void Start()
    {
        GenerateRandomSeed();
    }

    /// <summary>새 6자리 시드를 생성하고 모든 스트림을 처음 상태로 만듭니다.</summary>
    public void GenerateRandomSeed()
    {
        ApplySeed(SeedValue.Generate(), true);
        AppendSystemMessage("새 무작위 시드를 생성하고 적용했습니다.");
    }

    /// <summary>입력한 6자리 시드를 검증해 적용하며, 비어 있으면 무작위 생성합니다.</summary>
    public void ApplyInputSeed()
    {
        if (seedInput == null)
        {
            SetError("시드 입력 UI가 연결되지 않았습니다.");
            return;
        }

        if (string.IsNullOrEmpty(seedInput.text))
        {
            GenerateRandomSeed();
            return;
        }

        int parsedSeed;
        if (!SeedValue.TryParse(seedInput.text, out parsedSeed))
        {
            SetError("시드는 숫자 6자리로 입력해야 합니다.");
            return;
        }

        ApplySeed(parsedSeed, true);
        AppendSystemMessage("입력한 시드를 적용했습니다.");
    }

    /// <summary>현재 표시용 6자리 시드를 운영체제 클립보드에 복사합니다.</summary>
    public void CopyCurrentSeed()
    {
        if (!EnsureService())
            return;

        GUIUtility.systemCopyBuffer = seedService.FormattedRunSeed;
        AppendSystemMessage("현재 시드를 클립보드에 복사했습니다.");
    }

    /// <summary>같은 RunSeed로 서비스를 다시 만들어 모든 스트림을 첫 위치로 되돌립니다.</summary>
    public void ReplayCurrentSeed()
    {
        if (!EnsureService())
            return;

        int replaySeed = seedService.RunSeed;
        ApplySeed(replaySeed, false);
        AppendSystemMessage("동일 시드의 모든 스트림을 처음 상태로 되돌렸습니다.");
        DrawNextValues();
    }

    /// <summary>선택한 스트림에서 다음 32비트 값 5개를 소비하고 호출 번호와 함께 표시합니다.</summary>
    public void DrawNextValues()
    {
        if (!EnsureService())
            return;

        string streamName = GetStreamName();
        if (string.IsNullOrEmpty(streamName))
        {
            SetError("스트림 이름을 입력해야 합니다.");
            return;
        }

        IRandomStream stream = seedService.GetStream(streamName);
        ulong firstCall = stream.CallCount + 1;
        StringBuilder line = new StringBuilder();
        line.Append('[');
        line.Append(stream.Name);
        line.Append("] #");
        line.Append(firstCall);
        line.Append("~#");
        line.Append(firstCall + (ulong)ValuesPerRequest - 1UL);
        line.Append(" : ");

        for (int i = 0; i < ValuesPerRequest; i++)
        {
            if (i > 0)
                line.Append(", ");
            line.Append(stream.NextUInt32());
        }

        AppendOutput(line.ToString());
        RefreshStatus(stream);
        Debug.Log($"[SeedDebug] RunSeed={seedService.FormattedRunSeed}, Version={SeedService.SeedAlgorithmVersion}, {line}");
    }

    /// <summary>난수 결과 기록만 지우고 현재 시드와 스트림 상태는 유지합니다.</summary>
    public void ClearOutput()
    {
        outputBuilder.Clear();
        outputLineCount = 0;
        RefreshOutput();
        RefreshStatus(null);
    }

    // 기본 스트림 선택 버튼에 연결되는 짧은 진입점입니다.
    public void SelectMapStream() { SelectStream(SeedStreamNames.Map); }
    public void SelectMonsterStream() { SelectStream(SeedStreamNames.Monster); }
    public void SelectItemStream() { SelectStream(SeedStreamNames.Item); }
    public void SelectShopStream() { SelectStream(SeedStreamNames.Shop); }
    public void SelectForgeStream() { SelectStream(SeedStreamNames.Forge); }
    public void SelectEventStream() { SelectStream(SeedStreamNames.Event); }

    /// <summary>시드 입력란에 0~9 이외 문자가 입력되지 않게 걸러냅니다.</summary>
    private char ValidateSeedCharacter(string text, int characterIndex, char addedCharacter)
    {
        if (addedCharacter >= '0' && addedCharacter <= '9')
            return addedCharacter;
        return '\0';
    }

    /// <summary>새 SeedService를 만들고 입력·출력·상태 UI를 동기화합니다.</summary>
    private void ApplySeed(int seed, bool clearOutput)
    {
        seedService = new SeedService(seed);
        if (seedInput != null)
            seedInput.SetTextWithoutNotify(seedService.FormattedRunSeed);

        if (clearOutput)
        {
            outputBuilder.Clear();
            outputLineCount = 0;
        }

        RefreshOutput();
        RefreshStatus(null);
        Debug.Log($"[SeedDebug] RunSeed 적용: {seedService.FormattedRunSeed}, Version={SeedService.SeedAlgorithmVersion}");
    }

    /// <summary>선택된 스트림 이름을 입력란에 반영합니다.</summary>
    private void SelectStream(string streamName)
    {
        if (streamNameInput != null)
            streamNameInput.SetTextWithoutNotify(streamName);
        RefreshStatus(null);
    }

    /// <summary>스트림 이름 앞뒤 공백을 제거해 의도하지 않은 다른 스트림 생성을 막습니다.</summary>
    private string GetStreamName()
    {
        if (streamNameInput == null)
            return string.Empty;

        string streamName = streamNameInput.text.Trim();
        if (!string.Equals(streamName, streamNameInput.text, System.StringComparison.Ordinal))
            streamNameInput.SetTextWithoutNotify(streamName);
        return streamName;
    }

    /// <summary>시드가 아직 적용되지 않은 상태에서 API를 호출하지 않게 보호합니다.</summary>
    private bool EnsureService()
    {
        if (seedService != null)
            return true;

        SetError("먼저 시드를 생성하거나 적용해야 합니다.");
        return false;
    }

    private void AppendSystemMessage(string message)
    {
        AppendOutput($"- {message}");
        RefreshStatus(null);
    }

    /// <summary>오래된 출력 줄을 제거하고 새 한 줄을 결과 창에 추가합니다.</summary>
    private void AppendOutput(string line)
    {
        if (outputBuilder.Length > MaximumOutputLength)
        {
            outputBuilder.Clear();
            outputLineCount = 0;
        }

        if (outputLineCount >= MaximumOutputLines)
        {
            int firstLineEnd = IndexOfFirstLineEnd();
            if (firstLineEnd >= 0)
            {
                outputBuilder.Remove(0, firstLineEnd + 1);
                outputLineCount--;
            }
            else
            {
                outputBuilder.Clear();
                outputLineCount = 0;
            }
        }

        outputBuilder.AppendLine(line);
        outputLineCount++;
        RefreshOutput();
    }

    private int IndexOfFirstLineEnd()
    {
        for (int i = 0; i < outputBuilder.Length; i++)
        {
            if (outputBuilder[i] == '\n')
                return i;
        }

        return -1;
    }

    private void RefreshOutput()
    {
        if (streamOutput != null)
            streamOutput.text = outputBuilder.ToString();
    }

    /// <summary>현재 시드·알고리즘 버전·선택 스트림·호출 횟수를 표시합니다.</summary>
    private void RefreshStatus(IRandomStream stream)
    {
        if (statusOutput == null)
            return;

        if (seedService == null)
        {
            statusOutput.text = "RunSeed가 아직 적용되지 않았습니다.";
            return;
        }

        string selectedName = GetStreamName();
        if (stream == null && !string.IsNullOrEmpty(selectedName))
            stream = seedService.GetStream(selectedName);

        string callText = stream == null ? "스트림 미선택" : $"호출 {stream.CallCount}회";
        statusOutput.text =
            $"RunSeed {seedService.FormattedRunSeed} | Algorithm V{SeedService.SeedAlgorithmVersion}\n" +
            $"선택 스트림: {selectedName} | {callText}";
    }

    private void SetError(string message)
    {
        if (statusOutput != null)
            statusOutput.text = $"오류: {message}";
        Debug.LogWarning($"[SeedDebug] {message}");
    }
}
