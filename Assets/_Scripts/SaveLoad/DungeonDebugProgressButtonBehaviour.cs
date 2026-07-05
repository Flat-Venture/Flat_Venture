using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlatVenture.SaveLoad
{
    // 던전 진행 저장 테스트를 위한 임시 UI입니다.
    // 정식 던전 맵, 전투, 사망 통계 화면이 붙으면 개발자 전용 패널로 옮기거나 제거하면 됩니다.
    public sealed class DungeonDebugProgressButtonBehaviour : MonoBehaviour
    {
        private const string NoDungeonSaveMessage = "\uC9C4\uD589 \uC911\uC778 \uB358\uC804 \uC138\uC774\uBE0C\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
        private const string NoSaveMessage = "\uC9C4\uD589 \uC911\uC778 \uC138\uC774\uBE0C\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
        private const string NoDungeonSessionMessage = "\uB358\uC804 \uC138\uC158 \uC5C6\uC74C";
        private const string FloorSuffix = "\uCE35";
        private const string SaveLaterSuffix = "\uCE35 / \uB2E4\uC74C \uC800\uC7A5 \uC2DC \uBC18\uC601";
        private const string IncreaseFloorLabel = "\uCE35 +1";
        private const string DeathLabel = "\uC0AC\uB9DD";
        private const string DeathLogMessage = "[DungeonDebugProgress] \uD50C\uB808\uC774\uC5B4 \uC0AC\uB9DD \uCC98\uB9AC \uC800\uC7A5 \uC644\uB8CC. \uB9C8\uC744\uB85C \uC774\uB3D9\uD569\uB2C8\uB2E4.";

        [SerializeField] private Font font;
        [SerializeField] private string townSceneName = "Test_LRH_Town";
        [SerializeField] private Color buttonColor = new Color(0.18f, 0.34f, 0.38f, 1f);
        [SerializeField] private Color dangerButtonColor = new Color(0.65f, 0.16f, 0.12f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector2 anchorMin = new Vector2(0.78f, 0.18f);
        [SerializeField] private Vector2 anchorMax = new Vector2(0.96f, 0.34f);

        private Text statusText;

        // 임시 던전 진행 버튼 UI를 생성합니다.
        private void Start()
        {
            Build();
            RefreshStatus();
        }

        // 버튼에서 연결됩니다. 현재 던전 층을 1 올리고 저장 파일은 아직 쓰지 않습니다.
        public void OnIncreaseFloorClicked()
        {
            if (!DungeonRunSaveUtility.TryIncreaseFloor(1, out var currentFloor))
            {
                SetStatus(NoDungeonSaveMessage);
                return;
            }

            SetStatus(currentFloor + SaveLaterSuffix);
        }

        // 버튼에서 연결됩니다. 사망 처리 후 던전 진행을 정리하고 저장한 뒤 마을로 돌아갑니다.
        public void OnPlayerDeathClicked()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                SetStatus(NoSaveMessage);
                return;
            }

            DungeonRunSaveUtility.ClearDungeonProgress(SaveGameSession.CurrentSaveData);
            SaveLoadService.Save(SaveGameSession.CurrentSlotIndex, SaveGameSession.CurrentSaveData);
            Debug.Log(DeathLogMessage);
            SceneManager.LoadScene(townSceneName);
        }

        // 현재 세션의 던전 층 표시를 갱신합니다.
        private void RefreshStatus()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null || SaveGameSession.CurrentSaveData.dungeon == null)
            {
                SetStatus(NoDungeonSessionMessage);
                return;
            }

            SetStatus(SaveGameSession.CurrentSaveData.dungeon.currentFloor + FloorSuffix);
        }

        // 임시 버튼 UI를 생성합니다.
        private void Build()
        {
            ResolveFont();

            var canvas = EnsureCanvas();
            EnsureCanvasScaler(canvas);
            EnsureGraphicRaycaster(canvas);
            EnsureEventSystem();

            var root = CreateRect("Dungeon Debug Progress Root", transform);
            StretchToParent(root);

            var gap = 0.01f;
            var buttonHeight = ((anchorMax.y - anchorMin.y) - gap) * 0.5f;
            CreateButton(root, "Increase Floor Button", IncreaseFloorLabel, new Vector2(anchorMin.x, anchorMax.y - buttonHeight), anchorMax, buttonColor, OnIncreaseFloorClicked);
            CreateButton(root, "Player Death Button", DeathLabel, anchorMin, new Vector2(anchorMax.x, anchorMin.y + buttonHeight), dangerButtonColor, OnPlayerDeathClicked);

            statusText = CreateText("Status", root, string.Empty, 18, textColor, TextAnchor.MiddleRight);
            statusText.rectTransform.anchorMin = new Vector2(anchorMin.x - 0.22f, anchorMin.y);
            statusText.rectTransform.anchorMax = new Vector2(anchorMin.x - 0.02f, anchorMax.y);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
        }

        // 버튼 하나를 생성하고 클릭 이벤트를 연결합니다.
        private void CreateButton(RectTransform parent, string objectName, string labelText, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonRoot = CreateRect(objectName, parent);
            buttonRoot.anchorMin = min;
            buttonRoot.anchorMax = max;
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

            var label = CreateText("Label", buttonRoot, labelText, 24, textColor, TextAnchor.MiddleCenter);
            StretchToParent(label.rectTransform);
        }

        // 상태 문구를 표시합니다.
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        // 사용할 기본 폰트를 찾습니다.
        private void ResolveFont()
        {
            if (font != null)
                return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // 부모 Canvas가 있으면 재사용하고, 없으면 현재 오브젝트에 Canvas를 추가합니다.
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

        // 해상도 변화에 맞춰 UI 크기가 유지되도록 CanvasScaler를 보장합니다.
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
    }
}
