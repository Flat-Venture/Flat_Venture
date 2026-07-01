using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player
{
    /// <summary>
    /// 플레이어 런타임 상태를 소유하고 CharacterController 이동과 대시를 실행합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerStatsData baseStats;
        [Tooltip("이동 방향의 기준입니다. 비어 있으면 Main Camera를 자동으로 사용합니다.")]
        [SerializeField] private Transform movementReference;

        private CharacterController characterController;
        private PlayerInputReader inputReader;
        private PlayerRuntimeState runtimeState;
        private float verticalVelocity;
        private Vector3 dashDirection;
        private float dashTimeRemaining;
        private float dashRechargeRemaining;
        private int dashCharges;

        public PlayerRuntimeState RuntimeState => runtimeState;
        public bool IsDashing => dashTimeRemaining > 0f;
        public int DashCharges => dashCharges;
        public int MaxDashCharges => baseStats != null ? baseStats.MaxDashCharges : 0;
        public float DashRechargeRemaining => dashRechargeRemaining;

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

            runtimeState = new PlayerRuntimeState();
            runtimeState.Initialize(baseStats);
            dashCharges = baseStats.MaxDashCharges;

            if (movementReference == null && Camera.main != null)
            {
                movementReference = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            inputReader = GetComponent<PlayerInputReader>();
            inputReader.DashPressed += TryStartDash;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.DashPressed -= TryStartDash;
            }

            runtimeState?.SetInvincibility(InvincibilityReason.Dash, false);
        }

        private void Update()
        {
            if (runtimeState == null || runtimeState.IsDead)
            {
                return;
            }

            UpdateDashRecharge();

            if (IsDashing)
                MoveDash();
            else
                MoveNormally();
        }

        private void MoveNormally()
        {
            Vector3 planarDirection = GetPlanarMoveDirection(inputReader.Move);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = planarDirection * runtimeState.MoveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void TryStartDash()
        {
            if (runtimeState == null || runtimeState.IsDead || IsDashing || dashCharges <= 0)
            {
                return;
            }

            Vector3 requestedDirection = GetPlanarMoveDirection(inputReader.Move);
            if (requestedDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            dashDirection = requestedDirection.normalized;
            dashTimeRemaining = baseStats.DashDuration;
            dashCharges--;
            runtimeState.SetInvincibility(InvincibilityReason.Dash, true);

            if (dashCharges == baseStats.MaxDashCharges - 1)
            {
                dashRechargeRemaining = baseStats.DashRechargeCooldown;
            }
        }

        private void MoveDash()
        {
            float dashSpeed = baseStats.DashDistance / baseStats.DashDuration;
            characterController.Move(dashDirection * (dashSpeed * Time.deltaTime));
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Time.deltaTime);

            if (dashTimeRemaining <= 0f)
            {
                runtimeState.SetInvincibility(InvincibilityReason.Dash, false);
            }
        }

        private void UpdateDashRecharge()
        {
            if (dashCharges >= baseStats.MaxDashCharges)
            {
                dashRechargeRemaining = 0f;
                return;
            }

            dashRechargeRemaining -= Time.deltaTime;
            if (dashRechargeRemaining > 0f)
            {
                return;
            }

            dashCharges++;
            dashRechargeRemaining = dashCharges < baseStats.MaxDashCharges
                ? baseStats.DashRechargeCooldown
                : 0f;
        }

        private Vector3 GetPlanarMoveDirection(Vector2 input)
        {
            if (movementReference == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = movementReference.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = movementReference.right;
            right.y = 0f;
            right.Normalize();

            return Vector3.ClampMagnitude((right * input.x) + (forward * input.y), 1f);
        }
    }
}
