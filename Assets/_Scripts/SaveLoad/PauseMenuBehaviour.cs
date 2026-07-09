using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FlatVenture.Inventory;

namespace FlatVenture.SaveLoad
{
    // 마을/던전 씬에서 ESC로 열리는 임시 메뉴입니다.
    // 시간 정지와 입력 차단은 이후 담당 시스템이 붙을 예정이므로 여기서는 버튼과 저장 흐름만 담당합니다.
    public sealed class PauseMenuBehaviour : MonoBehaviour
    {
        private const int PauseCanvasSortingOrder = 10000;
        private static int openMenuCount;

        public static bool IsAnyOpen
        {
            get { return openMenuCount > 0; }
        }

        [SerializeField] private bool isDungeonScene;
        [SerializeField] private Key toggleKey = Key.Escape;
        [SerializeField] private string titleSceneName = "Test_03_Title";
        [SerializeField] private string townSceneName = "Test_LRH_Town";
        [SerializeField] private Font font;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.62f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.18f, 0.21f, 0.96f);
        [SerializeField] private Color buttonColor = new Color(0.18f, 0.34f, 0.38f, 1f);
        [SerializeField] private Color dangerButtonColor = new Color(0.65f, 0.16f, 0.12f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.24f, 1f);

        private RectTransform menuRoot;
        private Text statusText;
        private bool isOpen;

        // ESC 메뉴 UI를 만들고 처음에는 숨깁니다.
        private void Start()
        {
            Build();
            Close();
        }

