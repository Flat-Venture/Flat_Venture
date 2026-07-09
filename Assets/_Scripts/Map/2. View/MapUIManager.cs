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
/// 인게임 화면에서 지도의 가시성과 노드 상호작용 상태 관리
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
    [SerializeField] private GameObject linePrefab;             //단순한 흰색 Image 컴포넌트가 있는 프리팹
    [SerializeField] private MapNodeView nodePrefab;

    [Header("Visual Resources")]
    [SerializeField] private List<Sprite> visitedMarkSprites;   //사용할 랜덤 O 표시 스프라이트 목록
    [SerializeField] private List<RoomIconData> roomIconList;   //방 종류별 아이콘 리스트

    //View의 순수 UI 상태 변수
    public bool isMapOpendByTab = false;

    private class LineConnection
    {
        public Image lineImage;
        public int startNodeID;
        public int endNodeID;
    }

    //생성된 UI 추적용 (Object Pooling 적용 시 재사용)
    private List<LineConnection> lineConnections = new List<LineConnection>();
    private List<MapNodeView> spawnedNodes = new List<MapNodeView>();

    //실제 검색용 딕셔너리
    private Dictionary<RoomType, Sprite> roomIconDict = new Dictionary<RoomType, Sprite>();
    private Coroutine scrollCoroutine;

    private void Awake()
    {
        //딕셔너리 초기화
        foreach (var data in roomIconList)
        {
            if (!roomIconDict.ContainsKey(data.roomType))
            {
                roomIconDict.Add(data.roomType, data.iconSprite);
            }
        }

        //초기 상태에서 지도 UI 숨김
        HideMap();
    }

    /// <summary>
    /// 방 종류에 맞는 스프라이트를 리스트에서 찾아 반환
    /// </summary>
    private Sprite GetRoomIcon(RoomType type)
    {
        if (roomIconDict.TryGetValue(type, out Sprite sprite))
        {
            return sprite;
        }

        //어떤 타입에서 매칭이 안 되는지 로그 출력
        Debug.LogWarning($"[MapUIManager] {type} 타입에 맞는 아이콘을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// Model의 데이터를 받아 화면에 노드와 선을 인스턴스화
    /// </summary>
    public void DrawMap(List<List<MapNode>> mapData, System.Action<int> onNodeClickCallback)
    {
        ClearMap();

        //최적화: 선을 그릴 때 노드의 위치를 0(1) 속도로 찾기 위한 캐식 딕셔너리
        Dictionary<int, MapNodeView> nodeViewDict = new Dictionary<int, MapNodeView>();

        //노드 배치
        for (int floor = 0; floor < mapData.Count; floor++)
        {
            for (int i = 0; i < mapData[floor].Count; i++)
            {
                MapNode node = mapData[floor][i];
                MapNodeView nodeView = Instantiate(nodePrefab, nodeContainer);

                //RoomType에 맞는 고유 스프라이트를 가져와서 Init에 전달
                Sprite roomSprite = GetRoomIcon(node.RoomType);
                nodeView.Init(node, roomSprite);

                //보스방 아이콘 크기 조절
                nodeView.SetBossScale(node.RoomType == RoomType.Boss);

                nodeView.SetPosition(contentRect, node.NormalizedX, node.NormalizedY);
                nodeView.onNodeClicked = onNodeClickCallback;

                spawnedNodes.Add(nodeView);
                nodeViewDict.Add(node.NodeID, nodeView);
            }
        }

        //선 연결
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

        //맵 렌더링이 끝난 직후, 스크롤을 맨 아래(0.0f)로 강제 이동
        if (mapScrollRect != null)
        {
            // Canvas 강제 업데이트 후 스크롤 위치를 0(Bottom)으로 고정
            Canvas.ForceUpdateCanvases();
            mapScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 두 UI 좌표 사이의 거리와 각도를 계산해 선을 연결
    /// </summary>
    private void DrawLine(RectTransform startRect, RectTransform endRect, int startID, int endID)
    {
        Vector2 startPos = startRect.anchoredPosition;
        Vector2 endPos = endRect.anchoredPosition;
        
        GameObject lineObject = Instantiate(linePrefab, lineContainer);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();

        //강제 중앙 앵커 및 피벗 고정 (선이 노드 중앙에 닿지 않고 끊어지는 현상 차단)
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        //삼각함수를 통한 길이 및 각도 계산
        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //UI의 실제 가로 길이를 가져와 반지름 계산
        float startRadius = startRect.rect.width / 2f;
        float endRadius = endRect.rect.width / 2f;

        //아이콘과 선이 너무 딱 붙지 않게 틈을 줌
        float gapOffset = 10f;

        //동적 여백 = 출발지 반지름 + 도착지 반지름 + 여백
        float padding = startRadius + endRadius + gapOffset;
        float finalLineLength = Mathf.Max(0, distance - padding);

        //선의 두께를 5f로 설정하고 길이를 두 노드 사이의 거리만큼 조정
        lineRect.sizeDelta = new Vector2(finalLineLength, 5f);
        lineRect.anchoredPosition = startPos + direction / 2;   //두 지점의 중앙에 위치
        lineRect.localRotation = Quaternion.Euler(0, 0, angle); //목적지를 향해 회전

        LineConnection connectrion =  new LineConnection
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
    /// 최적화: GameObject.SetActive 대신 CanvasGroup을 제어합니다.
    /// </summary>
    /// <param name="isInteractable">노드 클릭 가능 여부 설정</param>
    public void ShowMap(bool isInteractable)
    {
        mapCanvasGroup.alpha = 1.0f;
        mapCanvasGroup.blocksRaycasts = isInteractable;
        mapCanvasGroup.enabled = isInteractable;
    }

    public void HideMap()
    {
        mapCanvasGroup.alpha = 0.0f;
        mapCanvasGroup.blocksRaycasts = false;
        mapCanvasGroup.enabled = false;
    }

    /// <summary>
    /// 맵에 배치된 모든 노드의 시각적 상태(알파값, 마크)를 일괄 업데이트
    /// </summary>
    public void UpdateNodeVisuals(int currentNodeID, List<int> visitedNodeIDs, List<MapNode> nextNodes)
    {
        //빠른 탐색을 위해 갈 수 있는 다름 노드들의 ID를 HashSet에 담음
        HashSet<int> attainableIDs = new HashSet<int>();

        if (nextNodes != null)
        {
            for (int i = 0; i < nextNodes.Count; i++) attainableIDs.Add(nextNodes[i].NodeID);
        }

        //노드 시각적 업데이트
        for (int i = 0; i < spawnedNodes.Count; i++)
        {
            MapNodeView nodeView = spawnedNodes[i];
            int id = nodeView.NodeID;

            if (id == currentNodeID) nodeView.SetVisualState(NodeVisualState.Current);
            else if (visitedNodeIDs.Contains(id))
            {
                Sprite randomMark = null;

                //선택된 마크 리스트가 존재할 경우 노드 ID를 시드로 사용하여 영구적이 난수 생성
                if (visitedMarkSprites != null && visitedMarkSprites.Count >0)
                {
                    System.Random random = new System.Random(id);
                    randomMark = visitedMarkSprites[random.Next(0, visitedMarkSprites.Count)];
                }
                nodeView.SetVisualState(NodeVisualState.Visited, randomMark);
            }

            else if (attainableIDs.Contains(id)) nodeView.SetVisualState(NodeVisualState.Attainable);
            else nodeView.SetVisualState(NodeVisualState.Locked);
        }

        //선 시각적 업데이트
        for (int i = 0; i < lineConnections.Count; i++)
        {
            LineConnection connection = lineConnections[i];
            Color lineColor = connection.lineImage.color;

            bool isStartVisited = visitedNodeIDs.Contains(connection.startNodeID) || connection.startNodeID == currentNodeID;
            bool isEndVisited = visitedNodeIDs.Contains(connection.endNodeID) || connection.endNodeID == currentNodeID;
            bool isNextPath = (connection.startNodeID == currentNodeID) && attainableIDs.Contains(connection.endNodeID);

            if (isStartVisited && isEndVisited) lineColor.a = 1.0f; //지나온 길 (밝게)
            else if (isNextPath) lineColor.a = 0.5f;                //갈 수 있는 길 (중간)
            else lineColor.a = 0.15f;                               //버려진 길 / 잠긴 길 (어둡게)

            connection.lineImage.color = lineColor; 
        }
    }

    /// <summary>
    /// 카메라 부드러운 스크롤 이동
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
        float duration = 0.4f;  //이동 시간

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mapScrollRect.verticalNormalizedPosition = Mathf.Lerp(startY, targetY, elapsed / duration);
            yield return null;
        }
        
        mapScrollRect.verticalNormalizedPosition = targetY;
    }
}
