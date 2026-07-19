using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FlatVenture.Enums;

[Serializable]
public struct RoomIconData
{
    public RoomType roomType;
    public Sprite iconSprite;
}

/// <summary>
/// 인게임 화면에서 지도의 표시 상태와 노드 상호작용 상태를 관리합니다.
/// </summary>
public class MapUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("지도를 구성하는 전체 UI의 CanvasGroup")]
    [SerializeField] private CanvasGroup mapCanvasGroup;
    [SerializeField] private ScrollRect mapScrollRect;

    [Header("Rendering References")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private GameObject linePrefab;             // 단순한 흰색 Image 컴포넌트가 있는 프리팹
    [SerializeField] private MapNodeView nodePrefab;

    [Header("Visual Resources")]
    [SerializeField] private List<Sprite> visitedMarkSprites;   // 사용할 랜덤 O 표시 스프라이트 목록
    [SerializeField] private List<RoomIconData> roomIconList;   // 방 종류별 아이콘 리스트

    // View의 순수 UI 상태 변수입니다.
    public bool isMapOpendByTab = false;

    private class LineConnection
    {
        public Image lineImage;
        public int startNodeID;
        public int endNodeID;
    }

    // 생성된 UI 추적용입니다. 이후 Object Pooling을 적용할 때 재사용할 수 있습니다.
    private List<LineConnection> lineConnections = new List<LineConnection>();
    private List<MapNodeView> spawnedNodes = new List<MapNodeView>();

    // 방 타입별 아이콘 검색용 딕셔너리입니다.
    private Dictionary<RoomType, Sprite> roomIconDict = new Dictionary<RoomType, Sprite>();
    private Coroutine scrollCoroutine;

    private void Awake()
    {
        // 방 타입별 아이콘 딕셔너리를 초기화합니다.
        foreach (var data in roomIconList)
        {
            if (!roomIconDict.ContainsKey(data.roomType))
            {
                roomIconDict.Add(data.roomType, data.iconSprite);
            }
        }

        // 초기 상태에서는 지도 UI를 숨깁니다.
        HideMap();
    }

    /// <summary>
    /// 방 종류에 맞는 스프라이트를 리스트에서 찾아 반환합니다.
    /// </summary>
    private Sprite GetRoomIcon(RoomType type)
    {
        if (roomIconDict.TryGetValue(type, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[MapUIManager] {type} 타입에 맞는 아이콘을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// Model의 데이터를 받아 화면에 노드와 선을 인스턴스화합니다.
    /// </summary>
    public void DrawMap(List<List<MapNode>> mapData, System.Action<int> onNodeClickCallback)
    {
        ClearMap();

        // 선을 그릴 때 노드 위치를 빠르게 찾기 위한 캐시 딕셔너리입니다.
        Dictionary<int, MapNodeView> nodeViewDict = new Dictionary<int, MapNodeView>();

        // 노드를 배치합니다.
        for (int floor = 0; floor < mapData.Count; floor++)
        {
            for (int i = 0; i < mapData[floor].Count; i++)
            {
                MapNode node = mapData[floor][i];
                MapNodeView nodeView = Instantiate(nodePrefab, nodeContainer);

                // RoomType에 맞는 고유 스프라이트를 가져와서 Init에 전달합니다.
                Sprite roomSprite = GetRoomIcon(node.RoomType);
                nodeView.Init(node, roomSprite);

                // 보스 방 아이콘 크기를 조절합니다.
                nodeView.SetBossScale(node.RoomType == RoomType.Boss);

                nodeView.SetPosition(contentRect, node.NormalizedX, node.NormalizedY);
                nodeView.onNodeClicked = onNodeClickCallback;

                spawnedNodes.Add(nodeView);
                nodeViewDict.Add(node.NodeID, nodeView);
            }
        }

        // 노드 사이의 선을 연결합니다.
        for (int floor = 0; floor < mapData.Count; floor++)
        {
            for (int i = 0; i < mapData[floor].Count; i++)
            {
                MapNode node = mapData[floor][i];
                RectTransform startRect = nodeViewDict[node.NodeID].GetComponent<RectTransform>();

                for (int j = 0; j < node.NextNodes.Count; j++)
                {
                    MapNode nextNode = node.NextNodes[j];
                    RectTransform endRect = nodeViewDict[nextNode.NodeID].GetComponent<RectTransform>();

                    DrawLine(startRect, endRect, node.NodeID, nextNode.NodeID);
                }
            }
        }

        // 맵 렌더링이 끝난 직후 스크롤을 맨 아래로 이동합니다.
        if (mapScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            mapScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 두 UI 좌표 사이의 거리와 각도를 계산해 선을 연결합니다.
    /// </summary>
    private void DrawLine(RectTransform startRect, RectTransform endRect, int startID, int endID)
    {
        Vector2 startPos = startRect.anchoredPosition;
        Vector2 endPos = endRect.anchoredPosition;

        GameObject lineObject = Instantiate(linePrefab, lineContainer);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();

        // 선이 노드 중앙에 어긋나지 않도록 앵커와 피벗을 중앙으로 고정합니다.
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float startRadius = startRect.rect.width / 2f;
        float endRadius = endRect.rect.width / 2f;
        float gapOffset = 10f;

        float padding = startRadius + endRadius + gapOffset;
        float finalLineLength = Mathf.Max(0, distance - padding);

        lineRect.sizeDelta = new Vector2(finalLineLength, 5f);
        lineRect.anchoredPosition = startPos + direction / 2;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        LineConnection connectrion = new LineConnection
        {
            lineImage = lineObject.GetComponent<Image>(),
            startNodeID = startID,
            endNodeID = endID
        };

        lineConnections.Add(connectrion);
    }

    public void ClearMap()
    {
        for (int i = 0; i < spawnedNodes.Count; i++) Destroy(spawnedNodes[i].gameObject);
        for (int i = 0; i < lineConnections.Count; i++) Destroy(lineConnections[i].lineImage.gameObject);

        spawnedNodes.Clear();
        lineConnections.Clear();
    }

    /// <summary>
    /// 지도 UI를 표시하고 노드 클릭 가능 여부를 설정합니다.
    /// </summary>
    public void ShowMap(bool isInteractable)
    {
        if (mapCanvasGroup == null)
        {
            return;
        }

        EnsureMapHierarchyActive();

        mapCanvasGroup.alpha = 1.0f;
        mapCanvasGroup.blocksRaycasts = true;
        mapCanvasGroup.interactable = true;
        mapCanvasGroup.enabled = true;
    }

    /// <summary>
    /// 방 안에서 지도를 확인할 때 사용하는 읽기 전용 지도 모드입니다.
    /// </summary>
    public void ShowMapPreview()
    {
        if (mapCanvasGroup == null)
        {
            return;
        }

        EnsureMapHierarchyActive();

        mapCanvasGroup.alpha = 1.0f;
        mapCanvasGroup.blocksRaycasts = true;
        mapCanvasGroup.interactable = true;
        mapCanvasGroup.enabled = true;
        SetNodeClickEnabled(false);
    }

    public void HideMap()
    {
        if (mapCanvasGroup == null)
        {
            return;
        }

        mapCanvasGroup.alpha = 0.0f;
        mapCanvasGroup.blocksRaycasts = false;
        mapCanvasGroup.interactable = false;
        mapCanvasGroup.enabled = false;
    }

    // StageManager가 지도 Canvas를 꺼둔 상태에서도 읽기 전용 지도를 다시 표시할 수 있게 부모 Canvas까지 활성화합니다.
    private void EnsureMapHierarchyActive()
    {
        if (mapCanvasGroup == null)
        {
            return;
        }

        Transform current = mapCanvasGroup.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            if (current.GetComponent<Canvas>() != null)
            {
                break;
            }

            current = current.parent;
        }
    }

    // 배치된 모든 노드의 클릭 가능 여부를 일괄 변경합니다.
    private void SetNodeClickEnabled(bool isEnabled)
    {
        for (int i = 0; i < spawnedNodes.Count; i++)
        {
            spawnedNodes[i].SetClickEnabled(isEnabled);
        }
    }

    /// <summary>
    /// 맵에 배치된 모든 노드와 선의 시각적 상태를 업데이트합니다.
    /// </summary>
    public void UpdateNodeVisuals(int currentNodeID, List<int> visitedNodeIDs, List<MapNode> nextNodes)
    {
        // 빠른 탐색을 위해 갈 수 있는 다음 노드들의 ID를 HashSet에 담습니다.
        HashSet<int> attainableIDs = new HashSet<int>();

        if (nextNodes != null)
        {
            for (int i = 0; i < nextNodes.Count; i++) attainableIDs.Add(nextNodes[i].NodeID);
        }

        // 노드 시각 상태를 업데이트합니다.
        for (int i = 0; i < spawnedNodes.Count; i++)
        {
            MapNodeView nodeView = spawnedNodes[i];
            int id = nodeView.NodeID;

            if (id == currentNodeID) nodeView.SetVisualState(NodeVisualState.Current);
            else if (visitedNodeIDs.Contains(id))
            {
                Sprite randomMark = null;

                // 방문 마크는 노드 ID를 시드처럼 사용해서 같은 노드에는 같은 마크가 나오도록 합니다.
                if (visitedMarkSprites != null && visitedMarkSprites.Count > 0)
                {
                    System.Random random = new System.Random(id);
                    randomMark = visitedMarkSprites[random.Next(0, visitedMarkSprites.Count)];
                }
                nodeView.SetVisualState(NodeVisualState.Visited, randomMark);
            }
            else if (attainableIDs.Contains(id)) nodeView.SetVisualState(NodeVisualState.Attainable);
            else nodeView.SetVisualState(NodeVisualState.Locked);
        }

        // 선 시각 상태를 업데이트합니다.
        for (int i = 0; i < lineConnections.Count; i++)
        {
            LineConnection connection = lineConnections[i];
            Color lineColor = connection.lineImage.color;

            bool isStartVisited = visitedNodeIDs.Contains(connection.startNodeID) || connection.startNodeID == currentNodeID;
            bool isEndVisited = visitedNodeIDs.Contains(connection.endNodeID) || connection.endNodeID == currentNodeID;
            bool isNextPath = (connection.startNodeID == currentNodeID) && attainableIDs.Contains(connection.endNodeID);

            if (isStartVisited && isEndVisited) lineColor.a = 1.0f; // 지나온 길
            else if (isNextPath) lineColor.a = 0.5f;                // 갈 수 있는 길
            else lineColor.a = 0.15f;                               // 잠긴 길

            connection.lineImage.color = lineColor;
        }
    }

    /// <summary>
    /// 현재 노드 위치로 지도 스크롤을 부드럽게 이동합니다.
    /// </summary>
    public void FocusCamera(float targetNormalizedY)
    {
        if (mapScrollRect == null) return;
        if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(ScrollToRoutine(targetNormalizedY));
    }

    private IEnumerator ScrollToRoutine(float targetY)
    {
        float startY = mapScrollRect.verticalNormalizedPosition;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            mapScrollRect.verticalNormalizedPosition = Mathf.Lerp(startY, targetY, elapsed / duration);
            yield return null;
        }

        mapScrollRect.verticalNormalizedPosition = targetY;
    }
}
