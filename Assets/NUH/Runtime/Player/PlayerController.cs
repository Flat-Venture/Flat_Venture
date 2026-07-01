using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player
{
    /// <summary>
    /// 플레이어 런타임 상태를 소유하고 CharacterController 이동을 실행합니다.
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

        public PlayerRuntimeState RuntimeState => runtimeState;

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

            if (movementReference == null && Camera.main != null)
            {
                movementReference = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (runtimeState == null || runtimeState.IsDead)
            {
                return;
            }

            Move();
        }

        private void Move()
        {
            Vector2 input = inputReader.Move;
            Vector3 planarDirection = GetPlanarMoveDirection(input);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }

            Vector3 velocity = planarDirection * runtimeState.MoveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
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

            Vector3 direction = (right * input.x) + (forward * input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
