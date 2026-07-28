using System.Collections.Generic;
using FlatVenture.Inventory;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;

// 제련소 방 컨트롤러입니다. 슬롯 강화, 프레임 속성 부여, 아이템 등급업을 임시 UI로 테스트합니다.
public sealed class ForgeRoomController : SpecialRoomControllerBase
{
    public enum ForgeMode
    {
        Menu,
        SlotUpgrade,
        FrameSlot,
        FrameElement,
        ItemRarityUpgrade
    }

    private const int SlotUpgradeCost = 500;
    private const int FrameElementCost = 600;
    private const int ItemRarityUpgradeCost = 1000;

    [SerializeField] private GameDataLoaderBehaviour dataLoader;

    private ForgeMode mode = ForgeMode.Menu;
    private bool isOpen;
    private bool isChoiceCompleted;
    private int selectedSlot = -1;
    private string message;

    public int SlotUpgradeCostValue
    {
        get { return SlotUpgradeCost; }
    }

    public int FrameElementCostValue
    {
        get { return FrameElementCost; }
    }

    public int ItemRarityUpgradeCostValue
    {
        get { return ItemRarityUpgradeCost; }
    }

    public ForgeMode Mode
    {
        get { return mode; }
    }

    public bool IsOpen
    {
        get { return isOpen; }
    }

    public int SelectedSlot
    {
        get { return selectedSlot; }
    }

    public string Message
    {
        get { return message; }
    }

    public override bool IsInteractionCompleted
    {
        get { return isChoiceCompleted; }
    }

    public override bool IsInteractionBusy
    {
        get { return isOpen || mode != ForgeMode.Menu; }
    }

    // 방 입장 시 제련소 상태를 초기화하고 포탈을 바로 생성합니다.
    // 특수 방은 포탈 생성 시 저장하지 않으므로 startFromPortalCheckpoint 값은 사용하지 않습니다.
    public override void StartRoomEvent(bool startFromPortalCheckpoint = false)
    {
        Debug.Log("<color=green>[Forge Room]</color> 제련소 방에 입장했습니다.");
        ResolveReferences();
        isOpen = false;
        isChoiceCompleted = false;
        mode = ForgeMode.Menu;
        selectedSlot = -1;
        SpawnPortal();
    }

    // NPC 상호작용으로 제련소 선택지 UI를 엽니다.
    public override void OpenInteractionUI()
    {
        if (IsInteractionBusy)
        {
            return;
        }

        if (isChoiceCompleted)
        {
            message = "이미 제련소 기능을 사용했습니다.";
            return;
        }

        ResolveReferences();
        isOpen = true;
        DungeonUiInputBlocker.SetBlocked(this, true);
        mode = ForgeMode.Menu;
        selectedSlot = -1;
        message = string.Empty;
    }

    // 슬롯 강화 모드로 전환하고 인벤토리 슬롯 선택 화면을 엽니다.
    public void OpenSlotUpgradeMode()
    {
        // 슬롯 선택은 기존 인벤토리 UI를 재사용합니다.
        mode = ForgeMode.SlotUpgrade;
        selectedSlot = -1;
        message = "강화할 슬롯을 선택하세요.";
        isOpen = false;
        ShowInventorySlotActionPanel(message, SelectInventorySlot, ReturnToForgeMenu);
    }

    // 프레임 속성 부여 모드로 전환하고 대상 슬롯 선택 화면을 엽니다.
    public void OpenFrameElementMode()
    {
        // 먼저 슬롯을 고르고, 다음 단계에서 부여할 속성을 선택합니다.
        mode = ForgeMode.FrameSlot;
        selectedSlot = -1;
        message = "프레임 속성을 부여할 슬롯을 선택하세요.";
        isOpen = false;
        ShowInventorySlotActionPanel(message, SelectInventorySlot, ReturnToForgeMenu);
    }

