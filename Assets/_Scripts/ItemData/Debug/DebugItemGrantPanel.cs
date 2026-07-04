using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.ItemData
{
    // 키 입력으로 여닫는 아이템 지급 테스트 패널입니다.
    // 모든 아이템을 버튼으로 보여주고, 클릭한 아이템의 한글 이름만 임시 보관함에 저장합니다.
    public sealed class DebugItemGrantPanel : MonoBehaviour
    {
        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private Key toggleKey = Key.F2;
        [SerializeField] private int inventoryCapacity = 25;
        [SerializeField] private int panelWidth = 1280;
        [SerializeField] private int panelHeight = 720;

        private DebugItemGrantInventory inventory;
        private GameDataCatalog catalog;
        private Vector2 itemScroll;
        private Vector2 inventoryScroll;
        private ItemRecord hoveredInventoryItem;
        private Rect hoveredTooltipRect;
        private bool isOpen;
        private string lastMessage = "F2 키로 아이템 치트 패널을 열 수 있습니다.";

        // 임시 보관함을 만들고 씬에 있는 데이터 로더를 찾습니다.
        private void Awake()
        {
            inventory = new DebugItemGrantInventory(inventoryCapacity);

            if (dataLoader == null)
                dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
        }

        // 토글 키 입력을 감지하고 카탈로그 참조를 갱신합니다.
        private void Update()
        {
            if (WasToggleKeyPressed())
            {
                isOpen = !isOpen;
                RefreshCatalog();
            }
        }

        // Input System에서 토글 키가 이번 프레임에 눌렸는지 확인합니다.
        private bool WasToggleKeyPressed()
        {
            return Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame;
        }

        // 열린 상태일 때만 치트 패널을 그립니다.
        private void OnGUI()
        {
            if (!isOpen)
                return;

            var rect = new Rect(20f, 20f, panelWidth, panelHeight);
            GUILayout.Window(GetInstanceID(), rect, DrawWindow, "아이템 치트 지급");
        }

        // 데이터 로더에서 최신 카탈로그를 가져옵니다.
        private void RefreshCatalog()
        {
            if (dataLoader == null)
                dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();

            catalog = dataLoader != null ? dataLoader.Catalog : null;
        }

        // 패널 전체 레이아웃을 그립니다.
        private void DrawWindow(int windowId)
        {
            DrawHeader();

            if (catalog == null)
            {
                GUILayout.Label("GameDataLoaderBehaviour를 찾지 못했거나 아직 Catalog가 로드되지 않았습니다.");
                GUILayout.Label("씬에 GameDataLoaderBehaviour를 붙인 오브젝트가 있는지 확인하세요.");
                DrawFooter();
                return;
            }

            GUILayout.BeginHorizontal();
            DrawItemList();
            DrawInventory();
            GUILayout.EndHorizontal();

            DrawHoveredItemTooltip();
            DrawFooter();
            GUI.DragWindow();
        }

        // 상단 상태 정보를 출력합니다.
        private void DrawHeader()
        {
            GUILayout.Label("토글 키: " + toggleKey);
            GUILayout.Label("상태: " + lastMessage);
            GUILayout.Space(6f);
        }

        // 전체 아이템 버튼 목록을 출력합니다.
        private void DrawItemList()
        {
            GUILayout.BeginVertical(GUILayout.Width(panelWidth * 0.58f));
            GUILayout.Label("전체 아이템");

            itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.Height(panelHeight - 125f));
            foreach (var item in GetSortedItems())
                DrawItemButton(item);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        // 아이템 하나를 지급 버튼으로 출력합니다.
        private void DrawItemButton(ItemRecord item)
        {
            if (item == null || item.definition == null)
                return;

            var label = item.definition.itemId + " / " + item.definition.displayName;
            if (GUILayout.Button(label, GUILayout.Height(26f)))
                GrantItem(item);
        }

        // 클릭한 아이템을 임시 보관함에 추가합니다.
        private void GrantItem(ItemRecord item)
        {
            if (inventory.TryAdd(item, out lastMessage))
                Debug.Log("[DebugItemGrantPanel] " + lastMessage + " (" + inventory.Count + "/" + inventory.Capacity + ")");
            else
                Debug.LogWarning("[DebugItemGrantPanel] " + lastMessage);
        }

        // 현재 보유 중인 임시 아이템 이름 목록을 출력합니다.
        private void DrawInventory()
        {
            GUILayout.BeginVertical(GUILayout.Width(panelWidth * 0.36f));
            GUILayout.Label("임시 인벤토리 " + inventory.Count + "/" + inventory.Capacity);

            hoveredInventoryItem = null;
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(panelHeight - 155f));
            var acquiredItems = inventory.AcquiredItems;
            for (var i = 0; i < acquiredItems.Count; i++)
                DrawInventoryItemLabel(i, acquiredItems[i]);
            GUILayout.EndScrollView();

            if (GUILayout.Button("비우기", GUILayout.Height(28f)))
            {
                inventory.Clear();
                lastMessage = "임시 인벤토리를 비웠습니다.";
            }

            GUILayout.EndVertical();
        }

        // 임시 인벤토리 안의 아이템 이름을 출력하고 마우스 오버를 감지합니다.
        private void DrawInventoryItemLabel(int index, DebugAcquiredItem acquiredItem)
        {
            if (acquiredItem == null)
                return;

            GUILayout.Label((index + 1) + ". " + acquiredItem.displayName, GUILayout.Height(24f));
            var itemRect = GUILayoutUtility.GetLastRect();

            if (!itemRect.Contains(Event.current.mousePosition))
                return;

            hoveredInventoryItem = acquiredItem.itemRecord;
            hoveredTooltipRect = new Rect(itemRect.xMax + 12f, itemRect.y, 360f, 260f);
        }

        // 마우스를 올린 인벤토리 아이템의 상세 정보를 오른쪽에 출력합니다.
        private void DrawHoveredItemTooltip()
        {
            if (hoveredInventoryItem == null || hoveredInventoryItem.definition == null)
                return;

            GUI.Box(hoveredTooltipRect, BuildItemTooltipText(hoveredInventoryItem));
        }

        // 아이템 상세 툴팁 문자열을 만듭니다.
        private static string BuildItemTooltipText(ItemRecord item)
        {
            var text = item.definition.displayName + "\n";
            text += "ID: " + item.definition.itemId + "\n";
            text += "희귀도: " + item.definition.rarityId + "\n";
            text += "고유: " + item.definition.isUnique + " / 저주: " + item.definition.isCursed + "\n\n";
            text += BuildElementText(item);
            text += BuildStatText(item);
            text += BuildEffectText(item);
            return text;
        }

        // 속성 정보를 툴팁 문자열로 만듭니다.
        private static string BuildElementText(ItemRecord item)
        {
            if (item.elements.Count == 0)
                return "속성: 없음\n";

            var text = "속성:\n";
            for (var i = 0; i < item.elements.Count; i++)
                text += "- " + item.elements[i].elementId + " +" + item.elements[i].elementValue + "\n";

            return text;
        }

        // 스탯 정보를 툴팁 문자열로 만듭니다.
        private static string BuildStatText(ItemRecord item)
        {
            if (item.stats.Count == 0)
                return "스탯: 없음\n";

            var text = "스탯:\n";
            for (var i = 0; i < item.stats.Count; i++)
            {
                var stat = item.stats[i];
                text += "- " + stat.statId + " " + stat.operation + " " + stat.value + "\n";
            }

            return text;
        }

        // 효과 정보를 툴팁 문자열로 만듭니다.
        private static string BuildEffectText(ItemRecord item)
        {
            if (item.effects.Count == 0)
                return "효과: 없음";

            var text = "효과:\n";
            for (var i = 0; i < item.effects.Count; i++)
            {
                var effect = item.effects[i];
                text += "- " + effect.triggerId + " -> " + effect.actionId + "\n";
            }

            return text;
        }

        // 하단 닫기 버튼을 출력합니다.
        private void DrawFooter()
        {
            GUILayout.Space(6f);
            if (GUILayout.Button("닫기", GUILayout.Height(28f)))
                isOpen = false;
        }

        // 아이템 목록을 item_id 기준으로 정렬해서 반환합니다.
        private List<ItemRecord> GetSortedItems()
        {
            var items = new List<ItemRecord>(catalog.items.Values);
            items.Sort(CompareItemId);
            return items;
        }

        // 아이템 ID를 기준으로 정렬합니다.
        private static int CompareItemId(ItemRecord left, ItemRecord right)
        {
            var leftId = left != null && left.definition != null ? left.definition.itemId : string.Empty;
            var rightId = right != null && right.definition != null ? right.definition.itemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
