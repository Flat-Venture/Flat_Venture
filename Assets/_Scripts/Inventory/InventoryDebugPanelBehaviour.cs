using System;
using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.Reward;
using FlatVenture.SaveLoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.Inventory
{
    // Inventory test panel used by reward/shop/rest/forge prototype UI.
    public sealed class InventoryDebugPanelBehaviour : MonoBehaviour
    {
        private enum InventoryInteractionMode
        {
            Normal,
            Swap,
            Sell,
            FrameElement
        }

        [SerializeField] private InventoryRuntimeBehaviour inventoryRuntime;
        [SerializeField] private ItemAssetDatabaseSO itemAssetDatabase;
        [SerializeField] private Key toggleKey = Key.I;
        [SerializeField] private Key discardKey = Key.F;

        private InventoryInteractionMode mode = InventoryInteractionMode.Normal;
        private int selectedSlot = -1;
        private int swapFirstSlot = -1;
        private int hoveredSlot = -1;
        private bool isVisible;
        private bool allowDiscardKey = true;
        private bool showSellPriceHint;
        private bool externalPanelMode;
        private string externalCloseButtonText = "\uB2EB\uAE30";
        private Action externalCloseAction;
        private Action externalCancelAction;
        private Action<int> externalSlotClickAction;
        private Action externalSellAction;
        private bool externalSyncActiveSave = true;
        private string message;

        public ItemAssetDatabaseSO ItemAssetDatabase
        {
            get { return itemAssetDatabase; }
        }

        private void Awake()
        {
            isVisible = false;
            FindRuntimeIfNeeded();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            if (CanToggleInventoryByKey() && keyboard[toggleKey].wasPressedThisFrame)
            {
                if (isVisible)
                {
                    HidePanel();
                }
                else
                {
                    ShowPanel();
                }
            }

            if (isVisible && allowDiscardKey && keyboard[discardKey].wasPressedThisFrame)
            {
                DiscardHoveredOrSelectedItem();
            }
        }

        // I키로 일반 인벤토리를 열고 닫을 수 있는 상태인지 확인합니다.
        private bool CanToggleInventoryByKey()
        {
            if (DungeonRewardSelectionController.IsOpen || global::DungeonUiInputBlocker.IsPauseMenuOpen)
            {
                return false;
            }

            // 스왑/판매/제련소 슬롯 선택처럼 특정 기능이 인벤토리 패널을 쓰는 중에는 I키 토글을 막습니다.
            return !externalPanelMode;
        }

        private void OnGUI()
        {
            if (!isVisible || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                GUI.Box(new Rect(24, 24, 420, 90), "InventoryRuntimeBehaviour not found.");
                return;
            }

            hoveredSlot = -1;
            DrawSynergyPanel();
            DrawInventoryPanel();
            DrawTooltipPanel();
            DrawControlPanel();
        }

        private void FindRuntimeIfNeeded()
        {
            if (inventoryRuntime != null)
            {
                return;
            }

            var runtimes = FindObjectsByType<InventoryRuntimeBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (runtimes.Length > 0)
            {
                inventoryRuntime = runtimes[0];
            }
        }

        // 인벤토리 테스트 기능을 실행할 수 있는 런타임 상태인지 확인합니다.
        private bool HasRuntime()
        {
            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                message = "InventoryRuntimeBehaviour를 찾지 못했습니다.";
                return false;
            }

            return true;
        }

        public void ShowPanel()
        {
            FindRuntimeIfNeeded();
            isVisible = true;
            externalPanelMode = false;
            allowDiscardKey = true;
            showSellPriceHint = false;
            externalCloseAction = null;
            externalCancelAction = null;
            externalSlotClickAction = null;
            externalSellAction = null;
            externalSyncActiveSave = true;
            externalCloseButtonText = "\uB2EB\uAE30";
            EnterMode(InventoryInteractionMode.Normal);
        }

        // 휴식 방처럼 제한된 상황에서 사용하는 스왑 전용 인벤토리 패널을 엽니다.
        public void ShowSwapPanel(int swapCount, Action onClose = null, bool syncActiveSave = true, Action onCancel = null)
        {
            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                return;
            }

            isVisible = true;
            externalPanelMode = true;
            allowDiscardKey = false;
            showSellPriceHint = false;
            externalCloseButtonText = "\uC2A4\uC651 \uC644\uB8CC";
            externalCloseAction = onClose;
            externalCancelAction = onCancel;
            externalSlotClickAction = null;
            externalSellAction = null;
            externalSyncActiveSave = syncActiveSave;
            global::DungeonUiInputBlocker.SetBlocked(this, true);
            EnterSwapMode(swapCount);
        }

        public void ShowShopSellPanel(Action onReturnToShop = null, Action onAfterSell = null, bool syncActiveSave = true)
        {
            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                return;
            }

            isVisible = true;
            externalPanelMode = true;
            allowDiscardKey = false;
            showSellPriceHint = true;
            externalCloseButtonText = "\uC0C1\uC810\uC73C\uB85C \uB3CC\uC544\uAC00\uAE30";
            externalCloseAction = onReturnToShop;
            externalCancelAction = null;
            externalSlotClickAction = null;
            externalSellAction = onAfterSell;
            externalSyncActiveSave = syncActiveSave;
            global::DungeonUiInputBlocker.SetBlocked(this, true);
            EnterMode(InventoryInteractionMode.Sell);
        }

        public void ShowExternalPreviewPanel(string closeButtonText, Action onClose = null)
        {
            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                return;
            }

            isVisible = true;
            externalPanelMode = true;
            allowDiscardKey = false;
            showSellPriceHint = false;
            externalCloseButtonText = string.IsNullOrEmpty(closeButtonText) ? "\uB2EB\uAE30" : closeButtonText;
            externalCloseAction = onClose;
            externalCancelAction = null;
            externalSlotClickAction = null;
            externalSellAction = null;
            externalSyncActiveSave = true;
            global::DungeonUiInputBlocker.SetBlocked(this, true);
            EnterMode(InventoryInteractionMode.Normal);
        }

        public void ShowExternalSlotActionPanel(string helpMessage, string closeButtonText, Action<int> onSlotClicked, Action onClose)
        {
            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                return;
            }

            isVisible = true;
            externalPanelMode = true;
            allowDiscardKey = false;
            showSellPriceHint = false;
            externalCloseButtonText = string.IsNullOrEmpty(closeButtonText) ? "\uB2EB\uAE30" : closeButtonText;
            externalCloseAction = onClose;
            externalCancelAction = null;
            externalSlotClickAction = onSlotClicked;
            externalSellAction = null;
            externalSyncActiveSave = true;
            selectedSlot = -1;
            swapFirstSlot = -1;
            mode = InventoryInteractionMode.Normal;
            message = helpMessage;
            global::DungeonUiInputBlocker.SetBlocked(this, true);
        }

        public void HidePanel()
        {
            isVisible = false;
            externalPanelMode = false;
            allowDiscardKey = true;
            showSellPriceHint = false;
            externalCloseAction = null;
            externalCancelAction = null;
            externalSlotClickAction = null;
            externalSellAction = null;
            externalSyncActiveSave = true;
            externalCloseButtonText = "\uB2EB\uAE30";
            global::DungeonUiInputBlocker.SetBlocked(this, false);
            EnterMode(InventoryInteractionMode.Normal);
        }

        // I키로 연 일반 인벤토리만 닫습니다. 스왑/강화 같은 외부 기능 패널은 닫지 않습니다.
        public bool HideNormalPanelIfVisible()
        {
            if (!isVisible || externalPanelMode)
            {
                return false;
            }

            HidePanel();
            return true;
        }

        private void DrawSynergyPanel()
        {
            var rect = new Rect(24, 160, 250, 420);
            GUI.Box(rect, "\uD65C\uC131\uD654\uB41C \uC2DC\uB108\uC9C0");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 32, rect.width - 28, rect.height - 46));
            var synergy = inventoryRuntime.SynergyResult;

            GUILayout.Label("\uBE59\uACE0 \uC18D\uC131 \uC2DC\uB108\uC9C0");
            if (synergy == null || synergy.completedLineCountsByElement.Count == 0)
            {
                GUILayout.Label("- \uC644\uC131\uB41C \uC904 \uC5C6\uC74C");
            }
            else
            {
                foreach (var pair in synergy.completedLineCountsByElement)
                {
                    DrawColorLabel(pair.Key, inventoryRuntime.GetElementDisplayName(pair.Key) + " x" + pair.Value + "\uC904");
                }
            }

            GUILayout.Space(12);
            GUILayout.Label("\uAC1C\uC218 \uC18D\uC131 \uC2DC\uB108\uC9C0");
            if (synergy == null || synergy.countSynergyLevelsByElement.Count == 0)
            {
                GUILayout.Label("- \uD65C\uC131\uD654 \uC5C6\uC74C");
            }
            else
            {
                foreach (var pair in synergy.countSynergyLevelsByElement)
                {
                    int points = 0;
                    synergy.elementPointCounts.TryGetValue(pair.Key, out points);
                    DrawColorLabel(pair.Key, inventoryRuntime.GetElementDisplayName(pair.Key) + " Lv." + pair.Value + " (" + points + "pt)");
                }
            }

            GUILayout.Space(12);
            GUILayout.Label("\uC18D\uC131 \uC0C9\uC0C1");
            DrawElementLegend();
            GUILayout.EndArea();
        }

        private void DrawElementLegend()
        {
            var elements = inventoryRuntime.SortedElementIds;
            int shown = 0;
            for (int i = 0; i < elements.Count && shown < 9; i++)
            {
                DrawColorLabel(elements[i], inventoryRuntime.GetElementDisplayName(elements[i]));
                shown++;
            }
        }

        private void DrawColorLabel(string elementId, string label)
        {
            GUILayout.BeginHorizontal();
            var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            DrawFilledRect(colorRect, GetElementColor(elementId));
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }

        private void DrawInventoryPanel()
        {
            var rect = new Rect(Screen.width * 0.5f - 205, 115, 410, 440);
            GUI.Box(rect, "\uC778\uBCA4\uD1A0\uB9AC");

            const float cellSize = 66f;
            const float gap = 8f;
            float startX = rect.x + 26f;
            float startY = rect.y + 54f;
            var grid = inventoryRuntime.Grid;

            for (int i = 0; i < grid.Capacity; i++)
            {
                var slot = grid.GetSlot(i);
                var cellRect = new Rect(
                    startX + slot.position.x * (cellSize + gap),
                    startY + slot.position.y * (cellSize + gap),
                    cellSize,
                    cellSize);

                DrawSlot(cellRect, slot);
            }
        }

        private void DrawSlot(Rect rect, InventorySlot slot)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                hoveredSlot = slot.position.index;
            }

            DrawFilledRect(rect, slot.isSealed ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.16f, 0.18f, 0.26f, 1f));

            if (!string.IsNullOrEmpty(slot.frameElementId))
            {
                DrawBorder(rect, GetElementColor(slot.frameElementId), 4f);
            }
            else
            {
                DrawBorder(rect, new Color(0.45f, 0.48f, 0.56f, 1f), 2f);
            }

            if (slot.HasItem)
            {
                var icon = ItemIconResolver.ResolveIcon(slot.item.itemId, itemAssetDatabase);
                DrawSprite(new Rect(rect.x + 9, rect.y + 11, rect.width - 18, rect.height - 20), icon);
                DrawItemElementStrip(rect, slot.item);
            }

            if (!string.IsNullOrEmpty(slot.frameElementId))
            {
                GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 16), inventoryRuntime.GetElementDisplayName(slot.frameElementId));
            }

            if (slot.upgradeLevel > 0)
            {
                GUI.Label(new Rect(rect.x + rect.width - 28, rect.y + 2, 28, 18), "+" + slot.upgradeLevel);
            }

            if (slot.isSealed)
            {
                GUI.Label(new Rect(rect.x + 10, rect.y + 23, rect.width - 20, 22), "\uBD09\uC778");
            }

            if (selectedSlot == slot.position.index)
            {
                DrawBorder(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), Color.white, 3f);
            }

            if (swapFirstSlot == slot.position.index)
            {
                DrawBorder(new Rect(rect.x - 6, rect.y - 6, rect.width + 12, rect.height + 12), Color.cyan, 3f);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                HandleSlotClicked(slot.position.index);
            }
        }

        private void DrawItemElementStrip(Rect rect, InventoryItem item)
        {
            string elementId;
            if (item == null || !item.TryGetPrimaryElementId(out elementId))
            {
                return;
            }

            DrawFilledRect(new Rect(rect.x + 5, rect.yMax - 8, rect.width - 10, 4), GetElementColor(elementId));
        }

        private void DrawTooltipPanel()
        {
            var rect = new Rect(Screen.width - 345, 160, 310, 300);
            GUI.Box(rect, "\uC544\uC774\uD15C \uC815\uBCF4");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 32, rect.width - 28, rect.height - 46));
            GUILayout.Label("\uACE8\uB4DC: " + inventoryRuntime.Wallet.Gold);
            GUILayout.Label("\uBAA8\uB4DC: " + GetModeDisplayName());
            GUILayout.Label(BuildDungeonFloorText());
            GUILayout.Space(8);

            var slot = inventoryRuntime.Grid.GetSlot(hoveredSlot >= 0 ? hoveredSlot : selectedSlot);
            if (slot == null || !slot.HasItem)
            {
                GUILayout.Label("\uC544\uC774\uD15C\uC5D0 \uB9C8\uC6B0\uC2A4\uB97C \uC62C\uB824\uC8FC\uC138\uC694.");
            }
            else
            {
                GUILayout.Label(slot.item.displayName);
                GUILayout.Label("#" + slot.item.itemId);
                GUILayout.Label("\uB4F1\uAE09: " + slot.item.rarityId);
                GUILayout.Label("\uC18D\uC131: " + BuildElementText(slot.item));
                int sellPrice = inventoryRuntime.PriceService != null
                    ? inventoryRuntime.PriceService.CalculateShopSellPrice(slot.item)
                    : 0;
                GUILayout.Label("\uD310\uB9E4\uAC00: " + sellPrice + " \uACE8\uB4DC");
                GUILayout.Space(12);

                if (allowDiscardKey)
                {
                    GUILayout.Label("F: \uBC84\uB9AC\uAE30");
                }
                else if (showSellPriceHint)
                {
                    GUILayout.Label("\uD074\uB9AD \uC2DC \uD310\uB9E4: " + sellPrice + " \uACE8\uB4DC");
                }
                else
                {
                    GUILayout.Label("\uD604\uC7AC \uBAA8\uB4DC\uC5D0\uC11C\uB294 F \uBC84\uB9AC\uAE30\uB97C \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
                }
            }

            GUILayout.EndArea();
        }

        private void DrawControlPanel()
        {
            var rect = new Rect(Screen.width * 0.5f - 250, 570, 500, externalPanelMode ? 110 : 150);
            GUI.Box(rect, externalPanelMode ? "\uC778\uBCA4\uD1A0\uB9AC \uBAA8\uB4DC" : "\uD14C\uC2A4\uD2B8 \uBAA8\uB4DC");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 30, rect.width - 28, rect.height - 38));
            if (!externalPanelMode)
            {
                DrawDebugButtons();
            }
            else
            {
                GUILayout.Label(BuildModeHelpText());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(externalCloseButtonText, GUILayout.Width(180f), GUILayout.Height(30f)))
                {
                    if (externalCloseAction != null)
                    {
                        externalCloseAction.Invoke();
                    }
                    else
                    {
                        HidePanel();
                    }
                }

                if (mode == InventoryInteractionMode.Swap && externalCancelAction != null)
                {
                    if (GUILayout.Button("\uC2A4\uC651 \uCDE8\uC18C", GUILayout.Width(180f), GUILayout.Height(30f)))
                    {
                        externalCancelAction.Invoke();
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(message ?? string.Empty);
            GUILayout.EndArea();
        }

        private void DrawDebugButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("\uC77C\uBC18")) EnterMode(InventoryInteractionMode.Normal);
            if (GUILayout.Button("\uC2A4\uC651 3\uD68C")) EnterSwapMode(3);
            if (GUILayout.Button("\uC2A4\uC651 5\uD68C")) EnterSwapMode(5);
            if (GUILayout.Button("\uD310\uB9E4")) EnterMode(InventoryInteractionMode.Sell);
            if (GUILayout.Button("\uD504\uB808\uC784")) EnterMode(InventoryInteractionMode.FrameElement);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("\uB79C\uB364 \uD68D\uB4DD"))
            {
                GrantRandomItem();
            }

            if (GUILayout.Button("\uC2AC\uB86F \uAC15\uD654"))
            {
                UpgradeSelectedSlot();
            }

            if (GUILayout.Button("\uC804\uCCB4 \uBE44\uC6B0\uAE30"))
            {
                ClearInventoryForDebug();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(BuildModeHelpText());
        }

        private void EnterMode(InventoryInteractionMode nextMode)
        {
            mode = nextMode;
            swapFirstSlot = -1;
            selectedSlot = -1;

            if (inventoryRuntime != null && inventoryRuntime.Grid != null && nextMode != InventoryInteractionMode.Swap)
            {
                inventoryRuntime.Grid.ClearTemporarySwapCount();
            }

            message = GetModeDisplayName() + " \uBAA8\uB4DC";
        }

        private void EnterSwapMode(int swapCount)
        {
            mode = InventoryInteractionMode.Swap;
            selectedSlot = -1;
            swapFirstSlot = -1;
            inventoryRuntime.Grid.SetTemporarySwapCount(swapCount);
            message = "\uC2A4\uC651 \uBAA8\uB4DC \uC2DC\uC791: " + swapCount + "\uD68C";
        }

        private void HandleSlotClicked(int index)
        {
            selectedSlot = index;

            if (externalSlotClickAction != null)
            {
                externalSlotClickAction.Invoke(index);
                return;
            }

            if (mode == InventoryInteractionMode.Sell)
            {
                SellSlot(index);
                return;
            }

            if (mode == InventoryInteractionMode.FrameElement)
            {
                CycleFrameElement(index);
                return;
            }

            if (mode == InventoryInteractionMode.Swap)
            {
                HandleSwapSlotClicked(index);
            }
        }

        private void HandleSwapSlotClicked(int index)
        {
            if (inventoryRuntime.Grid.TemporarySwapCount <= 0)
            {
                message = "\uD604\uC7AC \uC2A4\uC651 \uAC00\uB2A5\uD55C \uC0C1\uD669\uC774 \uC544\uB2D9\uB2C8\uB2E4.";
                return;
            }

            if (swapFirstSlot < 0)
            {
                swapFirstSlot = index;
                message = "\uC2A4\uC651\uD560 \uB450 \uBC88\uC9F8 \uCE78\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
                return;
            }

            if (swapFirstSlot == index)
            {
                swapFirstSlot = -1;
                message = "\uC2A4\uC651 \uCCAB \uBC88\uC9F8 \uC120\uD0DD\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
                return;
            }

            string swapMessage;
            if (inventoryRuntime.Grid.TrySwapItems(swapFirstSlot, index, out swapMessage))
            {
                inventoryRuntime.RecalculateSynergy();
                if (externalSyncActiveSave)
                {
                    inventoryRuntime.SyncActiveSaveData();
                }
            }

            swapFirstSlot = -1;
            message = swapMessage;

            if (inventoryRuntime.Grid.TemporarySwapCount <= 0)
            {
                mode = InventoryInteractionMode.Normal;
                message += " / \uC2A4\uC651 \uBAA8\uB4DC \uC885\uB8CC";
            }
        }

        private void DiscardHoveredOrSelectedItem()
        {
            int targetSlot = hoveredSlot >= 0 ? hoveredSlot : selectedSlot;
            if (!HasRuntime())
            {
                return;
            }

            InventoryItem removedItem;
            if (!inventoryRuntime.Grid.TryRemoveItem(targetSlot, out removedItem))
            {
                message = "\uBC84\uB9B4 \uC544\uC774\uD15C\uC5D0 \uB9C8\uC6B0\uC2A4\uB97C \uC62C\uB824\uC8FC\uC138\uC694.";
                return;
            }

            if (selectedSlot == targetSlot) selectedSlot = -1;
            if (swapFirstSlot == targetSlot) swapFirstSlot = -1;

            message = removedItem.displayName + " \uBC84\uB9BC";
            inventoryRuntime.RecalculateSynergy();
            inventoryRuntime.SyncActiveSaveData();
        }

        // 랜덤 아이템 1개를 지급합니다.
        private void GrantRandomItem()
        {
            if (!HasRuntime())
            {
                return;
            }

            var items = inventoryRuntime.SortedItems;
            if (items.Count == 0)
            {
                inventoryRuntime.RefreshCatalog();
                items = inventoryRuntime.SortedItems;
            }

            if (items.Count == 0)
            {
                message = "\uC9C0\uAE09\uD560 \uC544\uC774\uD15C \uB370\uC774\uD130\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
                return;
            }

            var record = items[UnityEngine.Random.Range(0, items.Count)];
            InventoryPosition position;
            inventoryRuntime.TryGrantItem(record, out position, out message);
        }

        // 지정 슬롯의 아이템을 판매하고 필요하면 외부 판매 완료 콜백을 실행합니다.
        private void SellSlot(int index)
        {
            if (!HasRuntime())
            {
                return;
            }

            int earnedGold;
            if (!inventoryRuntime.TrySellItemAt(index, externalSyncActiveSave, out earnedGold, out message))
            {
                return;
            }

            if (externalSellAction != null)
            {
                externalSellAction.Invoke();
            }
        }

        // 지정 슬롯의 프레임 속성을 다음 속성으로 순환시킵니다.
        private void CycleFrameElement(int index)
        {
            if (!HasRuntime())
            {
                return;
            }

            var slot = inventoryRuntime.Grid.GetSlot(index);
            var elements = inventoryRuntime.SortedElementIds;
            if (slot == null)
            {
                message = "\uD504\uB808\uC784 \uC18D\uC131\uC744 \uBC14\uAFC0 \uC2AC\uB86F\uC744 \uC120\uD0DD\uD574\uC8FC\uC138\uC694.";
                return;
            }

            if (elements.Count == 0)
            {
                message = "\uC0AC\uC6A9\uD560 \uC18D\uC131 \uC815\uC758\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
                return;
            }

            int currentIndex = IndexOf(elements, slot.frameElementId);
            int nextIndex = currentIndex + 1;
            if (nextIndex >= elements.Count)
            {
                slot.SetFrameElement(null);
                message = "\uD504\uB808\uC784 \uC18D\uC131 \uC81C\uAC70";
            }
            else
            {
                slot.SetFrameElement(elements[nextIndex]);
                message = "\uD504\uB808\uC784 \uC18D\uC131: " + inventoryRuntime.GetElementDisplayName(slot.frameElementId);
            }

            inventoryRuntime.RecalculateSynergy();
            inventoryRuntime.SyncActiveSaveData();
        }

        // 선택 슬롯에 테스트용 강화 수치를 적용합니다.
        private void UpgradeSelectedSlot()
        {
            if (!HasRuntime())
            {
                return;
            }

            int upgradeAmount;
            inventoryRuntime.TryAddRolledSlotUpgrade(selectedSlot, out upgradeAmount, out message);
        }

        // 테스트용으로 인벤토리와 골드를 초기화합니다.
        private void ClearInventoryForDebug()
        {
            if (!HasRuntime())
            {
                return;
            }

            inventoryRuntime.ClearInventoryForTest();
            ResetSelection();
            message = "\uC778\uBCA4\uD1A0\uB9AC\uC640 \uACE8\uB4DC\uB97C \uCD08\uAE30\uD654\uD588\uC2B5\uB2C8\uB2E4.";
        }

        private void ResetSelection()
        {
            selectedSlot = -1;
            swapFirstSlot = -1;
        }

        private static string BuildDungeonFloorText()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null || SaveGameSession.CurrentSaveData.dungeon == null)
            {
                return "\uB358\uC804 \uCE35: -";
            }

            return "\uB358\uC804 \uCE35: " + SaveGameSession.CurrentSaveData.dungeon.currentFloor;
        }

        private string GetModeDisplayName()
        {
            switch (mode)
            {
                case InventoryInteractionMode.Swap:
                    return "\uC2A4\uC651";
                case InventoryInteractionMode.Sell:
                    return "\uD310\uB9E4";
                case InventoryInteractionMode.FrameElement:
                    return "\uD504\uB808\uC784";
                default:
                    return "\uC77C\uBC18";
            }
        }

        private string BuildModeHelpText()
        {
            if (mode == InventoryInteractionMode.Swap)
            {
                return "\uC2A4\uC651: \uCCAB \uCE78 \uC120\uD0DD \uD6C4 \uB450 \uBC88\uC9F8 \uCE78 \uC120\uD0DD / \uB0A8\uC740 \uD69F\uC218 " + inventoryRuntime.Grid.TemporarySwapCount;
            }

            if (mode == InventoryInteractionMode.Sell)
            {
                return "\uD310\uB9E4: \uC544\uC774\uD15C \uCE78 \uD074\uB9AD \uC2DC \uC989\uC2DC \uD310\uB9E4";
            }

            if (mode == InventoryInteractionMode.FrameElement)
            {
                return "\uD504\uB808\uC784: \uC2AC\uB86F \uD074\uB9AD \uC2DC \uD504\uB808\uC784 \uC18D\uC131 \uBCC0\uACBD";
            }

            return allowDiscardKey ? "\uC77C\uBC18: \uC2AC\uB86F \uC120\uD0DD, F\uB85C \uC544\uC774\uD15C \uBC84\uB9AC\uAE30" : "\uC77C\uBC18: \uC2AC\uB86F \uC120\uD0DD";
        }

        private string BuildElementText(InventoryItem item)
        {
            if (item == null || item.elements.Count == 0)
            {
                return "-";
            }

            var parts = new List<string>();
            for (int i = 0; i < item.elements.Count; i++)
            {
                var element = item.elements[i];
                if (!inventoryRuntime.IsElementVisibleInInventory(element.elementId))
                {
                    continue;
                }

                parts.Add(inventoryRuntime.GetElementDisplayName(element.elementId) + " +" + element.elementValue);
            }

            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "-";
        }

        // 리스트에서 문자열의 위치를 찾습니다.
        private static int IndexOf(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Color GetElementColor(string elementId)
        {
            switch (elementId)
            {
                case "fire": return new Color(0.95f, 0.22f, 0.16f, 1f);
                case "water": return new Color(0.20f, 0.62f, 0.95f, 1f);
                case "grass": return new Color(0.25f, 0.78f, 0.30f, 1f);
                case "electric": return new Color(1f, 0.88f, 0.18f, 1f);
                case "ice": return new Color(0.55f, 0.9f, 1f, 1f);
                case "earth": return new Color(0.62f, 0.44f, 0.24f, 1f);
                case "wind": return new Color(0.52f, 0.95f, 0.75f, 1f);
                case "light": return new Color(1f, 0.95f, 0.62f, 1f);
                case "dark": return new Color(0.45f, 0.34f, 0.88f, 1f);
                case "poison": return new Color(0.55f, 0.25f, 0.85f, 1f);
                default: return new Color(0.78f, 0.82f, 0.9f, 1f);
            }
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);

            GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
        }
    }
}
