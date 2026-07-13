using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FlatVenture.NUH.Seed;

namespace FlatVenture.SaveLoad
{
    // 마을 씬에서 던전 진입을 테스트하기 위한 임시 버튼입니다.
    // 실제 마을 UI가 생기면 이 스크립트는 제거하고 던전 입장 버튼에서 EnterDungeon을 호출하면 됩니다.
    public sealed class TownDungeonEnterButtonBehaviour : MonoBehaviour
    {
        [SerializeField] private string dungeonSceneName = "Test_LRH_Dungeon";
        [SerializeField] private Font font;
        [SerializeField] private Color buttonColor = new Color(0.18f, 0.34f, 0.38f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector2 anchorMin = new Vector2(0.78f, 0.08f);
        [SerializeField] private Vector2 anchorMax = new Vector2(0.96f, 0.18f);

        private Text statusText;

        // 임시 던전 입장 버튼 UI를 생성합니다.
        private void Start()
        {
            Build();
        }

        // 현재 세이브를 던전 진입 상태로 저장하고 던전 씬으로 이동합니다.
        public void EnterDungeon()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                SetStatus("활성 세이브가 없어 던전에 입장할 수 없습니다.");
                Debug.LogWarning("[TownDungeonEnter] 활성 세이브가 없습니다.");
                return;
            }

            var saveData = SaveGameSession.CurrentSaveData;
            DungeonRunSaveUtility.StartDungeon(saveData, SeedValue.Generate());

            SaveLoadService.Save(SaveGameSession.CurrentSlotIndex, saveData);
            Debug.Log("[TownDungeonEnter] 던전 입장 저장 완료. Seed: " + saveData.dungeon.dungeonSeed);

            SceneManager.LoadScene(dungeonSceneName);
        }

        // 버튼 UI를 생성합니다.
        private void Build()
        {
            ResolveFont();

            var canvas = EnsureCanvas();
            EnsureCanvasScaler(canvas);
            EnsureGraphicRaycaster(canvas);
            EnsureEventSystem();

            var root = CreateRect("Town Dungeon Enter Root", transform);
            StretchToParent(root);

            var buttonRoot = CreateRect("Enter Dungeon Button", root);
            buttonRoot.anchorMin = anchorMin;
            buttonRoot.anchorMax = anchorMax;
            buttonRoot.offsetMin = Vector2.zero;
            buttonRoot.offsetMax = Vector2.zero;

            var image = CreateImage(buttonRoot.gameObject, buttonColor);
            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(EnterDungeon);

            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonColor * 1.15f;
            colors.pressedColor = buttonColor * 0.85f;
            button.colors = colors;

            var label = CreateText("Label", buttonRoot, "던전 입장", 24, textColor, TextAnchor.MiddleCenter);
            StretchToParent(label.rectTransform);

            statusText = CreateText("Status", root, string.Empty, 18, textColor, TextAnchor.MiddleRight);
            statusText.rectTransform.anchorMin = new Vector2(0.52f, 0.02f);
            statusText.rectTransform.anchorMax = new Vector2(0.96f, 0.07f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
        }

        // 상태 문구를 표시합니다.
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

        // 부모 Canvas가 있으면 재사용하고, 없으면 현재 오브젝트에 생성합니다.
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
