using FlatVenture.SaveLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 던전 방 안에서 현재 던전 지도를 읽기 전용으로 확인하는 임시 버튼입니다.
public sealed class DungeonMapPreviewButtonBehaviour : MonoBehaviour
{
    private const int ButtonSortingOrder = 30000;

    [SerializeField] private MapTestRunner mapTestRunner;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Font font;
    [SerializeField] private Color buttonColor = new Color(0.16f, 0.28f, 0.36f, 0.95f);
    [SerializeField] private Color closeButtonColor = new Color(0.52f, 0.15f, 0.12f, 0.95f);
    [SerializeField] private Color textColor = Color.white;

    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Button button;
    private Text label;
    private bool isPreviewOpen;

    // 시작 시 필요한 참조를 찾고 임시 지도 버튼 UI를 구성합니다.
    private void Start()
    {
        ResolveReferences();
        Build();
        SetPreviewOpen(false);
    }

    // 매 프레임 지도 미리보기 버튼의 표시 가능 상태를 갱신합니다.
    private void Update()
    {
        RefreshVisibleState();
    }

    // 오브젝트가 제거될 때 지도 미리보기 입력 차단 상태를 해제합니다.
    private void OnDestroy()
    {
        DungeonUiInputBlocker.SetBlocked(this, false);
    }

    // 지도 미리보기에 필요한 MapTestRunner, StageManager, 폰트를 찾습니다.
    private void ResolveReferences()
    {
        if (mapTestRunner == null)
        {
            mapTestRunner = FindFirstObjectByType<MapTestRunner>();
        }

        if (stageManager == null)
        {
            stageManager = FindFirstObjectByType<StageManager>();
        }

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }
    }

    // 화면 위에 사용할 지도 미리보기 버튼 UI를 생성합니다.
    private void Build()
    {
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = ButtonSortingOrder;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        var rect = new GameObject("Map Preview Button", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.86f, 0.91f);
        rect.anchorMax = new Vector2(0.98f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = rect.gameObject.AddComponent<Image>();
        button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(TogglePreview);

        label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.font = font;
        label.fontSize = 22;
        label.color = textColor;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
    }

    // 지도 미리보기 버튼을 눌렀을 때 지도 열기/닫기를 전환합니다.
    private void TogglePreview()
    {
        if (!CanShowPreviewButton())
        {
            SetPreviewOpen(false);
            return;
        }

        SetPreviewOpen(!isPreviewOpen);
    }

    // ESC 메뉴처럼 더 높은 우선순위 UI가 열릴 때 지도 미리보기를 닫습니다.
    public void ClosePreview()
    {
        SetPreviewOpen(false);
    }

    // 지도 미리보기 열림/닫힘 상태를 적용하고 버튼 표시를 갱신합니다.
    private void SetPreviewOpen(bool nextOpen)
    {
        isPreviewOpen = nextOpen;
        EnsureButtonIsOnTop();

        if (mapTestRunner != null)
        {
            if (isPreviewOpen)
            {
                // 읽기 전용 지도에서는 노드 선택을 비활성화하고 스크롤/닫기만 허용합니다.
                isPreviewOpen = mapTestRunner.OpenReadOnlyMap();
            }
            else
            {
                mapTestRunner.CloseReadOnlyMap();
            }
        }
        else if (isPreviewOpen)
        {
            isPreviewOpen = false;
        }

        // 지도 미리보기가 열린 동안 플레이어 이동과 다른 상호작용 입력을 막습니다.
        DungeonUiInputBlocker.SetBlocked(this, isPreviewOpen);

        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = isPreviewOpen ? closeButtonColor : buttonColor;
        }

        if (label != null)
        {
            label.text = isPreviewOpen ? "지도 닫기" : "지도";
        }

        RefreshVisibleState();
    }

    // 현재 게임 흐름에 따라 지도 버튼 표시와 클릭 가능 상태를 갱신합니다.
    private void RefreshVisibleState()
    {
        bool shouldShow = CanShowPreviewButton();
        if (!shouldShow && isPreviewOpen)
        {
            SetPreviewOpen(false);
            return;
        }

        if (canvas != null)
        {
            canvas.enabled = shouldShow;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ButtonSortingOrder;
        }

        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = shouldShow;
        }

        if (button != null)
        {
            button.interactable = shouldShow;
        }
    }

    // 지도 버튼 캔버스를 다른 일반 UI보다 앞에 보이도록 정렬합니다.
    private void EnsureButtonIsOnTop()
    {
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = ButtonSortingOrder;
        }

        transform.SetAsLastSibling();
    }

    // 현재 상황에서 지도 미리보기 버튼을 보여줘도 되는지 판단합니다.
    private bool CanShowPreviewButton()
    {
        ResolveReferences();
        return stageManager != null
            && stageManager.IsInActiveRoom
            && (!DungeonUiInputBlocker.BlocksGameplayInput || isPreviewOpen)
            && !PauseMenuBehaviour.IsAnyOpen;
    }

    // UI 버튼 클릭을 위해 EventSystem이 없으면 임시로 생성합니다.
    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }
}
