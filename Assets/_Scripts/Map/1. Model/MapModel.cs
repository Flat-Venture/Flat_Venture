using System.Collections.Generic;

/// <summary>
/// 지도의 데이터와 현재 게임 상태를 관리하는 Model
/// </summary>
public class MapModel
{
    public List<List<MapNode>> EntireMap { get; private set; }
    public int CurrentSeed { get; private set; }

    //현재 플레이어 위치 및 던전 안에 있는지 여부 확인
    public int CurrentNodeID { get; set; } = -1;
    public bool isInDungeon = false;

    //지나온 노드들의 ID를 저장하는 리스트
    public List<int> VisitedNodeIDs { get; private set; } = new List<int>();

    //시드값과 전체 맵 데이터
    public void SetMapData(int seed, List<List<MapNode>> mapData)
    {
        CurrentSeed = seed;
        EntireMap = mapData;
        VisitedNodeIDs.Clear();

        //맵이 생성되면, 0층의 유일한 노드를 플레이어의 최초 위치로 저장
        CurrentNodeID = mapData[0][0].NodeID;
    }

    /// <summary>
    /// 저장된 지도 진행 상태를 복원합니다. 세이브/로드가 없는 테스트에서는 호출하지 않아도 됩니다.
    /// </summary>
    public void RestoreProgress(int currentNodeID, IEnumerable<int> visitedNodeIDs, bool isInDungeon)
    {
        CurrentNodeID = currentNodeID;
        this.isInDungeon = isInDungeon;

        VisitedNodeIDs.Clear();
        if (visitedNodeIDs == null)
        {
            return;
        }

        foreach (int visitedNodeID in visitedNodeIDs)
        {
            if (!VisitedNodeIDs.Contains(visitedNodeID))
            {
                VisitedNodeIDs.Add(visitedNodeID);
            }
        }
    }

    /// <summary>
    /// 현재 노드가 유효하지 않으면 시작 노드로 되돌립니다.
    /// </summary>
    public void EnsureValidCurrentNode()
    {
        if (EntireMap == null || EntireMap.Count == 0 || EntireMap[0].Count == 0)
        {
            CurrentNodeID = -1;
            return;
        }

        if (GetNodeByID(CurrentNodeID) == null)
        {
            CurrentNodeID = EntireMap[0][0].NodeID;
        }
    }

    /// <summary>
    /// 노드 ID를 기반으로 맵 전체에서 해당 노드 객체를 찾아 반환
    /// </summary>
    public MapNode GetNodeByID(int nodeID)
    {
        for (int floor = 0; floor < EntireMap.Count; floor++)
        {
            for (int i = 0; i < EntireMap[floor].Count; i++)
            {
                if (EntireMap[floor][i].NodeID == nodeID) return EntireMap[floor][i];
            }
        }

        return null;
    }
}
