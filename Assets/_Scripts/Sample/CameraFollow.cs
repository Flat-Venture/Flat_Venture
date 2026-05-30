using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3f, -10f);
    public float maxDistance = 10f;
    public float minDistance = 0.5f;

    private LayerMask raycastMask;

    private void Start()
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            Debug.LogError("CameraFollow: 'Player' 레이어를 찾을 수 없습니다.");
            return;
        }

        raycastMask = ~(1 << playerLayer);
        maxDistance = Mathf.Abs(offset.z);
    }

    private void LateUpdate()
    {
        if (Player.Instance == null) return;

        Transform target = Player.Instance.transform;

        Vector3 desired = target.position + target.rotation * offset;

        Vector3 rayStart = target.position;
        Vector3 rayDir = (desired - rayStart).normalized;

        if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, maxDistance, raycastMask))
        {
            float safeDistance = Mathf.Max(minDistance, hit.distance - 0.1f);
            transform.position = rayStart + rayDir * safeDistance;
        }
        else
        {
            transform.position = desired;
        }

        transform.LookAt(target.position);
    }
}
