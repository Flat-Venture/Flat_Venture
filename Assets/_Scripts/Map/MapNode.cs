using System.Collections.Generic;

/// <summary>
/// 맵에 등장하는 방(노드)의 종류를 정의
/// </summary>
public enum RoomType
{
    Start,      //시작 방
    Normal,     //일반 몬스터
    Elite,      //엘리트 몬스터
    Rest,       //휴식
    Forge,      //대장간
    Shop,       //상점
    Unknown,    //이벤트(물음표/미지)
    Boss,       //보스    
}

/// <summary>
/// 개별 노드의 데이터 단위. UI와 완전히 분리된 순수 데이터 클래스.
/// </summary>
public class MapNode
{
    public int NodeID { get; private set; }         //노드 ID
    public int Floor { get; private set; }          //층수
    public RoomType RoomType { get; set; }          //노드의 방 타입

    //다음 층으로 연결된 노드들의 리스트
    public List<MapNode> NextNodes { get; private set; }

    //UI 배치용 논리적 좌표 (비율값: 0.0f ~ 1.0f)
    public float NormalizedX { get; set; }
    public float NormalizedY { get; set; }

    public MapNode(int id, int floor)
    {
        NodeID = id;
        Floor = floor;
        NextNodes = new List<MapNode>();
    }

    /// <summary>
    /// 다음 노드를 연결. 단방향 그래프 구조를 형성
    /// </summary>    
    public void AddNextNode(MapNode nextNode)
    {
        if(!NextNodes.Contains(nextNode)) NextNodes.Add(nextNode);
    }
}