    // 아이템 등급업 모드로 전환하고 대상 아이템 슬롯 선택 화면을 엽니다.
    public void OpenItemRarityUpgradeMode()
    {
        // 등급업 대상 아이템을 고르기 위해 인벤토리 슬롯 선택 화면으로 전환합니다.
        mode = ForgeMode.ItemRarityUpgrade;
        selectedSlot = -1;
        message = "등급업할 아이템 슬롯을 선택하세요.";
        isOpen = false;
        ShowInventorySlotActionPanel(message, SelectInventorySlot, ReturnToForgeMenu);
    }

    // 인벤토리 슬롯 선택 결과를 현재 제련소 모드에 맞게 처리합니다.
    public void SelectInventorySlot(int slotIndex)
    {
        if (mode == ForgeMode.SlotUpgrade)
        {
            // 제련소 선택 결과는 포탈 생성 시 저장하지 않으므로 syncActiveSave는 false입니다.
            int upgradeAmount;
            if (inventoryRuntime.TryPurchaseSlotUpgrade(slotIndex, SlotUpgradeCost, false, out upgradeAmount, out message))
            {
                CompleteChoice();
            }
            return;
        }

        if (mode == ForgeMode.FrameSlot)
        {
            // 슬롯만 먼저 기억해두고 인벤토리 패널을 닫은 뒤 속성 선택 UI로 돌아옵니다.
            selectedSlot = slotIndex;
            HideInventoryPanel();
            isOpen = true;
            mode = ForgeMode.FrameElement;
            message = "부여할 속성을 선택하세요.";
            return;
        }

        if (mode == ForgeMode.ItemRarityUpgrade)
        {
            TryUpgradeItemRarity(slotIndex);
        }
    }

    // 선택한 슬롯에 프레임 속성을 부여하거나 null이면 제거합니다.
    public void SelectFrameElement(string elementId)
    {
        if (mode != ForgeMode.FrameElement || selectedSlot < 0)
        {
            message = "먼저 슬롯을 선택해주세요.";
            return;
        }

        // null을 전달하면 기존 프레임 속성을 제거합니다.
        if (inventoryRuntime.TryPurchaseFrameElement(selectedSlot, elementId, FrameElementCost, false, out message))
        {
            CompleteChoice();
        }
    }

    // 선택한 슬롯의 프레임 속성을 제거합니다.
    public void ClearFrameElement()
    {
        SelectFrameElement(null);
    }

    // 선택한 아이템을 같은 속성을 가진 다음 희귀도 아이템으로 교체합니다.
    private void TryUpgradeItemRarity(int slotIndex)
    {
        var slot = inventoryRuntime.Grid.GetSlot(slotIndex);
        if (slot == null || !slot.HasItem)
        {
            message = "등급업할 아이템이 없습니다.";
            return;
        }

        ItemRecord nextItem;
        if (!TryFindRarityUpgradeItem(slot.item, out nextItem))
        {
            message = "같은 속성의 다음 등급 아이템을 찾지 못했습니다.";
            return;
        }

        // 등급업으로 교체된 아이템도 다음 노드 선택 전까지는 저장 데이터에 확정하지 않습니다.
        if (inventoryRuntime.TryReplaceItemAt(slotIndex, nextItem, ItemRarityUpgradeCost, false, out message))
        {
            CompleteChoice();
        }
    }

