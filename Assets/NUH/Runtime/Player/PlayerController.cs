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
            Vector3 planarDirection = new Vector3(input.x, 0f, input.y);

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
    }
}
