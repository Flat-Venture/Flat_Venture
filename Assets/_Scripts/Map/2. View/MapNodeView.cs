using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 개별 맵 노드의 시각적 랜더링과 클릭 이벤트 담당 컴포넌트
/// </summary>
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image nodeIcon;

    private int currntNodeID;

    public Action<int> onNodeClicked;

    public void Init(MapNode nodeData, Sprite iconSprite)
    {
        currntNodeID = nodeData.NodeID;

        if (iconSprite != null) nodeIcon.sprite = iconSprite;

        //메모리 누수 방지
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(() => onNodeClicked?.Invoke(currntNodeID));
    }

    /// <summary>
    /// Content의 실제 픽셀 크기와 Normalized 좌표를 받아서 RectTransform의 위치를 설정
    /// </summary>
    public void SetPosition(RectTransform contentRect, float normalizedX, float normalizedY)
    {
        RectTransform rect = GetComponent<RectTransform>();

        //앵커와 피벗을 정중앙(0.5, 0.5)으로 고정
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        //노드 크기(100)를 고려한 안전 여백
        float paddingX = 120f;
        float paddingY = 120f;

        float width = contentRect.rect.width;
        float height = contentRect.rect.height;

        //Content의 정중앙(0,0)을 기준으로 -절반 ~ +절반 범위 내에서 좌표를 매핑
        float x = Mathf.Lerp(-width / 2f + paddingX, width / 2f - paddingX, normalizedX);
        float y = Mathf.Lerp(-height / 2f + paddingY, height / 2f - paddingY, normalizedY);

        rect.anchoredPosition = new Vector2(x, y);
    }
}
