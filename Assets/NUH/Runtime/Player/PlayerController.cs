using UnityEngine;

/// <summary>플레이어 런타임 상태의 생성과 원본값 초기화를 담당합니다.</summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public sealed class PlayerController : MonoBehaviour
{
    // 직업별 원본 능력치 SO입니다. 플레이 중에는 수정하지 않고 RuntimeState로 복사합니다.
    [SerializeField] private PlayerStatsData baseStats;

    // 위치 초기화 시 CharacterController를 잠시 껐다 켜기 위해 보관합니다.
    private CharacterController characterController;
    // 사망 후 비활성화된 입력 컴포넌트를 재시작할 때 다시 켜기 위해 보관합니다.
    private PlayerInputReader inputReader;
    // 씬이 시작될 때의 위치입니다. ResetPlayer가 플레이어를 이 위치로 되돌립니다.
    private Vector3 spawnPosition;

    /// <summary>SO 원본을 복사해 만든 현재 플레이의 가변 상태입니다.</summary>
    public PlayerRuntimeState RuntimeState { get; private set; }

    /// <summary>현재 런타임 상태의 원본으로 사용하는 직업 능력치 SO입니다.</summary>
    public PlayerStatsData BaseStats { get { return baseStats; } }

    /// <summary>
    /// 다른 컴포넌트의 Start보다 먼저 필요한 참조와 런타임 상태를 준비합니다.
    /// Unity의 Awake 호출 단계에서 실행됩니다.
    /// </summary>
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputReader = GetComponent<PlayerInputReader>();

        if (baseStats == null)
        {
            Debug.LogError("PlayerController에 PlayerStatsData가 설정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        RuntimeState = new PlayerRuntimeState();
        RuntimeState.Initialize(baseStats);
        spawnPosition = transform.position;
    }

    /// <summary>
    /// 플레이어를 시작 위치로 옮기고 능력치·HP·무적 상태를 원본값으로 초기화합니다.
    /// RuntimeState.Reset 이벤트를 통해 이동·공격·스킬 모듈도 함께 초기화됩니다.
    /// </summary>
    public void ResetPlayer()
    {
        if (RuntimeState == null)
            return;

        characterController.enabled = false;
        transform.position = spawnPosition;
        characterController.enabled = true;

        RuntimeState.Reset();
        inputReader.enabled = true;
    }

    /// <summary>
    /// 테스트 또는 직업 선택에서 사용할 원본 능력치 SO를 교체하고 런타임 상태를 새 직업 기준으로 초기화합니다.
    /// </summary>
    /// <param name="nextStats">새로 적용할 직업별 원본 능력치 SO입니다.</param>
    /// <param name="resetPosition">true면 시작 위치로 되돌리고, false면 현재 위치를 유지합니다.</param>
    public void ApplyStatsData(PlayerStatsData nextStats, bool resetPosition)
    {
        if (nextStats == null || RuntimeState == null)
            return;

        baseStats = nextStats;

        if (resetPosition)
        {
            characterController.enabled = false;
            transform.position = spawnPosition;
            characterController.enabled = true;
        }

        RuntimeState.Initialize(baseStats);
        inputReader.enabled = true;
    }
}
