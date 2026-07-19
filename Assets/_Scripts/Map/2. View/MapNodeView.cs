using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 노드의 시각 상태를 정의하는 열거형입니다.
/// </summary>
public enum NodeVisualState
{
    Locked,     // 갈 수 없는 노드
    Attainable, // 갈 수 있는 노드
    Visited,    // 지나온 노드
    Current     // 현재 플레이어 위치
}

/// <summary>
/// 개별 맵 노드의 시각 렌더링과 클릭 이벤트를 담당하는 컴포넌트입니다.
/// </summary>
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image nodeIcon;

    [SerializeField] private Image visitedMark;
    [SerializeField] private GameObject playerIcon;

    private int currntNodeID;

    // 외부에서 ID를 확인할 수 있도록 공개합니다.
    public int NodeID => currntNodeID;

    public Action<int> onNodeClicked;

    public void Init(MapNode nodeData, Sprite iconSprite)
    {
        currntNodeID = nodeData.NodeID;

        Debug.Log($"노드 {currntNodeID} 타입 {nodeData.RoomType}, 스프라이트 할당: {(iconSprite != null ? iconSprite.name : "NULL")}");

        if (iconSprite != null)
        {
            nodeIcon.sprite = iconSprite;
            Color color = nodeIcon.color;
            color.a = 1.0f;
            nodeIcon.color = color;
        }
        else
        {
            Debug.LogWarning($"노드 {nodeData.NodeID}에 할당할 스프라이트가 없습니다.");
        }

        // 메모리 누수를 막기 위해 기존 리스너를 제거합니다.
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(() => onNodeClicked?.Invoke(currntNodeID));
    }

    /// <summary>
    /// Content의 실제 크기와 Normalized 좌표를 받아 RectTransform 위치를 설정합니다.
    /// </summary>
    public void SetPosition(RectTransform contentRect, float normalizedX, float normalizedY)
    {
        RectTransform rect = GetComponent<RectTransform>();

        // 앵커와 피벗을 중앙으로 고정합니다.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // 노드 크기를 고려한 안전 여백입니다.
        float paddingX = 120f;
        float paddingY = 120f;

        float width = contentRect.rect.width;
        float height = contentRect.rect.height;

        // Content 중앙을 기준으로 normalized 좌표를 실제 UI 좌표에 매핑합니다.
        float x = Mathf.Lerp(-width / 2f + paddingX, width / 2f - paddingX, normalizedX);
        float y = Mathf.Lerp(-height / 2f + paddingY, height / 2f - paddingY, normalizedY);

        rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>
    /// 전달받은 상태에 따라 노드의 투명도, 클릭 가능 여부, 방문 마크를 갱신합니다.
    /// </summary>
    public void SetVisualState(NodeVisualState state, Sprite markSprite = null)
    {
        // 잠긴 노드는 흐리게 표시합니다.
        Color iconColor = nodeIcon.color;
        iconColor.a = (state == NodeVisualState.Locked) ? 0.4f : 1.0f;
        nodeIcon.color = iconColor;

        // 갈 수 있는 노드만 버튼 클릭을 허용합니다.
        nodeButton.interactable = (state == NodeVisualState.Attainable);

        // 방문한 노드에는 마크를 표시합니다.
        if (visitedMark != null)
        {
            bool isVisited = (state == NodeVisualState.Visited);
            visitedMark.gameObject.SetActive(isVisited);

            // 방문 흔적일 때만 랜덤 마크 스프라이트를 적용합니다.
            if (isVisited && markSprite != null) visitedMark.sprite = markSprite;
        }

        // 플레이어 아이콘을 켜거나 끕니다.
        if (playerIcon != null)
        {
            playerIcon.SetActive(state == NodeVisualState.Current);

            // 현재 위치 아이콘은 노드의 우측 하단에 배치합니다.
            if (state == NodeVisualState.Current)
            {
                RectTransform playerRect = playerIcon.GetComponent<RectTransform>();
                playerRect.anchoredPosition = new Vector2(30f, -30f);
            }
        }
    }

    /// <summary>
    /// 보스 방인 경우 아이콘 크기를 키웁니다.
    /// </summary>
    public void SetBossScale(bool isBoss)
    {
        // 보스 방이면 2.5배, 아니면 기본 크기입니다.
        float scale = isBoss ? 2.5f : 1.0f;
        nodeIcon.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void SetClickEnabled(bool isEnabled)
    {
        if (nodeButton != null)
        {
            nodeButton.interactable = isEnabled;
        }
    }
}
