using UnityEngine;

/// <summary>화면의 마우스 좌표를 플레이어 기준 XZ 조준 방향으로 변환합니다.</summary>
[RequireComponent(typeof(PlayerInputReader))]
public sealed class PlayerAimResolver : MonoBehaviour
{
    // 화면 마우스 좌표를 읽는 입력 모듈입니다.
    private PlayerInputReader inputReader;
    // ScreenPointToRay를 만들 카메라입니다. 사라졌다면 호출 시 다시 찾습니다.
    private Camera mainCamera;

    /// <summary>입력 모듈과 현재 Main Camera를 캐시합니다.</summary>
    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        mainCamera = Camera.main;
    }

    /// <summary>
    /// 마우스 화면 좌표에서 Ray를 발사해 플레이어 기준 XZ 조준 방향을 계산합니다.
    /// 우선 지정 레이어의 지형을 사용하고, 맞지 않으면 플레이어 높이의 가상 평면을 사용합니다.
    /// </summary>
    /// <returns>유효한 방향을 계산했으면 true입니다.</returns>
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
