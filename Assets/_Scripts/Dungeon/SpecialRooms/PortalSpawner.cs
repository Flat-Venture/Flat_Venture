using UnityEngine;

// 방 클리어 이후 생성되는 포탈과 저장 체크포인트 호출을 관리합니다.
// 전투방, 특수방, 이벤트방 모두 같은 방식으로 사용할 수 있습니다.
public sealed class PortalSpawner : MonoBehaviour
{
    [Header("Portal")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private Transform portalSpawnPoint;
    [SerializeField] private bool saveWhenPortalGenerated = true;

    private DungeonRoomContext context;
    private Portal spawnedPortal;

    // 이미 포탈이 생성되어 있는지 확인할 때 사용합니다.
    public bool HasPortal
    {
        get { return spawnedPortal != null; }
    }

    // 포탈 생성 시점에 저장 체크포인트를 만들지 여부를 설정합니다.
    // 선택 결과를 다음 노드 클릭 때 확정하려는 방에서는 false로 둡니다.
    public void SetSaveWhenPortalGenerated(bool shouldSave)
    {
        saveWhenPortalGenerated = shouldSave;
    }

    // StageManager에서 전달받은 방 진행 컨텍스트를 보관합니다.
    public void Initialize(DungeonRoomContext nextContext)
    {
        context = nextContext;
    }

    // 포탈이 없으면 생성하고, 포탈 탑승 시 지도 복귀 콜백을 연결합니다.
    public bool SpawnPortal()
    {
        if (spawnedPortal != null)
        {
            return true;
        }

        if (portalPrefab == null)
        {
            Debug.LogWarning("[PortalSpawner] 포탈 프리팹이 없습니다.", this);
            return false;
        }

        var spawnTransform = portalSpawnPoint != null ? portalSpawnPoint : transform;
        var portalObject = Instantiate(portalPrefab, spawnTransform.position, spawnTransform.rotation, transform);
        spawnedPortal = portalObject.GetComponent<Portal>();

        if (spawnedPortal != null)
        {
            spawnedPortal.onPortalEntered = HandlePortalEntered;
        }

        if (saveWhenPortalGenerated && context != null)
        {
            context.NotifyPortalGenerated();
        }

        return true;
    }

    // Portal 컴포넌트의 onPortalEntered에서 호출됩니다.
    // 포탈을 탔을 때는 방이 완전히 끝난 것이므로 StageManager로 방 클리어를 알립니다.
    private void HandlePortalEntered()
    {
        if (context != null)
        {
            context.NotifyRoomCleared();
        }
    }
}
