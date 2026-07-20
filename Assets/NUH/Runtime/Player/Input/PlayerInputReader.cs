using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 공유 InputActionAsset을 수정하지 않고 플레이어 입력값만 읽어 전달합니다.
/// </summary>
public sealed class PlayerInputReader : MonoBehaviour
{
    // 프로젝트에서 공유하는 InputActionAsset입니다. 이 컴포넌트는 값을 읽기만 합니다.
    [SerializeField] private InputActionAsset inputActions;
    // 아래 경로 문자열로 필요한 액션을 찾으므로 InputActions의 맵/액션 이름과 일치해야 합니다.
    [SerializeField] private string moveActionPath = "Player/Move";
    [SerializeField] private string dashActionPath = "Player/Dash";
    [SerializeField] private string manualAimActionPath = "Player/Attack";
    [SerializeField] private string pointerPositionActionPath = "Player/AimPosition";
    [SerializeField] private string activeSkillActionPath = "Player/ActiveSkill";

    // 경로 검색이 끝난 실제 InputAction 참조입니다. OnEnable에서 연결하고 OnDisable에서 해제합니다.
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction manualAimAction;
    private InputAction pointerPositionAction;
    private InputAction activeSkillAction;

    /// <summary>현재 WASD 이동 입력입니다. 대각선 속도 증가를 막기 위해 길이를 1로 제한합니다.</summary>
    public Vector2 Move { get; private set; }
    /// <summary>좌클릭을 누르고 있어 수동 조준 중인지 나타냅니다.</summary>
    public bool IsManualAimHeld { get; private set; }
    /// <summary>현재 마우스의 화면 좌표입니다. PlayerAimResolver가 월드 방향으로 변환합니다.</summary>
    public Vector2 PointerScreenPosition { get; private set; }

    // 버튼 입력은 한 번 발생하는 이벤트로 다른 모듈에 전달합니다.
    public event Action DashPressed;
    public event Action ActiveSkillPressed;
    public event Action ActiveSkillReleased;

    /// <summary>컴포넌트가 켜질 때 액션을 찾아 콜백을 구독하고 입력을 활성화합니다.</summary>
    private void OnEnable()
    {
        if (!TryResolveActions())
        {
            enabled = false;
            return;
        }

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        dashAction.performed += OnDashPerformed;
        manualAimAction.performed += OnManualAimPerformed;
        manualAimAction.canceled += OnManualAimCanceled;
        pointerPositionAction.performed += OnPointerPositionPerformed;
        activeSkillAction.performed += OnActiveSkillPerformed;
        activeSkillAction.canceled += OnActiveSkillCanceled;
        moveAction.Enable();
        dashAction.Enable();
        manualAimAction.Enable();
        pointerPositionAction.Enable();
        activeSkillAction.Enable();
    }

    /// <summary>
    /// 사망이나 오브젝트 비활성화 시 모든 콜백과 액션을 정리합니다.
    /// 입력값도 0으로 돌려 이전 프레임 입력이 남지 않게 합니다.
    /// </summary>
    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Disable();
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDashPerformed;
            dashAction.Disable();
        }

        if (manualAimAction != null)
        {
            manualAimAction.performed -= OnManualAimPerformed;
            manualAimAction.canceled -= OnManualAimCanceled;
            manualAimAction.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.performed -= OnPointerPositionPerformed;
            pointerPositionAction.Disable();
        }

        if (activeSkillAction != null)
        {
            activeSkillAction.performed -= OnActiveSkillPerformed;
            activeSkillAction.canceled -= OnActiveSkillCanceled;
            activeSkillAction.Disable();
        }

        Move = Vector2.zero;
        IsManualAimHeld = false;
    }

    /// <summary>문자열 경로로 모든 필수 액션을 찾아 캐시합니다.</summary>
    /// <returns>필수 액션을 모두 찾았으면 true입니다.</returns>
    private bool TryResolveActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("PlayerInputReader에 InputActionAsset이 설정되지 않았습니다.", this);
            return false;
        }

        moveAction = inputActions.FindAction(moveActionPath, false);
        dashAction = inputActions.FindAction(dashActionPath, false);
        manualAimAction = inputActions.FindAction(manualAimActionPath, false);
        pointerPositionAction = inputActions.FindAction(pointerPositionActionPath, false);
        activeSkillAction = inputActions.FindAction(activeSkillActionPath, false);
        if (moveAction != null && dashAction != null && manualAimAction != null
            && pointerPositionAction != null && activeSkillAction != null)
        {
            return true;
        }

        Debug.LogError("Move, Dash, Attack, AimPosition 또는 ActiveSkill 입력 액션을 찾을 수 없습니다.", this);
        return false;
    }

    // 아래 콜백들은 Input System의 performed/canceled 신호를 게임에서 쓰기 쉬운 값과 이벤트로 변환합니다.
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        SetMove(context.ReadValue<Vector2>());
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        SetMove(Vector2.zero);
    }

    /// <summary>이동 벡터를 정규화해 대각선 입력이 더 빠르지 않게 저장합니다.</summary>
    private void SetMove(Vector2 value)
    {
        Move = Vector2.ClampMagnitude(value, 1f);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        DashPressed?.Invoke();
    }

    private void OnManualAimPerformed(InputAction.CallbackContext context)
    {
        IsManualAimHeld = true;
    }

    private void OnManualAimCanceled(InputAction.CallbackContext context)
    {
        IsManualAimHeld = false;
    }

    private void OnPointerPositionPerformed(InputAction.CallbackContext context)
    {
        PointerScreenPosition = context.ReadValue<Vector2>();
    }

    private void OnActiveSkillPerformed(InputAction.CallbackContext context)
    {
        ActiveSkillPressed?.Invoke();
    }

    private void OnActiveSkillCanceled(InputAction.CallbackContext context)
    {
        ActiveSkillReleased?.Invoke();
    }
}
