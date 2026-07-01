using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject linePrefab;         //단순한 흰색 Image 컴포넌트가 있는 프리팹
    [SerializeField] private MapNodeView nodePrefab;    

    //View의 순수 UI 상태 변수
    public bool isMapOpendByTab = false;

    //생성된 UI 추적용 (Object Pooling 적용 시 재사용)
    private List<GameObject> spawnedLines = new List<GameObject>();
    private List<MapNodeView> spawnedNodes = new List<MapNodeView>();

    private void Awake()
    {
        //초기 상태에서 지도 UI 숨김
        HideMap();
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

                //TODO: 추후 RoomType(node.RoomType)에 따라 맞는 Sprite를 매칭해 넘겨주는 로직 연동
                nodeView.Init(node, null);
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

                    DrawLine(startRect.anchoredPosition, endRect.anchoredPosition);
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
    private void DrawLine(Vector2 startPos, Vector2 endPos)
    {
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

        //선의 두께를 5f로 설정하고 길이를 두 노드 사이의 거리만큼 조정
        lineRect.sizeDelta = new Vector2(distance, 5f);
        lineRect.anchoredPosition = startPos + direction / 2;   //두 지점의 중앙에 위치
        lineRect.localRotation = Quaternion.Euler(0, 0, angle); //목적지를 향해 회전

        spawnedLines.Add(lineObject);
    }

    private void ClearMap()
    {
        for (int i = 0; i < spawnedNodes.Count; i++) Destroy(spawnedNodes[i].gameObject);
        for (int i = 0; i < spawnedLines.Count; i++) Destroy(spawnedLines[i]);

        spawnedNodes.Clear();
        spawnedLines.Clear();
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
}
