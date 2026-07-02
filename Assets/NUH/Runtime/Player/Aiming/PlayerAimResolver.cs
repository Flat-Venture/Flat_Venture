using FlatVenture.NUH.Player.Input;
using UnityEngine;

namespace FlatVenture.NUH.Player.Aiming
{
    /// <summary>화면의 마우스 좌표를 플레이어 기준 XZ 조준 방향으로 변환합니다.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerAimResolver : MonoBehaviour
    {
        private PlayerInputReader inputReader;
        private Camera mainCamera;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
            mainCamera = Camera.main;
        }

        public bool TryResolvePointerDirection(Transform origin, LayerMask surfaceMask, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (origin == null)
                return false;

            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(inputReader.PointerScreenPosition);
            Vector3 worldPoint;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                worldPoint = hit.point;
            }
            else
            {
                Plane plane = new Plane(Vector3.up, origin.position);
                if (!plane.Raycast(ray, out float distance))
                    return false;
                worldPoint = ray.GetPoint(distance);
            }

            direction = worldPoint - origin.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();
            return true;
        }
    }
}
