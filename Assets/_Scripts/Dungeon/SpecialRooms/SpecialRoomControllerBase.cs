using FlatVenture.Inventory;
using UnityEngine;

// 휴식/상점/제련소 방 컨트롤러의 공통 기반입니다.
public abstract class SpecialRoomControllerBase : MonoBehaviour, IDungeonRoomController
{
    [Header("Room")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] protected PortalSpawner portalSpawner;
    [SerializeField] protected InventoryRuntimeBehaviour inventoryRuntime;

    protected DungeonRoomContext Context { get; private set; }

    // true이면 선택지 사용 후 같은 방에서 다시 상호작용할 수 없습니다.
    public virtual bool ConsumesInteraction
    {
        get { return true; }
    }

    public Transform PlayerSpawnPoint
    {
        get { return playerSpawnPoint; }
    }

    public InventoryRuntimeBehaviour InventoryRuntime
    {
        get { return inventoryRuntime; }
    }

    // SpecialRoomInteract가 F키 상호작용 가능 여부를 판단할 때 확인합니다.
    public virtual bool IsInteractionCompleted
    {
        get { return false; }
    }

    // UI가 열려 있거나 기능 선택용 인벤토리 패널을 사용 중이면 추가 F 상호작용을 막습니다.
    public virtual bool IsInteractionBusy
    {
        get { return false; }
    }

    // StageManager가 현재 노드 정보를 전달합니다.
    public virtual void InitializeRoom(DungeonRoomContext context)
    {
        Context = context;

        if (portalSpawner == null)
        {
            portalSpawner = GetComponentInChildren<PortalSpawner>(true);
        }

        if (portalSpawner != null)
        {
            // 특수 방은 선택지 결과를 포탈 생성 시점에 저장하지 않고, 다음 노드 선택 시점에 확정합니다.
            portalSpawner.SetSaveWhenPortalGenerated(false);
            portalSpawner.Initialize(context);
        }

        FindInventoryIfNeeded();
    }

    // 방 입장 시 호출됩니다.
    // 전투방은 포탈 체크포인트 복원에 매개변수를 사용하지만, 특수 방은 저장 정책상 보통 무시합니다.
    public abstract void StartRoomEvent(bool startFromPortalCheckpoint = false);

    // NPC/F 상호작용 시 호출됩니다.
    public abstract void OpenInteractionUI();

    // 프리팹에 인벤토리 참조가 직접 연결되어 있지 않은 테스트 환경을 보조합니다.
    protected void FindInventoryIfNeeded()
    {
        if (inventoryRuntime == null)
        {
            var runtimes = FindObjectsByType<InventoryRuntimeBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (runtimes.Length > 0)
            {
                inventoryRuntime = runtimes[0];
            }
        }
    }

    // 선택지 사용이 끝났을 때 포탈을 생성합니다.
    // 저장 여부는 PortalSpawner의 saveWhenPortalGenerated 정책을 따릅니다.
    protected void SpawnPortal()
    {
        if (portalSpawner != null)
        {
            portalSpawner.SpawnPortal();
        }
    }
}