    // 원본 아이템의 속성과 다음 희귀도를 기준으로 등급업 후보를 찾습니다.
    private bool TryFindRarityUpgradeItem(InventoryItem sourceItem, out ItemRecord nextItem)
    {
        nextItem = null;

        if (sourceItem == null || sourceItem.elements.Count == 0 || dataLoader == null || dataLoader.Catalog == null)
        {
            return false;
        }

        string nextRarityId;
        if (!TryGetNextRarityId(sourceItem.rarityId, out nextRarityId))
        {
            return false;
        }

        // 원본 아이템이 가진 속성 중 하나라도 겹치는 다음 등급 아이템을 후보로 봅니다.
        var elementIds = new HashSet<string>();
        for (int i = 0; i < sourceItem.elements.Count; i++)
        {
            elementIds.Add(sourceItem.elements[i].elementId);
        }

        var candidates = new List<ItemRecord>();
        List<ItemRecord> source;
        if (!dataLoader.Catalog.itemsByRarity.TryGetValue(nextRarityId, out source))
        {
            return false;
        }

        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (item == null || item.definition == null)
            {
                continue;
            }

            for (int j = 0; j < item.elements.Count; j++)
            {
                if (elementIds.Contains(item.elements[j].elementId))
                {
                    candidates.Add(item);
                    break;
                }
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        // 같은 시드에서 항상 같은 결과를 얻기 위해 랜덤 추첨 전에 후보 순서를 고정합니다.
        candidates.Sort((left, right) => string.CompareOrdinal(left.definition.itemId, right.definition.itemId));
        var random = CreateForgeRandom();
        nextItem = candidates[random.Range(0, candidates.Count)];
        return true;
    }

    // 현재 희귀도보다 sortOrder가 1 높은 다음 희귀도 ID를 찾습니다.
    private bool TryGetNextRarityId(string rarityId, out string nextRarityId)
    {
        nextRarityId = null;

        RarityDefinition current;
        if (dataLoader == null || dataLoader.Catalog == null || !dataLoader.Catalog.rarities.TryGetValue(rarityId, out current))
        {
            return false;
        }

        foreach (var pair in dataLoader.Catalog.rarities)
        {
            if (pair.Value.sortOrder == current.sortOrder + 1)
            {
                nextRarityId = pair.Key;
                return true;
            }
        }

        return false;
    }

    // 제련소 랜덤 결과를 뽑을 시드 스트림을 생성합니다.
    private IRandomStream CreateForgeRandom()
    {
        // 제련소 랜덤 결과는 던전 시드와 노드 ID를 섞은 전용 스트림으로 고정합니다.
        int seed = SaveGameSession.HasActiveSave && SaveGameSession.CurrentSaveData != null && SaveGameSession.CurrentSaveData.dungeon != null
            ? SaveGameSession.CurrentSaveData.dungeon.dungeonSeed
            : SeedValue.Generate();

        int nodeId = Context != null ? Context.NodeId : 0;
        return new SeedService(seed).GetStream(SeedStreamNames.Forge + "_" + nodeId);
    }

    // 제련소 선택지를 완료 처리합니다.
    private void CompleteChoice()
    {
        // 선택지 하나를 사용하면 UI를 닫습니다. 포탈은 방 입장 시 이미 생성되어 있습니다.
        isChoiceCompleted = true;
        isOpen = false;
        DungeonUiInputBlocker.SetBlocked(this, false);
        HideInventoryPanel();
        selectedSlot = -1;
        mode = ForgeMode.Menu;
    }

    // 제련소 기능에 필요한 인벤토리와 데이터 로더 참조를 찾습니다.
    private void ResolveReferences()
    {
        FindInventoryIfNeeded();
        if (dataLoader == null)
        {
            dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
        }
    }

    // 제련소 UI와 연결된 인벤토리 패널을 닫습니다.
    public void CloseUI()
    {
        isOpen = false;
        DungeonUiInputBlocker.SetBlocked(this, false);
        HideInventoryPanel();
    }

    // 현재 선택을 취소하고 인벤토리 패널을 닫은 뒤 제련소 메뉴로 돌아옵니다.
    public void ReturnToForgeMenu()
    {
        HideInventoryPanel();
        isOpen = true;
        mode = ForgeMode.Menu;
        selectedSlot = -1;
        message = "선택을 취소했습니다.";
    }

    // 기존 인벤토리 패널을 제련소 슬롯 선택 도구로 엽니다.
    private void ShowInventorySlotActionPanel(string helpMessage, System.Action<int> onSlotClicked, System.Action onClose)
    {
        // 정식 UI 제작 전까지 기존 인벤토리 패널을 슬롯 선택 도구로 재사용합니다.
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.ShowExternalSlotActionPanel(helpMessage, "제련소로 돌아가기", onSlotClicked, onClose);
        }
    }

    // 열려 있는 인벤토리 패널을 닫습니다.
    private void HideInventoryPanel()
    {
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.HidePanel();
        }
    }
}
