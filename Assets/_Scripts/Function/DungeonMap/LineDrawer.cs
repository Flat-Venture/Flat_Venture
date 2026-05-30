using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    public GameObject linePrefab;               // 라인 UI(Image) 프리펩
    public RectTransform lineParent;            // 프리펩 생성위치 설정
    public float lineThickness = 4f;            // 라인 두깨 설정.

    private readonly List<GameObject> activeLines = new List<GameObject>();
    //연출을 위한 라인의 정보 저장
    private Dictionary<int, List<GameObject>> floorLines = new Dictionary<int, List<GameObject>>();
    
    public void DrawAllConnections(NodeButton[,] nodes, int maxFloor, int maxColumn)
    {
        ClearAllLines();
        //모든 노드 순회
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int Column = 0; Column < maxColumn; Column++)
            {
                NodeButton NodeData = nodes[floor, Column];
                //(현 노드 or 다음노드가 Null이거나), RoomType이 None이면 continue
                if ((NodeData == null ||NodeData.nextNodes == null ||(NodeData.CurrentRoomType == Enums.RoomType.None)))
                {
                    continue;
                }

                foreach (NodeButton targetNode in NodeData.nextNodes)
                {
                    if (targetNode == null || targetNode.CurrentRoomType == Enums.RoomType.None)
                    {
                        continue;
                    }
                    DrawLine(NodeData, targetNode);
                }
            }
        }
    }


    public void ClearAllLines()
    {
        foreach (var line in activeLines)
        {
            if (line != null) Destroy(line);
        }
        activeLines.Clear();
    }

    public void HideAllLines()
    {
        foreach (var line in activeLines)
            line.SetActive(false);
    }

    public void ShowLinesForFloor(int floor)
    {
        if (!floorLines.ContainsKey(floor)) return;

        foreach (var line in floorLines[floor])
            line.SetActive(true);
    }

    private void DrawLine(NodeButton startNode, NodeButton targetNode)
    {
        if (startNode == null || targetNode == null) return;
        //라인 프리펩을 이용한 선잇기.
        GameObject lineObj = Instantiate(linePrefab, lineParent);
        activeLines.Add(lineObj);

        //연출용 코드 추가
        int floor = Mathf.Min(startNode.floor, targetNode.floor);

        if (!floorLines.ContainsKey(floor))
            floorLines[floor] = new List<GameObject>();

        floorLines[floor].Add(lineObj);


        //라인의 위치값
        RectTransform lineRect  = lineObj.GetComponent<RectTransform>();
        //시작노드와 목적지 노드의 위치.
        RectTransform startRect = startNode.GetComponent<RectTransform>();
        RectTransform targetRect = targetNode.GetComponent<RectTransform>();

        // 1. UI anchoredPosition 직접 사용 (가장 정확한 방법)
        Vector2 start = startRect.anchoredPosition;
        Vector2 end = targetRect.anchoredPosition;

        // 2. 중간 지점 배치
        Vector2 center = (start + end) * 0.5f;
        lineRect.anchoredPosition = center;

        // 3. 라인 길이 설정
        float distance = Vector2.Distance(start, end);
        lineRect.sizeDelta = new Vector2(distance, lineThickness);

        // 4. 각도 적용
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
