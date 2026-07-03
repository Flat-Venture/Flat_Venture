using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 노드의 시각적 상태를 정의하는 열거형
/// </summary>
public enum NodeVisualState
{
    Locked,     //갈 수 없는 노드
    Attainable, //갈 수 있는 노드
    Visited,    //지나온 노드
    Current     //현재 플레이어 위치
}

/// <summary>
/// 개별 맵 노드의 시각적 랜더링과 클릭 이벤트 담당 컴포넌트
/// </summary>
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image nodeIcon;

    [SerializeField] private Image visitedMark;
    [SerializeField] private GameObject playerIcon;

    private int currntNodeID;

    //외부에서 ID를 확인할 수 있도록 프로퍼티 추가
    public int NodeID => currntNodeID;

    public Action<int> onNodeClicked;

    public void Init(MapNode nodeData, Sprite iconSprite)
    {
        currntNodeID = nodeData.NodeID;

        Debug.Log($"노드 {currntNodeID} 타입: {nodeData.RoomType}, 스프라이트 할당: {(iconSprite != null ? iconSprite.name : "NULL")}");

        if (iconSprite != null)
        {
            nodeIcon.sprite = iconSprite;
            Color color = nodeIcon.color;
            color.a = 1.0f;
            nodeIcon.color = color;
        }

        else Debug.LogWarning($"노드 {nodeData.NodeID}에 할당할 스프라이트가 없습니다.");

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

    /// <summary>
    /// 전달받은 상태에 따라 노드의 알파값, 상호작용, 마크 활성화 여부를 변경
    /// </summary>
    public void SetVisualState(NodeVisualState state, Sprite markSprite = null)
    {
        //알파값 0.4 또는 1.0 조절
        Color iconColor = nodeIcon.color;
        iconColor.a = (state == NodeVisualState.Locked) ? 0.4f : 1.0f;
        nodeIcon.color = iconColor;
        
        //갈 수 있는 노드만 버튼 클릭 활성화
        nodeButton.interactable = (state == NodeVisualState.Attainable);

        //O 표시 활성화 및 무작위 스프라이트 적용
        if (visitedMark != null)
        {
            bool isVisited = (state == NodeVisualState.Visited);
            visitedMark.gameObject.SetActive(isVisited);

            //방문 흔적일 때만 마크 스프라이트 적용 (현재 위치는 기본 마크 유지)
            if (isVisited && markSprite != null) visitedMark.sprite = markSprite;
        }

        //플레이어 아이콘 켜기/끄기
        if (playerIcon != null)
        {
            playerIcon.SetActive(state == NodeVisualState.Current);

            //현재 위치일 때 우측 하단 4~5시 방향(30, -30)으로 위치 조정
            if (state == NodeVisualState.Current)
            {
                RectTransform playerRect = playerIcon.GetComponent<RectTransform>();                
                playerRect.anchoredPosition = new Vector2(30f, -30f);
            }
        }
    }

    /// <summary>
    /// 보스 방인 경우 아이콘 크기를 확대
    /// </summary>
    public void SetBossScale(bool isBoss)
    {
        //보스 방이면 2.5배, 아니면 기본 크기 1배
        float scale = isBoss ? 2.5f : 1.0f;
        nodeIcon.transform.localScale = new Vector3(scale, scale, 1f);
    }
}