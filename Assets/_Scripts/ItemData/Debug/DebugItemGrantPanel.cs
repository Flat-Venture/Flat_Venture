using FlatVenture.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.ItemData
{
    // F2로 열리는 아이템 지급 치트 패널입니다.
    // 클릭한 아이템은 InventoryRuntimeBehaviour가 들고 있는 실제 테스트 인벤토리에 바로 들어갑니다.
    public sealed class DebugItemGrantPanel : MonoBehaviour
    {
        private const string WindowTitle = "아이템 치트 지급";
        private const string EmptyCatalogMessage = "아이템 데이터가 아직 로드되지 않았습니다.";
        private const string MissingRuntimeMessage = "InventoryRuntimeBehaviour를 찾지 못했습니다.";

        [SerializeField] private InventoryRuntimeBehaviour inventoryRuntime;
        [SerializeField] private ItemAssetDatabaseSO itemAssetDatabase;
        [SerializeField] private Key toggleKey = Key.F2;
        [SerializeField] private int panelWidth = 620;
        [SerializeField] private int panelHeight = 640;
        [SerializeField] private int iconSize = 28;

        private Vector2 itemScroll;
        private bool isOpen;
        private string lastMessage = "F2로 아이템 치트 지급창을 열 수 있습니다.";
        private GUIStyle itemButtonStyle;

        // 씬에 있는 공유 인벤토리 런타임을 찾습니다.
        private void Awake()
        {
            FindRuntimeIfNeeded();
        }

        // Input System으로 치트 지급창 토글을 처리합니다.
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                return;
            }

            isOpen = !isOpen;
            FindRuntimeIfNeeded();

            if (inventoryRuntime != null)
            {
                inventoryRuntime.RefreshCatalog();
            }
        }

        // 치트 지급창을 그립니다.
        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            var rect = new Rect(20f, 20f, panelWidth, panelHeight);
            GUILayout.Window(GetInstanceID(), rect, DrawWindow, WindowTitle);
        }

        // 패널 전체 레이아웃을 그립니다.
        private void DrawWindow(int windowId)
        {
            DrawHeader();

            if (inventoryRuntime == null)
            {
                GUILayout.Label(MissingRuntimeMessage);
                DrawFooter();
                return;
            }

            if (inventoryRuntime.Catalog == null)
            {
                GUILayout.Label(EmptyCatalogMessage);
                if (GUILayout.Button("데이터 새로고침", GUILayout.Height(28f)))
                {
                    inventoryRuntime.RefreshCatalog();
                }

                DrawFooter();
                return;
            }

            DrawItemList();
            DrawFooter();
            GUI.DragWindow();
        }

        // 상단 상태 정보를 출력합니다.
        private void DrawHeader()
        {
            GUILayout.Label("토글 키: " + toggleKey);
            GUILayout.Label("상태: " + lastMessage);

            if (inventoryRuntime != null && inventoryRuntime.Grid != null)
            {
                GUILayout.Label("인벤토리: " + CountItems() + "/" + inventoryRuntime.Grid.Capacity);
            }

            GUILayout.Space(6f);
        }

        // 전체 아이템 지급 버튼 목록을 출력합니다.
        private void DrawItemList()
        {
            GUILayout.Label("전체 아이템");
            itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.Height(panelHeight - 125f));

            var items = inventoryRuntime.SortedItems;
            for (int i = 0; i < items.Count; i++)
            {
                DrawItemButton(items[i]);
            }

            GUILayout.EndScrollView();
        }

        // 아이콘이 포함된 아이템 지급 버튼을 출력합니다.
        private void DrawItemButton(ItemRecord item)
        {
            if (item == null || item.definition == null)
            {
                return;
            }

            string itemId = item.definition.itemId;
            string label = itemId + " / " + item.definition.displayName;
            var rect = GUILayoutUtility.GetRect(1f, iconSize + 8f, GUILayout.ExpandWidth(true));

            if (GUI.Button(rect, GUIContent.none, itemButtonStyle))
            {
                GrantItem(item);
            }

            var iconRect = new Rect(rect.x + 6f, rect.y + 4f, iconSize, iconSize);
            var labelRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - iconSize - 18f, rect.height);
            DrawSprite(iconRect, ItemIconResolver.ResolveIcon(itemId, itemAssetDatabase));
            GUI.Label(labelRect, label);
        }

        // 클릭한 아이템을 공유 인벤토리에 지급합니다.
        private void GrantItem(ItemRecord item)
        {
            InventoryPosition position;
            if (inventoryRuntime.TryGrantItem(item, out position, out lastMessage))
            {
                Debug.Log("[DebugItemGrantPanel] " + lastMessage);
            }
            else
            {
                Debug.LogWarning("[DebugItemGrantPanel] " + lastMessage);
            }
        }

        // 하단 닫기 버튼을 출력합니다.
        private void DrawFooter()
        {
            GUILayout.Space(6f);
            if (GUILayout.Button("닫기", GUILayout.Height(28f)))
            {
                isOpen = false;
            }
        }

        // 현재 인벤토리에 들어있는 아이템 수를 계산합니다.
        private int CountItems()
        {
            int count = 0;
            var slots = inventoryRuntime.Grid.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].HasItem)
                {
                    count++;
                }
            }

            return count;
        }

        // 씬에 있는 InventoryRuntimeBehaviour를 찾습니다.
        private void FindRuntimeIfNeeded()
        {
            if (inventoryRuntime == null)
            {
                inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            }
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

        // IMGUI 스타일을 준비합니다.
        private void EnsureStyles()
        {
            if (itemButtonStyle != null)
            {
                return;
            }

            itemButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = iconSize + 8f
            };
        }
    }
}
