using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.NUH.Player.Input
{
    /// <summary>
    /// 공유 InputActionAsset을 수정하지 않고 플레이어 입력값만 읽어 전달합니다.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string moveActionPath = "Player/Move";
        [SerializeField] private string dashActionPath = "Player/Dash";
        [SerializeField] private string manualAimActionPath = "Player/Attack";
        [SerializeField] private string pointerPositionActionPath = "Player/AimPosition";

        private InputAction moveAction;
        private InputAction dashAction;
        private InputAction manualAimAction;
        private InputAction pointerPositionAction;

        public Vector2 Move { get; private set; }
        public bool IsManualAimHeld { get; private set; }
        public Vector2 PointerScreenPosition { get; private set; }

        public event Action<Vector2> MoveChanged;
        public event Action DashPressed;

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
            moveAction.Enable();
            dashAction.Enable();
            manualAimAction.Enable();
            pointerPositionAction.Enable();
        }

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

            Move = Vector2.zero;
            IsManualAimHeld = false;
        }

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
            if (moveAction != null && dashAction != null && manualAimAction != null && pointerPositionAction != null)
            {
                return true;
            }

            Debug.LogError("Move, Dash, Attack 또는 AimPosition 입력 액션을 찾을 수 없습니다.", this);
            return false;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            SetMove(context.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            SetMove(Vector2.zero);
        }

        private void SetMove(Vector2 value)
        {
            Move = Vector2.ClampMagnitude(value, 1f);
            MoveChanged?.Invoke(Move);
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
    }
}
