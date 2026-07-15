using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.Reward;
using FlatVenture.SaveLoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.Inventory
{
    // 인벤토리 규칙을 한 씬에서 확인하기 위한 테스트 패널입니다.
    // 모드에 따라 클릭 동작을 바꿔서 스왑, 판매, 프레임 속성 부여를 빠르게 확인합니다.
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
        private string message;

        // 공유 인벤토리 런타임을 찾고 시작 시에는 패널을 숨깁니다.
        private void Awake()
        {
            isVisible = false;
            FindRuntimeIfNeeded();
        }

        // Input System으로 패널 토글과 F키 버리기를 처리합니다.
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            if (!DungeonRewardSelectionBehaviour.IsOpen && keyboard[toggleKey].wasPressedThisFrame)
            {
                isVisible = !isVisible;
            }

            if (isVisible && keyboard[discardKey].wasPressedThisFrame)
            {
                DiscardHoveredOrSelectedItem();
            }
        }

        // 임시 인벤토리 화면을 그립니다.
        private void OnGUI()
        {
            if (!isVisible || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            FindRuntimeIfNeeded();
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                GUI.Box(new Rect(24, 24, 420, 90), "InventoryRuntimeBehaviour를 찾지 못했습니다.");
                return;
            }

            hoveredSlot = -1;
            DrawSynergyPanel();
            DrawInventoryPanel();
            DrawTooltipPanel();
            DrawControlPanel();
        }

        // 씬에 있는 InventoryRuntimeBehaviour를 찾습니다.
        private void FindRuntimeIfNeeded()
        {
            if (inventoryRuntime == null)
            {
                inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            }
        }

        // 다른 UI에서 인벤토리 확인이 필요할 때 패널을 엽니다.
        public void ShowPanel()
        {
            isVisible = true;
        }

        // 다른 UI에서 인벤토리 확인을 마쳤을 때 패널을 닫습니다.
        public void HidePanel()
        {
            isVisible = false;
        }

        // 왼쪽의 활성화된 시너지 목록과 속성 색상표를 표시합니다.
        private void DrawSynergyPanel()
        {
            var rect = new Rect(24, 160, 250, 420);
            GUI.Box(rect, "활성화된 시너지");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 32, rect.width - 28, rect.height - 46));
            var synergy = inventoryRuntime.SynergyResult;

            GUILayout.Label("빙고 속성 시너지");
            if (synergy == null || synergy.completedLineCountsByElement.Count == 0)
            {
                GUILayout.Label("- 완성된 줄 없음");
            }
            else
            {
                foreach (var pair in synergy.completedLineCountsByElement)
                {
                    DrawColorLabel(pair.Key, inventoryRuntime.GetElementDisplayName(pair.Key) + " x" + pair.Value + "줄");
                }
            }

            GUILayout.Space(12);
            GUILayout.Label("개수 속성 시너지");
            if (synergy == null || synergy.countSynergyLevelsByElement.Count == 0)
            {
                GUILayout.Label("- 활성화 없음");
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
            GUILayout.Label("속성 색상");
            DrawElementLegend();
            GUILayout.EndArea();
        }

        // 속성 색상표를 표시합니다.
        private void DrawElementLegend()
        {
            var elements = inventoryRuntime.SortedElementIds;
            int shown = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                string elementId = elements[i];
                if (shown >= 9)
                {
                    break;
                }

                DrawColorLabel(elementId, inventoryRuntime.GetElementDisplayName(elementId));
                shown++;
            }
        }

        // 색상 사각형과 텍스트를 함께 표시합니다.
        private void DrawColorLabel(string elementId, string label)
        {
            GUILayout.BeginHorizontal();
            var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            DrawFilledRect(colorRect, GetElementColor(elementId));
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }

        // 가운데 5x5 인벤토리를 표시합니다.
        private void DrawInventoryPanel()
        {
            var rect = new Rect(Screen.width * 0.5f - 205, 115, 410, 440);
            GUI.Box(rect, "인벤토리");

            float cellSize = 66f;
            float gap = 8f;
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

        // 슬롯 한 칸을 그립니다.
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
                GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 16), inventoryRuntime.GetElementDisplayName(slot.frameElementId));
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

            if (slot.upgradeLevel > 0)
            {
                GUI.Label(new Rect(rect.x + rect.width - 28, rect.y + 2, 28, 18), "+" + slot.upgradeLevel);
            }

            if (slot.isSealed)
            {
                GUI.Label(new Rect(rect.x + 10, rect.y + 23, rect.width - 20, 22), "봉인");
            }

            if (IsSelected(slot.position.index))
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

        // 아이템 대표 속성을 슬롯 하단 색상 바에 표시합니다.
        private void DrawItemElementStrip(Rect rect, InventoryItem item)
        {
            string elementId;
            if (!TryGetDisplayElementId(item, out elementId))
            {
                return;
            }

            DrawFilledRect(new Rect(rect.x + 5, rect.yMax - 8, rect.width - 10, 4), GetElementColor(elementId));
        }

        // 오른쪽의 마우스 오버 아이템 상세 정보와 골드를 표시합니다.
        private void DrawTooltipPanel()
        {
            var rect = new Rect(Screen.width - 345, 160, 310, 300);
            GUI.Box(rect, "아이템 정보");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 32, rect.width - 28, rect.height - 46));
            GUILayout.Label("골드: " + inventoryRuntime.DebugGold);
            GUILayout.Label("모드: " + GetModeDisplayName());
            GUILayout.Label(BuildDungeonFloorText());
            GUILayout.Space(8);

            var slot = inventoryRuntime.Grid.GetSlot(hoveredSlot >= 0 ? hoveredSlot : selectedSlot);
            if (slot == null || !slot.HasItem)
            {
                GUILayout.Label("아이템에 마우스를 올려주세요.");
            }
            else
            {
                GUILayout.Label(slot.item.displayName);
                GUILayout.Label("#" + slot.item.itemId);
                GUILayout.Label("등급: " + slot.item.rarityId);
                GUILayout.Label("속성: " + BuildElementText(slot.item));
                GUILayout.Label("판매가: " + slot.item.sellPrice + " 골드");
                GUILayout.Space(12);
                GUILayout.Label("F: 버리기");
            }

            GUILayout.EndArea();
        }

        // 하단 조작 패널을 표시합니다.
        private void DrawControlPanel()
        {
            var rect = new Rect(Screen.width * 0.5f - 250, 570, 500, 150);
            GUI.Box(rect, "테스트 모드");

            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 30, rect.width - 28, rect.height - 38));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("일반"))
            {
                EnterMode(InventoryInteractionMode.Normal);
            }

            if (GUILayout.Button("스왑 모드 3회"))
            {
                EnterSwapMode(3);
            }

            if (GUILayout.Button("스왑 모드 5회"))
            {
                EnterSwapMode(5);
            }

            if (GUILayout.Button("판매 모드"))
            {
                EnterMode(InventoryInteractionMode.Sell);
            }

            if (GUILayout.Button("프레임 모드"))
            {
                EnterMode(InventoryInteractionMode.FrameElement);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("랜덤 획득"))
            {
                GrantRandomItem();
            }

            if (GUILayout.Button("슬롯 강화"))
            {
                UpgradeSelectedSlot();
            }

            if (GUILayout.Button("전체 비우기"))
            {
                inventoryRuntime.ClearInventoryForTest();
                ResetSelection();
                message = "인벤토리와 테스트 골드를 초기화했습니다.";
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(BuildModeHelpText());
            GUILayout.Label(message ?? string.Empty);
            GUILayout.EndArea();
        }

        // 현재 모드를 변경합니다.
        private void EnterMode(InventoryInteractionMode nextMode)
        {
            mode = nextMode;
            swapFirstSlot = -1;
            selectedSlot = -1;

            if (nextMode != InventoryInteractionMode.Swap)
            {
                inventoryRuntime.Grid.ClearTemporarySwapCount();
            }

            message = GetModeDisplayName() + " 전환";
        }

        // 특정 상황용 스왑 모드를 시작합니다.
        private void EnterSwapMode(int swapCount)
        {
            mode = InventoryInteractionMode.Swap;
            selectedSlot = -1;
            swapFirstSlot = -1;
            inventoryRuntime.Grid.SetTemporarySwapCount(swapCount);
            message = "스왑 모드 시작: " + swapCount + "회";
        }

        // 슬롯 클릭을 현재 모드에 맞게 처리합니다.
        private void HandleSlotClicked(int index)
        {
            selectedSlot = index;

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

        // 스왑 모드에서 첫 번째/두 번째 슬롯 선택을 처리합니다.
        private void HandleSwapSlotClicked(int index)
        {
            if (inventoryRuntime.Grid.TemporarySwapCount <= 0)
            {
                message = "현재 스왑 가능한 상황이 아닙니다.";
                return;
            }

            if (swapFirstSlot < 0)
            {
                swapFirstSlot = index;
                message = "스왑할 두 번째 칸을 선택하세요.";
                return;
            }

            if (swapFirstSlot == index)
            {
                swapFirstSlot = -1;
                message = "스왑 첫 번째 선택을 취소했습니다.";
                return;
            }

            string swapMessage;
            if (inventoryRuntime.Grid.TrySwapItems(swapFirstSlot, index, out swapMessage))
            {
                inventoryRuntime.RecalculateSynergy();
            }

            swapFirstSlot = -1;
            message = swapMessage;

            if (inventoryRuntime.Grid.TemporarySwapCount <= 0)
            {
                mode = InventoryInteractionMode.Normal;
                message += " / 스왑 모드 종료";
            }
        }

        // 랜덤 아이템 1개를 지급합니다.
        private void GrantRandomItem()
        {
            var items = inventoryRuntime.SortedItems;
            if (items.Count == 0)
            {
                inventoryRuntime.RefreshCatalog();
                items = inventoryRuntime.SortedItems;
            }

            if (items.Count == 0)
            {
                message = "지급할 아이템 데이터가 없습니다.";
                return;
            }

            var record = items[Random.Range(0, items.Count)];
            InventoryPosition position;
            inventoryRuntime.TryGrantItem(record, out position, out message);
        }

        // 지정한 슬롯의 아이템을 판매하고 골드를 추가합니다.
        private void SellSlot(int index)
        {
            InventoryItem removedItem;
            if (!inventoryRuntime.Grid.TryRemoveItem(index, out removedItem))
            {
                message = "판매할 아이템이 없습니다.";
                return;
            }

            inventoryRuntime.AddDebugGold(removedItem.sellPrice);
            inventoryRuntime.RecalculateSynergy();
            message = removedItem.displayName + " 판매: +" + removedItem.sellPrice + " 골드";
        }

        // 마우스 오버 또는 선택 슬롯의 아이템을 버립니다.
        private void DiscardHoveredOrSelectedItem()
        {
            int targetSlot = hoveredSlot >= 0 ? hoveredSlot : selectedSlot;

            InventoryItem removedItem;
            if (!inventoryRuntime.Grid.TryRemoveItem(targetSlot, out removedItem))
            {
                message = "버릴 아이템에 마우스를 올려주세요.";
                return;
            }

            if (selectedSlot == targetSlot)
            {
                selectedSlot = -1;
            }

            if (swapFirstSlot == targetSlot)
            {
                swapFirstSlot = -1;
            }

            message = removedItem.displayName + " 버림";
            inventoryRuntime.RecalculateSynergy();
        }

        // 지정한 슬롯의 프레임 속성을 다음 속성으로 변경합니다.
        private void CycleFrameElement(int index)
        {
            var slot = inventoryRuntime.Grid.GetSlot(index);
            var elements = inventoryRuntime.SortedElementIds;

            if (slot == null)
            {
                message = "프레임 속성을 바꿀 슬롯을 선택해주세요.";
                return;
            }

            if (elements.Count == 0)
            {
                message = "사용할 속성 정의가 없습니다.";
                return;
            }

            int currentIndex = IndexOf(elements, slot.frameElementId);
            int nextIndex = currentIndex + 1;
            if (nextIndex >= elements.Count)
            {
                slot.SetFrameElement(null);
                message = "프레임 속성 제거";
            }
            else
            {
                slot.SetFrameElement(elements[nextIndex]);
                message = "프레임 속성: " + inventoryRuntime.GetElementDisplayName(slot.frameElementId);
            }

            inventoryRuntime.RecalculateSynergy();
        }

        // 선택한 슬롯에 강화 수치를 적용합니다.
        private void UpgradeSelectedSlot()
        {
            int upgradeAmount;
            inventoryRuntime.TryAddRolledSlotUpgrade(selectedSlot, out upgradeAmount, out message);
        }

        // 선택 상태를 초기화합니다.
        private void ResetSelection()
        {
            selectedSlot = -1;
            swapFirstSlot = -1;
        }

        // 현재 모드의 표시 이름을 반환합니다.
        // 현재 세션의 던전 층을 디버그 패널에 표시합니다.
        private static string BuildDungeonFloorText()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null || SaveGameSession.CurrentSaveData.dungeon == null)
            {
                return "던전 층: -";
            }

            return "던전 층: " + SaveGameSession.CurrentSaveData.dungeon.currentFloor;
        }

        private string GetModeDisplayName()
        {
            switch (mode)
            {
                case InventoryInteractionMode.Swap:
                    return "스왑";
                case InventoryInteractionMode.Sell:
                    return "판매";
                case InventoryInteractionMode.FrameElement:
                    return "프레임";
                default:
                    return "일반";
            }
        }

        // 현재 모드의 도움말을 만듭니다.
        private string BuildModeHelpText()
        {
            if (mode == InventoryInteractionMode.Swap)
            {
                return "스왑: 첫 칸 선택 후 두 번째 칸 선택 / 남은 횟수 " + inventoryRuntime.Grid.TemporarySwapCount;
            }

            if (mode == InventoryInteractionMode.Sell)
            {
                return "판매: 아이템 칸 클릭 시 즉시 판매";
            }

            if (mode == InventoryInteractionMode.FrameElement)
            {
                return "프레임: 슬롯 클릭 시 프레임 속성 변경";
            }

            return "일반: 슬롯 선택, F로 아이템 버리기";
        }

        // 선택 상태인지 확인합니다.
        private bool IsSelected(int index)
        {
            return selectedSlot == index;
        }

        // 아이템 속성 표시 문자열을 만듭니다.
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

        // UI에 표시할 아이템 대표 속성을 찾습니다. advanced 속성은 표시하지 않습니다.
        private bool TryGetDisplayElementId(InventoryItem item, out string elementId)
        {
            elementId = null;
            if (item == null)
            {
                return false;
            }

            int bestValue = int.MinValue;
            for (int i = 0; i < item.elements.Count; i++)
            {
                var element = item.elements[i];
                if (!inventoryRuntime.IsElementVisibleInInventory(element.elementId))
                {
                    continue;
                }

                if (element.elementValue > bestValue)
                {
                    bestValue = element.elementValue;
                    elementId = element.elementId;
                }
            }

            return !string.IsNullOrEmpty(elementId);
        }

        // 리스트에서 문자열의 위치를 찾습니다.
        private static int IndexOf(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        // 속성 ID에 맞는 UI 색상을 반환합니다.
        private static Color GetElementColor(string elementId)
        {
            switch (elementId)
            {
                case "fire":
                    return new Color(0.95f, 0.22f, 0.12f, 1f);
                case "water":
                    return new Color(0.18f, 0.52f, 0.95f, 1f);
                case "nature":
                    return new Color(0.22f, 0.75f, 0.32f, 1f);
                case "earth":
                    return new Color(0.66f, 0.48f, 0.27f, 1f);
                case "lightning":
                    return new Color(1f, 0.86f, 0.18f, 1f);
                case "poison":
                    return new Color(0.62f, 0.32f, 0.88f, 1f);
                case "dark":
                    return new Color(0.32f, 0.24f, 0.58f, 1f);
                case "curse":
                    return new Color(0.55f, 0.06f, 0.12f, 1f);
                case "light":
                    return new Color(1f, 0.95f, 0.62f, 1f);
                case "neutral":
                    return new Color(0.68f, 0.72f, 0.76f, 1f);
                case "lava":
                    return new Color(1f, 0.38f, 0.04f, 1f);
                case "ice":
                    return new Color(0.45f, 0.9f, 1f, 1f);
                case "wind":
                    return new Color(0.5f, 1f, 0.72f, 1f);
                case "steel":
                    return new Color(0.58f, 0.64f, 0.7f, 1f);
                case "void":
                    return new Color(0.15f, 0.08f, 0.24f, 1f);
                case "chemical":
                    return new Color(0.2f, 0.95f, 0.42f, 1f);
                case "harmony":
                    return new Color(0.94f, 0.74f, 1f, 1f);
                default:
                    return ItemIconResolver.GetPlaceholderColor(elementId);
            }
        }

        // 단색 사각형을 그립니다.
        private static void DrawFilledRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        // 사각형 테두리를 그립니다.
        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        // Sprite를 IMGUI에 맞게 표시합니다.
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
