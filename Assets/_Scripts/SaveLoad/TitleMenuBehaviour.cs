using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlatVenture.SaveLoad
{
    // 임시 타이틀 씬에서 시작하기, 이어하기, 설정, 종료 버튼을 생성하고 처리합니다.
    // 시작하기/이어하기는 세이브 슬롯 선택 패널을 연 뒤 선택된 슬롯으로 게임 씬에 진입합니다.
    public sealed class TitleMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Test_LRH_Town";
        [SerializeField] private string dungeonSceneName = "Test_LRH_Dungeon";
        [SerializeField] private Font font;
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
        [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.24f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.18f, 0.34f, 0.38f, 1f);
        [SerializeField] private Color disabledButtonColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);
        [SerializeField] private Color textColor = Color.white;

        private RectTransform mainMenuRoot;
        private RectTransform menuContentRoot;
        private Button continueButton;
        private Text statusText;
        private SaveSlotSelectPanelBehaviour slotPanel;

        // 타이틀 메뉴 UI를 생성합니다.
        private void OnEnable()
        {
            RefreshContinueButton();
        }

        // 타이틀 메뉴 UI를 생성합니다.
        private void Start()
        {
            Build();
            RefreshContinueButton();
        }

        // 타이틀 메뉴를 다시 표시하고 이어하기 버튼 상태를 갱신합니다.
        public void ShowMainMenu()
        {
            if (menuContentRoot != null)
                menuContentRoot.gameObject.SetActive(true);

            RefreshContinueButton();
        }

        // 시작하기 버튼 처리입니다.
        public void OnStartClicked()
        {
            OpenSlotPanel(SaveSlotSelectPanelBehaviour.SlotPanelMode.NewGame);
        }

        // 이어하기 버튼 처리입니다.
        public void OnContinueClicked()
        {
            if (!HasAnyUsableSave())
            {
                SetStatus("이어할 세이브가 없습니다.");
                RefreshContinueButton();
                return;
            }

            OpenSlotPanel(SaveSlotSelectPanelBehaviour.SlotPanelMode.Continue);
        }

        // 설정 버튼 처리입니다.
        public void OnSettingsClicked()
        {
            SetStatus("설정은 이후 단계에서 연결합니다.");
        }

        // 종료 버튼 처리입니다.
        public void OnQuitClicked()
        {
            Application.Quit();
            Debug.Log("[TitleMenu] 게임 종료 요청");
        }

        // 전체 타이틀 UI를 생성합니다.
        private void Build()
        {
            ClearChildren(transform);
            ResolveFont();

            var canvas = EnsureCanvas();
            EnsureCanvasScaler(canvas);
            EnsureGraphicRaycaster(canvas);
            EnsureEventSystem();

            mainMenuRoot = CreateRect("Title Main Menu", transform);
            StretchToParent(mainMenuRoot);
            CreateImage(mainMenuRoot.gameObject, backgroundColor);

            menuContentRoot = CreateRect("Menu Content", mainMenuRoot);
            StretchToParent(menuContentRoot);

            var title = CreateText("Title", menuContentRoot, "FLAT VENTURE", 72, titleColor, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.2f, 0.58f);
            title.rectTransform.anchorMax = new Vector2(0.8f, 0.78f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var buttonArea = CreateRect("Button Area", menuContentRoot);
            buttonArea.anchorMin = new Vector2(0.39f, 0.18f);
            buttonArea.anchorMax = new Vector2(0.61f, 0.55f);
            buttonArea.offsetMin = Vector2.zero;
            buttonArea.offsetMax = Vector2.zero;

            CreateMenuButton(buttonArea, "시작하기", 0, OnStartClicked);
            continueButton = CreateMenuButton(buttonArea, "이어하기", 1, OnContinueClicked);
            CreateMenuButton(buttonArea, "설정", 2, OnSettingsClicked);
            CreateMenuButton(buttonArea, "게임 종료", 3, OnQuitClicked);
            RefreshContinueButton();

            statusText = CreateText("Status", menuContentRoot, string.Empty, 22, textColor, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0.25f, 0.08f);
            statusText.rectTransform.anchorMax = new Vector2(0.75f, 0.15f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            slotPanel = CreateSlotPanel();
        }

        // 슬롯 선택 패널을 생성합니다.
        private SaveSlotSelectPanelBehaviour CreateSlotPanel()
        {
            var slotPanelRect = CreateRect("Save Slot Select Panel", mainMenuRoot);
            StretchToParent(slotPanelRect);

            var panel = slotPanelRect.gameObject.AddComponent<SaveSlotSelectPanelBehaviour>();
            panel.SetBuildOnStart(false);

            // 슬롯 패널 이벤트 연결 지점입니다.
            // onNewGameCreated/onSaveLoaded는 슬롯 선택 후 게임 씬으로 넘어가게 하고,
            // onBack은 슬롯 화면을 닫고 타이틀 메뉴를 다시 보여줍니다.
            panel.onNewGameCreated.AddListener(HandleNewGameCreated);
            panel.onSaveLoaded.AddListener(HandleSaveLoaded);
            panel.onBack.AddListener(ShowMainMenu);
            panel.PrepareHidden();
            return panel;
        }

        // 슬롯 패널을 열고 메인 메뉴를 숨깁니다.
        private void OpenSlotPanel(SaveSlotSelectPanelBehaviour.SlotPanelMode mode)
        {
            if (slotPanel == null)
                slotPanel = CreateSlotPanel();

            menuContentRoot.gameObject.SetActive(false);

            if (mode == SaveSlotSelectPanelBehaviour.SlotPanelMode.NewGame)
                slotPanel.OpenForNewGame();
            else
                slotPanel.OpenForContinue();
        }

        // 새 게임 생성 후 세션에 등록하고 게임 씬으로 이동합니다.
        private void HandleNewGameCreated(SaveData saveData)
        {
            SaveGameSession.SetActiveSave(saveData, SaveGameSession.StartReason.NewGame);
            LoadGameScene(saveData);
        }

        // 기존 세이브 로드 후 세션에 등록하고 게임 씬으로 이동합니다.
        private void HandleSaveLoaded(SaveData saveData)
        {
            SaveGameSession.SetActiveSave(saveData, SaveGameSession.StartReason.Continue);
            LoadGameScene(saveData);
        }

        // 설정된 게임 씬으로 이동합니다.
        private void LoadGameScene(SaveData saveData)
        {
            var sceneName = ResolveSceneName(saveData);
            if (string.IsNullOrEmpty(sceneName))
            {
                SetStatus("이동할 게임 씬 이름이 비어 있습니다.");
                ShowMainMenu();
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        // 저장 데이터의 진행 위치에 맞는 게임 씬 이름을 반환합니다.
        private string ResolveSceneName(SaveData saveData)
        {
            if (saveData != null && saveData.dungeon != null && saveData.dungeon.isInDungeon)
                return dungeonSceneName;

            return gameSceneName;
        }

        // 이어하기 버튼 활성화 상태를 갱신합니다.
        private void RefreshContinueButton()
        {
            if (continueButton == null)
                return;

            var hasSave = HasAnyUsableSave();
            continueButton.interactable = hasSave;

            var colors = continueButton.colors;
            colors.normalColor = hasSave ? buttonColor : disabledButtonColor;
            colors.highlightedColor = hasSave ? buttonColor * 1.18f : disabledButtonColor;
            continueButton.colors = colors;
        }

        // 사용할 수 있는 세이브가 하나라도 있는지 확인합니다.
        private static bool HasAnyUsableSave()
        {
            var summaries = SaveLoadService.GetAllSlotSummaries();
            for (var i = 0; i < summaries.Length; i++)
            {
                if (summaries[i].exists && !summaries[i].isCorrupted)
                    return true;
            }

            return false;
        }

        // 하단 상태 메시지를 변경합니다.
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        // 메뉴 버튼 하나를 생성합니다.
        private Button CreateMenuButton(RectTransform parent, string label, int order, UnityEngine.Events.UnityAction onClick)
        {
            var buttonRoot = CreateRect(label + " Button", parent);
            var top = 1f - order * 0.25f;
            buttonRoot.anchorMin = new Vector2(0f, top - 0.19f);
            buttonRoot.anchorMax = new Vector2(1f, top - 0.03f);
            buttonRoot.offsetMin = Vector2.zero;
            buttonRoot.offsetMax = Vector2.zero;

            var image = CreateImage(buttonRoot.gameObject, buttonColor);
            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonColor * 1.18f;
            colors.pressedColor = buttonColor * 0.85f;
            colors.disabledColor = disabledButtonColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var buttonText = CreateText("Label", buttonRoot, label, 30, textColor, TextAnchor.MiddleCenter);
            StretchToParent(buttonText.rectTransform);
            return button;
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

        // 하위 UI 오브젝트를 모두 삭제합니다.
        private static void ClearChildren(Transform target)
        {
            for (var i = target.childCount - 1; i >= 0; i--)
                Destroy(target.GetChild(i).gameObject);
        }
    }
}
