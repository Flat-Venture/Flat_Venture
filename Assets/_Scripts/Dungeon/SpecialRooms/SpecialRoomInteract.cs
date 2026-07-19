using FlatVenture.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

// 특수 방 NPC/오브젝트에 붙여 F키 상호작용을 처리합니다.
public sealed class SpecialRoomInteract : MonoBehaviour
{
    [SerializeField] private SpecialRoomControllerBase roomController;

    private bool isPlayerInRange;

    // 같은 프리팹 안의 특수 방 컨트롤러 참조를 자동으로 찾습니다.
    private void Awake()
    {
        if (roomController == null)
        {
            roomController = GetComponentInParent<SpecialRoomControllerBase>();
        }
    }

    // 플레이어가 범위 안에 있고 다른 UI가 막고 있지 않을 때 F키 상호작용을 처리합니다.
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null
            && isPlayerInRange
            && !DungeonUiInputBlocker.BlocksGameplayInput
            && !IsInteractionCompleted()
            && keyboard.fKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    // F키 상호작용으로 현재 방의 임시 UI를 엽니다.
    public void Interact()
    {
        if (roomController == null)
        {
            Debug.LogWarning("[SpecialRoomInteract] 연결된 방 컨트롤러가 없습니다.", this);
            return;
        }

        HideNormalInventoryPanelIfNeeded();
        roomController.OpenInteractionUI();
    }

    // 일반 인벤토리가 열려 있으면 특수 방 선택지 UI를 열기 전에 닫습니다.
    private void HideNormalInventoryPanelIfNeeded()
    {
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.HideNormalPanelIfVisible();
        }
    }

    // 플레이어가 상호작용 범위에 들어오면 F키 입력을 받을 수 있게 표시합니다.
    private void OnTriggerEnter(Collider other)
    {
        if (IsInteractionCompleted())
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("[SpecialRoomInteract] F키로 상호작용할 수 있습니다.");
        }
    }

    // 이미 선택지를 사용한 특수 방이면 닫기 후 재상호작용을 막습니다.
    private bool IsInteractionCompleted()
    {
        return roomController != null && roomController.IsInteractionCompleted;
    }

    // 범위를 벗어나면 F키 입력 대상에서 제외합니다.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}