        // ESC 키 입력으로 메뉴를 열고 닫습니다.
        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                Toggle();
        }

        // 메뉴를 토글합니다.
        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        // 메뉴를 엽니다.
        public void Open()
        {
            SetOpenState(true);
            menuRoot.gameObject.SetActive(true);
            menuRoot.SetAsLastSibling();
            SetStatus(string.Empty);
        }

        // 메뉴를 닫습니다.
        public void Close()
        {
            SetOpenState(false);

            if (menuRoot != null)
                menuRoot.gameObject.SetActive(false);
        }

        // 메뉴 열림 상태를 전역으로 공유해서 다른 디버그 UI가 뒤에서 클릭되지 않게 합니다.
        private void SetOpenState(bool nextOpen)
        {
            if (isOpen == nextOpen)
            {
                return;
            }

            isOpen = nextOpen;
            openMenuCount = Mathf.Max(0, openMenuCount + (nextOpen ? 1 : -1));
        }

        // 씬 전환이나 오브젝트 제거 중 열린 상태가 남지 않게 정리합니다.
        private void OnDestroy()
        {
            if (isOpen)
            {
                SetOpenState(false);
            }
        }

        // 계속하기 버튼 처리입니다.
        public void OnContinueClicked()
        {
            Close();
        }

        // 설정 버튼 처리입니다.
        public void OnSettingsClicked()
        {
            SetStatus("설정은 이후 단계에서 연결합니다.");
        }

        // 던전 포기 버튼 처리입니다. 현재 세이브의 던전 진행을 비우고 저장한 뒤 마을로 이동합니다.
        public void OnAbandonDungeonClicked()
        {
            if (!SaveGameSession.HasActiveSave)
            {
                SetStatus("활성 세이브가 없어 던전을 포기할 수 없습니다.");
                return;
            }

            ResetDungeonProgress(SaveGameSession.CurrentSaveData);
            ClearInventoryRuntimeIfExists();
            SaveCurrentSession("던전 포기");
            SceneManager.LoadScene(townSceneName);
        }

        // 게임 종료 버튼 처리입니다. 현재 상태를 저장하고 타이틀로 돌아갑니다.
        public void OnExitToTitleClicked()
        {
            SaveCurrentSession("게임 종료");
            SaveGameSession.Clear();
            SceneManager.LoadScene(titleSceneName);
        }

        // 현재 세션을 파일에 저장합니다.
        private void SaveCurrentSession(string reason)
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                Debug.LogWarning("[PauseMenu] 저장할 활성 세이브가 없습니다. 사유: " + reason);
                return;
            }

            CaptureInventoryIfExists(SaveGameSession.CurrentSaveData);
            SaveLoadService.Save(SaveGameSession.CurrentSlotIndex, SaveGameSession.CurrentSaveData);
            Debug.Log("[PauseMenu] 저장 완료: " + reason + " / 슬롯 " + SaveGameSession.CurrentSlotIndex);
        }

        // 씬에 인벤토리 런타임이 있으면 저장 직전에 현재 인벤토리 상태를 SaveData에 반영합니다.
        private static void CaptureInventoryIfExists(SaveData saveData)
        {
            var inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            if (inventoryRuntime != null)
            {
                inventoryRuntime.CaptureToSaveData(saveData);
            }
        }

        // 던전 포기처럼 던전 진행을 버리는 상황에서는 런타임 인벤토리도 함께 비웁니다.
        private static void ClearInventoryRuntimeIfExists()
        {
            var inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            if (inventoryRuntime != null)
            {
                inventoryRuntime.ClearInventoryForTest();
            }
        }

        // 던전 진행 데이터를 던전 밖 상태로 초기화합니다.
        private static void ResetDungeonProgress(SaveData saveData)
        {
            DungeonRunSaveUtility.ClearDungeonProgress(saveData);
        }

        // 메뉴 UI 전체를 생성합니다.
        private void Build()
        {
            ResolveFont();

            var canvas = EnsureCanvas();
            EnsureCanvasScaler(canvas);
            EnsureGraphicRaycaster(canvas);
            EnsureEventSystem();
            canvas.overrideSorting = true;
            canvas.sortingOrder = PauseCanvasSortingOrder;

            menuRoot = CreateRect("Pause Menu", transform);
            StretchToParent(menuRoot);
            CreateImage(menuRoot.gameObject, dimColor);

            var panel = CreateRect("Panel", menuRoot);
            panel.anchorMin = new Vector2(0.36f, 0.24f);
            panel.anchorMax = new Vector2(0.64f, 0.76f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            CreateImage(panel.gameObject, panelColor);

            var title = CreateText("Title", panel, "일시 정지", 34, titleColor, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.8f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var buttonArea = CreateRect("Button Area", panel);
            buttonArea.anchorMin = new Vector2(0.16f, 0.22f);
            buttonArea.anchorMax = new Vector2(0.84f, 0.78f);
            buttonArea.offsetMin = Vector2.zero;
            buttonArea.offsetMax = Vector2.zero;

            var buttonCount = isDungeonScene ? 4 : 3;
            var order = 0;
            CreateMenuButton(buttonArea, "계속하기", order++, buttonCount, buttonColor, OnContinueClicked);
            CreateMenuButton(buttonArea, "설정", order++, buttonCount, buttonColor, OnSettingsClicked);

            if (isDungeonScene)
                CreateMenuButton(buttonArea, "던전 포기", order++, buttonCount, dangerButtonColor, OnAbandonDungeonClicked);

            CreateMenuButton(buttonArea, "게임 종료", order, buttonCount, dangerButtonColor, OnExitToTitleClicked);

            statusText = CreateText("Status", panel, string.Empty, 18, textColor, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0.08f, 0.05f);
            statusText.rectTransform.anchorMax = new Vector2(0.92f, 0.18f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
        }

        // 버튼 하나를 생성합니다.
        private void CreateMenuButton(RectTransform parent, string label, int order, int buttonCount, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var gap = 0.04f;
            var height = (1f - gap * (buttonCount - 1)) / buttonCount;
            var top = 1f - order * (height + gap);

            var buttonRoot = CreateRect(label + " Button", parent);
            buttonRoot.anchorMin = new Vector2(0f, top - height);
            buttonRoot.anchorMax = new Vector2(1f, top);
            buttonRoot.offsetMin = Vector2.zero;
            buttonRoot.offsetMax = Vector2.zero;

            var image = CreateImage(buttonRoot.gameObject, color);
            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.85f;
            button.colors = colors;

            var text = CreateText("Label", buttonRoot, label, 24, textColor, TextAnchor.MiddleCenter);
            StretchToParent(text.rectTransform);
        }

        // 하단 상태 메시지를 변경합니다.
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        // 사용할 폰트를 찾습니다.
        private void ResolveFont()
        {
            if (font != null)
                return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Canvas가 없으면 현재 오브젝트에 생성합니다.
        private Canvas EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        // 해상도 변화에 대응하기 위한 CanvasScaler를 보장합니다.
        private static void EnsureCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 버튼 입력을 받을 GraphicRaycaster를 보장합니다.
        private static void EnsureGraphicRaycaster(Canvas canvas)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Input System UI 버튼 입력을 받을 EventSystem을 보장합니다.
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // RectTransform UI 오브젝트를 생성합니다.
        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        // 부모 전체를 채우도록 RectTransform을 설정합니다.
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Image 컴포넌트를 추가하고 색을 지정합니다.
        private static Image CreateImage(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
                image = target.AddComponent<Image>();

            image.color = color;
            return image;
        }

        // Text UI를 생성합니다.
        private Text CreateText(string objectName, RectTransform parent, string text, int size, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(objectName, parent);
            var textComponent = rect.gameObject.AddComponent<Text>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.raycastTarget = false;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            return textComponent;
        }
    }
}
