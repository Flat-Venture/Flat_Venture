using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player.Movement
{
    /// <summary>카메라 기준 이동, 대시와 대시 충전만 담당합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerLocomotionController : MonoBehaviour
    {
        [Tooltip("비어 있으면 Main Camera를 이동 기준으로 사용합니다.")]
        [SerializeField] private Transform movementReference;

        private PlayerController player;
        private PlayerInputReader inputReader;
        private CharacterController characterController;
        private Vector3 dashDirection;
        private float verticalVelocity;
        private float dashTimeRemaining;
        private float dashRechargeRemaining;
        private int dashCharges;

        private PlayerRuntimeState State { get { return player.RuntimeState; } }

        public bool IsDashing { get { return dashTimeRemaining > 0f; } }
        public int DashCharges { get { return dashCharges; } }
        public int MaxDashCharges { get { return State?.MaxDashCharges ?? 0; } }
        public float DashRechargeRemaining { get { return dashRechargeRemaining; } }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
            characterController = GetComponent<CharacterController>();

            if (movementReference == null && Camera.main != null)
                movementReference = Camera.main.transform;
        }

        private void Start()
        {
            State.ResetCompleted += ResetLocomotion;
            ResetLocomotion();
        }

        private void OnEnable()
        {
            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();
            inputReader.DashPressed += TryStartDash;
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.DashPressed -= TryStartDash;
            State?.SetInvincibility(InvincibilityReason.Dash, false);
        }

        private void OnDestroy()
        {
            if (State != null)
                State.ResetCompleted -= ResetLocomotion;
        }

        private void Update()
        {
            if (State == null || State.IsDead)
                return;

            UpdateDashRecharge();
            if (IsDashing)
                MoveDash();
            else
                MoveNormally();
        }

        public void SetDebugMaxDashCharges(int value)
        {
            State.SetMaxDashCharges(value);
            dashCharges = State.MaxDashCharges;
            dashRechargeRemaining = 0f;
        }

        private void ResetLocomotion()
        {
            dashTimeRemaining = 0f;
            dashRechargeRemaining = 0f;
            verticalVelocity = 0f;
            dashCharges = State.MaxDashCharges;
            State.SetInvincibility(InvincibilityReason.Dash, false);
        }

        private void MoveNormally()
        {
            Vector3 planarDirection = GetPlanarMoveDirection(inputReader.Move);
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = planarDirection * State.MoveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void TryStartDash()
        {
            if (State == null || State.IsDead || IsDashing || dashCharges <= 0)
                return;

            Vector3 requestedDirection = GetPlanarMoveDirection(inputReader.Move);
            if (requestedDirection.sqrMagnitude <= 0.0001f)
                return;

            dashDirection = requestedDirection.normalized;
            dashTimeRemaining = State.DashDuration;
            dashCharges--;
            State.SetInvincibility(InvincibilityReason.Dash, true);

            if (dashCharges == State.MaxDashCharges - 1)
                dashRechargeRemaining = State.DashRechargeCooldown;
        }

        private void MoveDash()
        {
            float dashSpeed = State.DashDistance / State.DashDuration;
            characterController.Move(dashDirection * (dashSpeed * Time.deltaTime));
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Time.deltaTime);
            if (dashTimeRemaining <= 0f)
                State.SetInvincibility(InvincibilityReason.Dash, false);
        }

        private void UpdateDashRecharge()
        {
            if (dashCharges >= State.MaxDashCharges)
            {
                dashRechargeRemaining = 0f;
                return;
            }

            dashRechargeRemaining -= Time.deltaTime;
            if (dashRechargeRemaining > 0f)
                return;

            dashCharges++;
            dashRechargeRemaining = dashCharges < State.MaxDashCharges ? State.DashRechargeCooldown : 0f;
        }

        private Vector3 GetPlanarMoveDirection(Vector2 input)
        {
            if (movementReference == null)
                return new Vector3(input.x, 0f, input.y);

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
