using FlatVenture.Inventory;
using FlatVenture.SaveLoad;
using UnityEngine;

// 몬스터가 등장하지 않고 휴식/스왑 선택지를 제공하는 휴식 방 컨트롤러입니다.
public class RestRoomController : SpecialRoomControllerBase
{
    private const int RestSwapCount = 3;

    private bool isOpen;
    private bool isChoiceCompleted;
    private string message;
    private InventorySaveData swapStartSnapshot;

    public bool IsOpen
    {
        get { return isOpen; }
    }

    public string Message
    {
        get { return message; }
    }

    public int SwapCount
    {
        get { return RestSwapCount; }
    }

    public override bool IsInteractionCompleted
    {
        get { return isChoiceCompleted; }
    }

    // StageManager가 방 입장 시 호출합니다.
    // 특수 방은 포탈 생성 시 저장하지 않으므로 startFromPortalCheckpoint 값은 사용하지 않습니다.
    public override void StartRoomEvent(bool startFromPortalCheckpoint = false)
    {
        Debug.Log("<color=green>[Rest Room]</color> 휴식 방에 입장했습니다.");
        isOpen = false;
        isChoiceCompleted = false;
        SpawnPortal();
    }

    // SpecialRoomInteract 또는 기존 CampfireInteract UnityEvent에서 호출합니다.
    public override void OpenInteractionUI()
    {
        OpenRestUI();
    }

    // 휴식 선택지 UI를 엽니다.
    public void OpenRestUI()
    {
        if (isChoiceCompleted)
        {
            message = "\uC774\uBBF8 \uC120\uD0DD\uC774 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
            return;
        }

        FindInventoryIfNeeded();
        isOpen = true;
        DungeonUiInputBlocker.SetBlocked(this, true);
        message = string.Empty;
    }

    // HP 회복 선택입니다. 실제 회복 적용은 플레이어 시스템 연결 후 추가합니다.
    public void SelectRest()
    {
        if (isChoiceCompleted)
        {
            return;
        }

        // TODO: 플레이어 HP 회복 적용

        message = "\uD734\uC2DD\uC744 \uC120\uD0DD\uD588\uC2B5\uB2C8\uB2E4. HP \uD68C\uBCF5\uC740 \uD50C\uB808\uC774\uC5B4 \uC2DC\uC2A4\uD15C \uC5F0\uACB0 \uD6C4 \uC801\uC6A9\uD569\uB2C8\uB2E4.";
        CompleteChoice();
    }

    // 아이템 위치 스왑 모드를 엽니다. 스왑 완료 버튼을 누른 뒤 포탈이 생성됩니다.
    // 특수 방 정책상 스왑 결과는 즉시 저장하지 않고 다음 노드 선택 시점에 저장합니다.
    public void SelectSwap()
    {
        if (isChoiceCompleted)
        {
            return;
        }

        FindInventoryIfNeeded();
        if (inventoryRuntime == null || inventoryRuntime.Grid == null)
        {
            message = "InventoryRuntimeBehaviour\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.";
            return;
        }

        inventoryRuntime.Grid.SetTemporarySwapCount(RestSwapCount);
        swapStartSnapshot = inventoryRuntime.CaptureSnapshot();
        isOpen = false;
        ShowInventorySwapPanel();
        message = "\uC544\uC774\uD15C \uC704\uCE58 \uC2A4\uC651 " + RestSwapCount + "\uD68C\uAC00 \uD65C\uC131\uD654\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
    }

    // 선택지를 완료합니다.
    // 휴식 방 포탈은 입장 시 이미 열려 있고, 결과 확정 저장은 다음 노드 선택 시점입니다.
    private void CompleteChoice()
    {
        isChoiceCompleted = true;
        isOpen = false;
        DungeonUiInputBlocker.SetBlocked(this, false);
    }

    // 휴식 UI를 닫습니다. 닫기는 선택 완료가 아니므로 포탈을 생성하지 않습니다.
    public void CloseUI()
    {
        isOpen = false;
        DungeonUiInputBlocker.SetBlocked(this, false);
    }

    // 인벤토리 스왑 패널을 엽니다.
    private void ShowInventorySwapPanel()
    {
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            // syncActiveSave=false: 스왑 결과는 런타임에만 반영하고 즉시 저장하지 않습니다.
            panel.ShowSwapPanel(RestSwapCount, CompleteSwapChoice, false, CancelSwapChoice);
        }
    }

    // 스왑 패널에서 완료 버튼을 눌렀을 때 호출됩니다.
    private void CompleteSwapChoice()
    {
        swapStartSnapshot = null;
        HideInventoryPanel();
        CompleteChoice();
    }

    // 스왑 시작 시점의 인벤토리로 되돌리고 휴식 선택지 UI로 복귀합니다.
    private void CancelSwapChoice()
    {
        if (inventoryRuntime != null && swapStartSnapshot != null)
        {
            inventoryRuntime.RestoreSnapshot(swapStartSnapshot);
            inventoryRuntime.Grid.ClearTemporarySwapCount();
        }

        swapStartSnapshot = null;
        HideInventoryPanel();
        isOpen = true;
        message = "스왑을 취소했습니다. 다시 선택할 수 있습니다.";
    }

    // 열려 있는 임시 인벤토리 패널을 닫습니다.
    private void HideInventoryPanel()
    {
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.HidePanel();
        }
    }
}
