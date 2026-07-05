using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FlatVenture.SaveLoad
{
    // 세이브 슬롯 3개를 카드 형태의 버튼 UI로 보여주는 임시 타이틀 패널입니다.
    // 시작하기와 이어하기가 같은 슬롯 패널을 모드만 바꿔 재사용합니다.
    public sealed class SaveSlotSelectPanelBehaviour : MonoBehaviour
    {
        private const string NewGameTitleText = "\uC0C8 \uAC8C\uC784\uC744 \uC2DC\uC791\uD560 \uC138\uC774\uBE0C \uD504\uB85C\uD544\uC744 \uC120\uD0DD\uD558\uC138\uC694!";
        private const string ContinueTitleText = "\uD50C\uB808\uC774\uD560 \uC138\uC774\uBE0C \uD504\uB85C\uD544\uC744 \uC120\uD0DD\uD558\uC138\uC694!";
        private const string SlotTitlePrefix = "\uC138\uC774\uBE0C \uD504\uB85C\uD544 ";
        private const string EmptySlotText = "\uBE44\uC5B4\uC788\uC74C";
        private const string CorruptedSaveText = "\uC138\uC774\uBE0C \uC190\uC0C1";
        private const string PlayTimeText = "\uD50C\uB808\uC774 \uC2DC\uAC04";
        private const string UpdatedText = "\uC5C5\uB370\uC774\uD2B8\uB428";
        private const string InDungeonText = "\uB358\uC804 \uC9C4\uD589 \uC911 / ";
        private const string InTownText = "\uB9C8\uC744 / Lv.";
        private const string BackText = "\uB4A4\uB85C";
        private const string DeleteText = "\uC0AD\uC81C";
        private const string OverwriteTitleText = "\uC800\uC7A5 \uB370\uC774\uD130 \uB36E\uC5B4\uC4F0\uAE30";
        private const string OverwriteMessageText = "\uAE30\uC874 \uB370\uC774\uD130\uB97C \uC9C0\uC6B0\uACE0 \uC0C8\uB85C \uC2DC\uC791\uD558\uACA0\uC2B5\uB2C8\uAE4C?";
        private const string ConfirmYesText = "\uC608";
        private const string ConfirmNoText = "\uC544\uB2C8\uC624";

        public enum SlotPanelMode
        {
            NewGame = 0,
            Continue = 1
        }

        [SerializeField] private SlotPanelMode mode = SlotPanelMode.Continue;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Font font;
        [SerializeField] private Color backgroundColor = new Color(0.03f, 0.04f, 0.05f, 0f);
        [SerializeField] private Color cardColor = new Color(0.08f, 0.23f, 0.28f, 0.95f);
        [SerializeField] private Color cardHoverColor = new Color(0.12f, 0.31f, 0.37f, 0.98f);
        [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.24f, 1f);
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Color accentColor = new Color(0.46f, 0.82f, 0.92f, 1f);

        // TitleMenuBehaviour.CreateSlotPanel에서 HandleSaveLoaded를 연결합니다.
        // 이어하기 모드에서 세이브 슬롯을 클릭해 로드에 성공하면 HandleSlotClicked에서 Invoke합니다.
        public UnityEvent<SaveData> onSaveLoaded = new UnityEvent<SaveData>();

        // TitleMenuBehaviour.CreateSlotPanel에서 HandleNewGameCreated를 연결합니다.
        // 시작하기 모드에서 슬롯을 클릭해 새 세이브를 만든 뒤 HandleSlotClicked에서 Invoke합니다.
        public UnityEvent<SaveData> onNewGameCreated = new UnityEvent<SaveData>();

        // TitleMenuBehaviour.CreateSlotPanel에서 ShowMainMenu를 연결합니다.
        // 슬롯 화면의 뒤로가기 버튼을 누르면 HandleBackClicked에서 Invoke합니다.
        public UnityEvent onBack = new UnityEvent();

        private RectTransform panelRoot;
        private RectTransform overwriteConfirmRoot;
        private Text titleText;
        private SaveSlotSummary pendingOverwriteSummary;
        private readonly SaveSlotCardView[] slotCards = new SaveSlotCardView[SaveLoadService.SlotCount];

        // 외부에서 자동 UI 생성 여부를 설정합니다.
        public void SetBuildOnStart(bool value)
        {
            buildOnStart = value;
        }

        // 외부에서 생성 직후 패널을 미리 만들고 숨깁니다.
        public void PrepareHidden()
        {
            BuildIfNeeded();
            Close();
        }

        // 설정에 따라 슬롯 패널을 자동 생성합니다.
        private void Start()
        {
            if (buildOnStart)
                Build();
        }

        // 새 게임 슬롯 선택 모드로 패널을 엽니다.
        public void OpenForNewGame()
        {
            mode = SlotPanelMode.NewGame;
            BuildIfNeeded();
            panelRoot.gameObject.SetActive(true);
            CloseOverwriteConfirm();
            Refresh();
        }

        // 이어하기 슬롯 선택 모드로 패널을 엽니다.
        public void OpenForContinue()
        {
            mode = SlotPanelMode.Continue;
            BuildIfNeeded();
            panelRoot.gameObject.SetActive(true);
            CloseOverwriteConfirm();
            Refresh();
        }

        // 슬롯 패널을 닫습니다.
        public void Close()
        {
            CloseOverwriteConfirm();

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }

        // 슬롯 UI를 새로고침합니다.
        public void Refresh()
        {
            BuildIfNeeded();
            titleText.text = mode == SlotPanelMode.NewGame ? NewGameTitleText : ContinueTitleText;

            var summaries = SaveLoadService.GetAllSlotSummaries();
            for (var i = 0; i < slotCards.Length; i++)
                slotCards[i].Bind(summaries[i], mode);
        }

        // UI가 아직 없으면 생성합니다.
        private void BuildIfNeeded()
        {
            if (panelRoot == null)
                Build();
        }

        // 패널 전체 UI를 생성합니다.
        private void Build()
        {
            ClearChildren(transform);
            ResolveFont();

            var canvas = EnsureCanvas();
            EnsureCanvasScaler(canvas);
            EnsureGraphicRaycaster(canvas);
            EnsureEventSystem();

            panelRoot = CreateRect("Save Slot Panel", transform);
            StretchToParent(panelRoot);
            CreateImage(panelRoot.gameObject, backgroundColor);

            titleText = CreateText("Title", panelRoot, string.Empty, 34, titleColor, TextAnchor.MiddleCenter);
            titleText.rectTransform.anchorMin = new Vector2(0.1f, 0.78f);
            titleText.rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            var cardArea = CreateRect("Slot Cards", panelRoot);
            cardArea.anchorMin = new Vector2(0.12f, 0.2f);
            cardArea.anchorMax = new Vector2(0.88f, 0.72f);
            cardArea.offsetMin = Vector2.zero;
            cardArea.offsetMax = Vector2.zero;

            for (var i = 0; i < slotCards.Length; i++)
                slotCards[i] = CreateSlotCard(cardArea, i + 1);

            var backButton = CreateButton("Back Button", panelRoot, BackText, 34, new Color(0.72f, 0.18f, 0.12f, 1f), Color.white);
            backButton.rectTransform.anchorMin = new Vector2(0.02f, 0.04f);
            backButton.rectTransform.anchorMax = new Vector2(0.14f, 0.13f);
            backButton.rectTransform.offsetMin = Vector2.zero;
            backButton.rectTransform.offsetMax = Vector2.zero;
            backButton.button.onClick.AddListener(HandleBackClicked);

            CreateOverwriteConfirmPanel();
            Refresh();
        }

        // 슬롯 카드 하나를 생성합니다.
        private SaveSlotCardView CreateSlotCard(RectTransform parent, int slotIndex)
        {
            var spacing = 0.035f;
            var width = (1f - spacing * 2f) / 3f;
            var minX = (slotIndex - 1) * (width + spacing);

            var cardRoot = CreateRect("Slot " + slotIndex, parent);
            cardRoot.anchorMin = new Vector2(minX, 0.12f);
            cardRoot.anchorMax = new Vector2(minX + width, 1f);
            cardRoot.offsetMin = Vector2.zero;
            cardRoot.offsetMax = Vector2.zero;

            var cardButton = cardRoot.gameObject.AddComponent<Button>();
            var cardImage = CreateImage(cardRoot.gameObject, cardColor);
            var colors = cardButton.colors;
            colors.normalColor = cardColor;
            colors.highlightedColor = cardHoverColor;
            colors.pressedColor = cardHoverColor * 0.85f;
            cardButton.colors = colors;
            cardButton.targetGraphic = cardImage;

            var title = CreateText("Slot Title", cardRoot, string.Empty, 28, titleColor, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.76f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.93f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var body = CreateText("Slot Body", cardRoot, string.Empty, 22, bodyColor, TextAnchor.MiddleCenter);
            body.rectTransform.anchorMin = new Vector2(0.08f, 0.24f);
            body.rectTransform.anchorMax = new Vector2(0.92f, 0.72f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;

            var state = CreateText("Slot State", cardRoot, string.Empty, 20, accentColor, TextAnchor.MiddleCenter);
            state.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
            state.rectTransform.anchorMax = new Vector2(0.92f, 0.22f);
            state.rectTransform.offsetMin = Vector2.zero;
            state.rectTransform.offsetMax = Vector2.zero;

            var deleteButton = CreateButton("Delete Button", cardRoot, DeleteText, 20, new Color(0.73f, 0.18f, 0.14f, 1f), Color.white);
            deleteButton.rectTransform.anchorMin = new Vector2(0.36f, -0.16f);
            deleteButton.rectTransform.anchorMax = new Vector2(0.64f, -0.04f);
            deleteButton.rectTransform.offsetMin = Vector2.zero;
            deleteButton.rectTransform.offsetMax = Vector2.zero;

            return new SaveSlotCardView(slotIndex, cardButton, title, body, state, deleteButton.button, this);
        }

        // 기존 세이브를 새 게임으로 덮어쓰기 전에 보여줄 확인 패널을 생성합니다.
        private void CreateOverwriteConfirmPanel()
        {
            overwriteConfirmRoot = CreateRect("Overwrite Confirm Overlay", panelRoot);
            StretchToParent(overwriteConfirmRoot);

            var blocker = CreateImage(overwriteConfirmRoot.gameObject, new Color(0f, 0f, 0f, 0.58f));
            blocker.raycastTarget = true;

            var dialog = CreateRect("Overwrite Confirm Dialog", overwriteConfirmRoot);
            dialog.anchorMin = new Vector2(0.32f, 0.34f);
            dialog.anchorMax = new Vector2(0.68f, 0.66f);
            dialog.offsetMin = Vector2.zero;
            dialog.offsetMax = Vector2.zero;
            CreateImage(dialog.gameObject, new Color(0.07f, 0.17f, 0.2f, 0.98f));

            var title = CreateText("Title", dialog, OverwriteTitleText, 30, titleColor, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.68f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var message = CreateText("Message", dialog, OverwriteMessageText, 22, bodyColor, TextAnchor.MiddleCenter);
            message.rectTransform.anchorMin = new Vector2(0.1f, 0.42f);
            message.rectTransform.anchorMax = new Vector2(0.9f, 0.66f);
            message.rectTransform.offsetMin = Vector2.zero;
            message.rectTransform.offsetMax = Vector2.zero;

            var noButton = CreateButton("No Button", dialog, ConfirmNoText, 24, new Color(0.18f, 0.34f, 0.38f, 1f), Color.white);
            noButton.rectTransform.anchorMin = new Vector2(0.16f, 0.14f);
            noButton.rectTransform.anchorMax = new Vector2(0.44f, 0.32f);
            noButton.rectTransform.offsetMin = Vector2.zero;
            noButton.rectTransform.offsetMax = Vector2.zero;
            noButton.button.onClick.AddListener(CloseOverwriteConfirm);

            var yesButton = CreateButton("Yes Button", dialog, ConfirmYesText, 24, new Color(0.72f, 0.18f, 0.12f, 1f), Color.white);
            yesButton.rectTransform.anchorMin = new Vector2(0.56f, 0.14f);
            yesButton.rectTransform.anchorMax = new Vector2(0.84f, 0.32f);
            yesButton.rectTransform.offsetMin = Vector2.zero;
            yesButton.rectTransform.offsetMax = Vector2.zero;
            yesButton.button.onClick.AddListener(HandleOverwriteConfirmed);

            CloseOverwriteConfirm();
        }

        // 슬롯 카드를 클릭했을 때 현재 모드에 맞게 새 게임 생성 또는 이어하기 로드를 처리합니다.
        private void HandleSlotClicked(SaveSlotSummary summary)
        {
            if (mode == SlotPanelMode.NewGame)
            {
                if (summary.exists)
                {
                    OpenOverwriteConfirm(summary);
                    return;
                }

                CreateNewGame(summary.slotIndex);
                return;
            }

            SaveData loaded;
            if (!SaveLoadService.TryLoad(summary.slotIndex, out loaded))
            {
                Debug.LogWarning("[SaveSlotSelectPanel] Empty slot. Slot " + summary.slotIndex);
                return;
            }

            Debug.Log("[SaveSlotSelectPanel] Save loaded. Slot " + summary.slotIndex);
            onSaveLoaded?.Invoke(loaded);
        }

        // 새 게임 세이브를 생성하고 해당 슬롯으로 게임을 시작합니다.
        private void CreateNewGame(int slotIndex)
        {
            var saveData = SaveLoadService.CreateNewSave(slotIndex, SlotTitlePrefix + slotIndex);
            SaveLoadService.Save(slotIndex, saveData);
            Debug.Log("[SaveSlotSelectPanel] New game created. Slot " + slotIndex);
            onNewGameCreated?.Invoke(saveData);
            Refresh();
        }

        // 기존 세이브가 있는 슬롯을 새 게임으로 덮어쓸지 묻는 확인창을 엽니다.
        private void OpenOverwriteConfirm(SaveSlotSummary summary)
        {
            pendingOverwriteSummary = summary;

            if (overwriteConfirmRoot != null)
                overwriteConfirmRoot.gameObject.SetActive(true);
        }

        // 덮어쓰기 확인창을 닫고 선택 대기 상태를 비웁니다.
        private void CloseOverwriteConfirm()
        {
            pendingOverwriteSummary = null;

            if (overwriteConfirmRoot != null)
                overwriteConfirmRoot.gameObject.SetActive(false);
        }

        // 예 버튼에서 연결됩니다. 기존 세이브를 새 세이브로 덮어쓰고 게임을 시작합니다.
        private void HandleOverwriteConfirmed()
        {
            if (pendingOverwriteSummary == null)
            {
                CloseOverwriteConfirm();
                return;
            }

            var slotIndex = pendingOverwriteSummary.slotIndex;
            CloseOverwriteConfirm();
            CreateNewGame(slotIndex);
        }

        // 삭제 버튼 클릭을 처리합니다.
        private void HandleDeleteClicked(int slotIndex)
        {
            if (SaveLoadService.Delete(slotIndex))
                Debug.Log("[SaveSlotSelectPanel] Save deleted. Slot " + slotIndex);

            Refresh();
        }

        // 뒤로가기 버튼 클릭을 처리합니다.
        private void HandleBackClicked()
        {
            onBack?.Invoke();
            Close();
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

        // 부모 Canvas가 있으면 재사용하고 없으면 현재 오브젝트에 생성합니다.
        private Canvas EnsureCanvas()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
                return parentCanvas;

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

        // 부모 영역 전체를 채우도록 RectTransform을 설정합니다.
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

        // Button과 Text가 포함된 버튼 UI를 생성합니다.
        private ButtonView CreateButton(string objectName, RectTransform parent, string label, int fontSize, Color background, Color textColor)
        {
            var rect = CreateRect(objectName, parent);
            var image = CreateImage(rect.gameObject, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText("Label", rect, label, fontSize, textColor, TextAnchor.MiddleCenter);
            StretchToParent(labelText.rectTransform);

            return new ButtonView(rect, button);
        }

        // 하위 UI 오브젝트를 모두 삭제합니다.
        private static void ClearChildren(Transform target)
        {
            for (var i = target.childCount - 1; i >= 0; i--)
                Destroy(target.GetChild(i).gameObject);
        }

        // 버튼 RectTransform과 Button 컴포넌트를 함께 넘기기 위한 작은 구조체입니다.
        private readonly struct ButtonView
        {
            public readonly RectTransform rectTransform;
            public readonly Button button;

            public ButtonView(RectTransform rectTransform, Button button)
            {
                this.rectTransform = rectTransform;
                this.button = button;
            }
        }

        // 슬롯 카드 하나의 표시와 버튼 이벤트를 관리합니다.
        private sealed class SaveSlotCardView
        {
            private readonly int slotIndex;
            private readonly Button cardButton;
            private readonly Text title;
            private readonly Text body;
            private readonly Text state;
            private readonly Button deleteButton;
            private readonly SaveSlotSelectPanelBehaviour owner;
            private SaveSlotSummary currentSummary;

            public SaveSlotCardView(int slotIndex, Button cardButton, Text title, Text body, Text state, Button deleteButton, SaveSlotSelectPanelBehaviour owner)
            {
                this.slotIndex = slotIndex;
                this.cardButton = cardButton;
                this.title = title;
                this.body = body;
                this.state = state;
                this.deleteButton = deleteButton;
                this.owner = owner;

                cardButton.onClick.AddListener(HandleCardClicked);
                deleteButton.onClick.AddListener(HandleDeleteClicked);
            }

            // 슬롯 요약 정보를 UI에 반영합니다.
            public void Bind(SaveSlotSummary summary, SlotPanelMode mode)
            {
                currentSummary = summary;
                title.text = SlotTitlePrefix + slotIndex;
                body.text = BuildBodyText(summary);
                state.text = BuildStateText(summary);
                deleteButton.gameObject.SetActive(summary.exists && !summary.isCorrupted);
                cardButton.interactable = mode == SlotPanelMode.NewGame || (summary.exists && !summary.isCorrupted);
            }

            // 카드 버튼 클릭을 소유 패널로 전달합니다.
            private void HandleCardClicked()
            {
                owner.HandleSlotClicked(currentSummary);
            }

            // 삭제 버튼 클릭을 소유 패널로 전달합니다.
            private void HandleDeleteClicked()
            {
                owner.HandleDeleteClicked(slotIndex);
            }

            // 슬롯 카드 본문 문자열을 만듭니다.
            private static string BuildBodyText(SaveSlotSummary summary)
            {
                if (!summary.exists)
                    return EmptySlotText;

                if (summary.isCorrupted)
                    return CorruptedSaveText + "\n" + summary.errorMessage;

                return PlayTimeText + "\n"
                    + FormatPlayTime(summary.playTimeSeconds)
                    + "\n\n" + UpdatedText + "\n"
                    + FormatUpdatedAt(summary.updatedAt);
            }

            // 슬롯 하단 상태 문자열을 만듭니다.
            private static string BuildStateText(SaveSlotSummary summary)
            {
                if (!summary.exists || summary.isCorrupted)
                    return string.Empty;

                return summary.isInDungeon
                    ? InDungeonText + summary.dungeonState
                    : InTownText + summary.userLevel;
            }

            // 플레이 시간을 HH:MM:SS 형태로 만듭니다.
            private static string FormatPlayTime(double seconds)
            {
                var timeSpan = TimeSpan.FromSeconds(Math.Max(0d, seconds));
                var totalHours = (int)timeSpan.TotalHours;
                return totalHours.ToString("00", CultureInfo.InvariantCulture)
                    + ":" + timeSpan.Minutes.ToString("00", CultureInfo.InvariantCulture)
                    + ":" + timeSpan.Seconds.ToString("00", CultureInfo.InvariantCulture);
            }

            // 저장 시각을 로컬 시간 문자열로 만듭니다.
            private static string FormatUpdatedAt(string updatedAt)
            {
                DateTime dateTime;
                if (!DateTime.TryParse(updatedAt, null, DateTimeStyles.RoundtripKind, out dateTime))
                    return updatedAt;

                return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
        }
    }
}
