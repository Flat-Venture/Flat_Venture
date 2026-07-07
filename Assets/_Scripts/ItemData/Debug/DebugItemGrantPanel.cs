using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.ItemData
{
    // F2 입력으로 열리는 아이템 지급 테스트 패널입니다.
    // 아이템 목록과 임시 인벤토리에 아이콘을 표시해서 ItemAssetDatabaseSO 연결 상태를 확인할 수 있습니다.
    public sealed class DebugItemGrantPanel : MonoBehaviour
    {
        private const string WindowTitle = "\uC544\uC774\uD15C \uCE58\uD2B8 \uC9C0\uAE09";
        private const string ItemListTitle = "\uC804\uCCB4 \uC544\uC774\uD15C";
        private const string InventoryTitle = "\uC784\uC2DC \uC778\uBCA4\uD1A0\uB9AC ";
        private const string ClearButtonText = "\uBE44\uC6B0\uAE30";
        private const string CloseButtonText = "\uB2EB\uAE30";
        private const string EmptyCatalogMessage = "GameDataLoaderBehaviour\uB97C \uCC3E\uC9C0 \uBABB\uD588\uAC70\uB098 Catalog\uAC00 \uC544\uC9C1 \uB85C\uB4DC\uB418\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4.";
        private const string CheckLoaderMessage = "\uC2EC\uC5D0 GameDataLoaderBehaviour\uB97C \uBD99\uC778 \uC624\uBE0C\uC81D\uD2B8\uAC00 \uC788\uB294\uC9C0 \uD655\uC778\uD558\uC138\uC694.";
        private const string InitialMessage = "F2\uB85C \uC544\uC774\uD15C \uCE58\uD2B8 \uD328\uB110\uC744 \uC5F4 \uC218 \uC788\uC2B5\uB2C8\uB2E4.";
        private const string ClearedMessage = "\uC784\uC2DC \uC778\uBCA4\uD1A0\uB9AC\uB97C \uBE44\uC6E0\uC2B5\uB2C8\uB2E4.";

        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private ItemAssetDatabaseSO itemAssetDatabase;
        [SerializeField] private Key toggleKey = Key.F2;
        [SerializeField] private int inventoryCapacity = 25;
        [SerializeField] private int panelWidth = 1280;
        [SerializeField] private int panelHeight = 720;
        [SerializeField] private int iconSize = 28;

        private DebugItemGrantInventory inventory;
        private GameDataCatalog catalog;
        private Vector2 itemScroll;
        private Vector2 inventoryScroll;
        private ItemRecord hoveredInventoryItem;
        private Rect hoveredTooltipRect;
        private bool isOpen;
        private string lastMessage = InitialMessage;
        private GUIStyle itemButtonStyle;
        private GUIStyle inventoryRowStyle;

        // 임시 인벤토리를 만들고 씬에 있는 데이터 로더를 찾습니다.
        private void Awake()
        {
            inventory = new DebugItemGrantInventory(inventoryCapacity);

            if (dataLoader == null)
                dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
        }

        // 토글 키 입력을 감지하고 Catalog 참조를 갱신합니다.
        private void Update()
        {
            if (!WasToggleKeyPressed())
                return;

            isOpen = !isOpen;
            RefreshCatalog();
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

            EnsureStyles();
            var rect = new Rect(20f, 20f, panelWidth, panelHeight);
            GUILayout.Window(GetInstanceID(), rect, DrawWindow, WindowTitle);
        }

        // 데이터 로더에서 최신 Catalog를 가져옵니다.
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
                GUILayout.Label(EmptyCatalogMessage);
                GUILayout.Label(CheckLoaderMessage);
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
            GUILayout.Label("\uD1A0\uAE00 \uD0A4: " + toggleKey);
            GUILayout.Label("ItemAssetDatabase: " + BuildDatabaseStatusText());
            GUILayout.Label("\uC0C1\uD0DC: " + lastMessage);
            GUILayout.Space(6f);
        }

        // 전체 아이템 버튼 목록을 출력합니다.
        private void DrawItemList()
        {
            GUILayout.BeginVertical(GUILayout.Width(panelWidth * 0.58f));
            GUILayout.Label(ItemListTitle);

            itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.Height(panelHeight - 125f));
            foreach (var item in GetSortedItems())
                DrawItemButton(item);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        // 아이콘이 포함된 아이템 지급 버튼을 출력합니다.
        private void DrawItemButton(ItemRecord item)
        {
            if (item == null || item.definition == null)
                return;

            var itemId = item.definition.itemId;
            var label = itemId + " / " + item.definition.displayName;
            var rect = GUILayoutUtility.GetRect(1f, iconSize + 8f, GUILayout.ExpandWidth(true));

            if (GUI.Button(rect, GUIContent.none, itemButtonStyle))
                GrantItem(item);

            var iconRect = new Rect(rect.x + 6f, rect.y + 4f, iconSize, iconSize);
            var labelRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - iconSize - 18f, rect.height);
            DrawSprite(iconRect, GetIconSprite(itemId));
            GUI.Label(labelRect, label);
        }

        // 클릭한 아이템을 임시 인벤토리에 추가합니다.
        private void GrantItem(ItemRecord item)
        {
            if (inventory.TryAdd(item, out lastMessage))
                Debug.Log("[DebugItemGrantPanel] " + lastMessage + " (" + inventory.Count + "/" + inventory.Capacity + ")");
            else
                Debug.LogWarning("[DebugItemGrantPanel] " + lastMessage);
        }

        // 현재 보유 중인 임시 아이템 목록을 출력합니다.
        private void DrawInventory()
        {
            GUILayout.BeginVertical(GUILayout.Width(panelWidth * 0.36f));
            GUILayout.Label(InventoryTitle + inventory.Count + "/" + inventory.Capacity);

            hoveredInventoryItem = null;
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(panelHeight - 155f));
            var acquiredItems = inventory.AcquiredItems;
            for (var i = 0; i < acquiredItems.Count; i++)
                DrawInventoryItemRow(i, acquiredItems[i]);
            GUILayout.EndScrollView();

            if (GUILayout.Button(ClearButtonText, GUILayout.Height(28f)))
            {
                inventory.Clear();
                lastMessage = ClearedMessage;
            }

            GUILayout.EndVertical();
        }

        // 임시 인벤토리 한 줄을 아이콘과 함께 출력하고 마우스 오버를 감지합니다.
        private void DrawInventoryItemRow(int index, DebugAcquiredItem acquiredItem)
        {
            if (acquiredItem == null)
                return;

            var rowHeight = iconSize + 8f;
            var rowRect = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true));
            GUI.Box(rowRect, GUIContent.none, inventoryRowStyle);

            var iconRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, iconSize, iconSize);
            var labelRect = new Rect(iconRect.xMax + 8f, rowRect.y, rowRect.width - iconSize - 16f, rowHeight);
            DrawSprite(iconRect, GetIconSprite(acquiredItem.itemId));
            GUI.Label(labelRect, (index + 1) + ". " + acquiredItem.displayName);

            if (!rowRect.Contains(Event.current.mousePosition))
                return;

            hoveredInventoryItem = acquiredItem.itemRecord;
            hoveredTooltipRect = new Rect(rowRect.xMax + 12f, rowRect.y, 360f, 260f);
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
            text += "\uD76C\uADC0\uB3C4: " + item.definition.rarityId + "\n";
            text += "\uACE0\uC720: " + item.definition.isUnique + " / \uC800\uC8FC: " + item.definition.isCursed + "\n\n";
            text += BuildElementText(item);
            text += BuildStatText(item);
            text += BuildEffectText(item);
            return text;
        }

        // 속성 정보를 툴팁 문자열로 만듭니다.
        private static string BuildElementText(ItemRecord item)
        {
            if (item.elements.Count == 0)
                return "\uC18D\uC131: \uC5C6\uC74C\n";

            var text = "\uC18D\uC131:\n";
            for (var i = 0; i < item.elements.Count; i++)
                text += "- " + item.elements[i].elementId + " +" + item.elements[i].elementValue + "\n";

            return text;
        }

        // 스탯 정보를 툴팁 문자열로 만듭니다.
        private static string BuildStatText(ItemRecord item)
        {
            if (item.stats.Count == 0)
                return "\uC2A4\uD0EF: \uC5C6\uC74C\n";

            var text = "\uC2A4\uD0EF:\n";
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
                return "\uD6A8\uACFC: \uC5C6\uC74C";

            var text = "\uD6A8\uACFC:\n";
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
            if (GUILayout.Button(CloseButtonText, GUILayout.Height(28f)))
                isOpen = false;
        }

        // 아이템 목록을 item_id 기준으로 정렬해서 반환합니다.
        private List<ItemRecord> GetSortedItems()
        {
            var items = new List<ItemRecord>(catalog.items.Values);
            items.Sort(CompareItemId);
            return items;
        }

        // item_id로 아이콘 Texture를 가져옵니다.
        private Sprite GetIconSprite(string itemId)
        {
            return ItemIconResolver.ResolveIcon(itemId, itemAssetDatabase);
        }

        // Sprite의 textureRect를 반영해서 IMGUI 영역에 그립니다.
        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.ScaleToFit);
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

        // 패널 상단에 보여줄 Database 연결 상태 문자열을 만듭니다.
        private string BuildDatabaseStatusText()
        {
            if (itemAssetDatabase == null)
                return "None - Inspector\uC5D0 ItemAssetDatabase.asset\uC744 \uC5F0\uACB0\uD574\uC57C \uC2E4\uC81C \uC544\uC774\uCF58\uC774 \uB098\uC635\uB2C8\uB2E4.";

            return itemAssetDatabase.ItemAssets.Count + " assets";
        }

        // IMGUI 스타일을 준비합니다.
        private void EnsureStyles()
        {
            if (itemButtonStyle == null)
            {
                itemButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    imagePosition = ImagePosition.ImageLeft,
                    fixedHeight = iconSize + 8f
                };
            }

            if (inventoryRowStyle == null)
            {
                inventoryRowStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleLeft
                };
            }
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
