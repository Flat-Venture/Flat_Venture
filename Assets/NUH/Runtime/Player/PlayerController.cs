using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.State;
using UnityEngine;

namespace FlatVenture.NUH.Player
{
    /// <summary>플레이어 런타임 상태의 생성과 원본값 초기화를 담당합니다.</summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerStatsData baseStats;

        private CharacterController characterController;
        private PlayerInputReader inputReader;
        private Vector3 spawnPosition;

        public PlayerRuntimeState RuntimeState { get; private set; }

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
    }
}
