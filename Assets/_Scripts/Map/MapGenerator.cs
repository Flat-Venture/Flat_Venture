using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시드값을 기반으로 지도의 전체 구조와 방 배치를 생성하는 클래스
/// </summary>
public class MapGenerator
{
    private const int MAX_FLOOR = 8;        //세로 노드 수
    private const int MAX_WIDTH = 6;        //가로 최대 노드 수
    private const int MAX_START_NODES = 4;  //시작 최대 노드 수

    public int CurrentSeed { get; private set; }

    /// <summary>
    /// 지정된 시드를 바탕으로 맵(그래프)를 생성하여 반환
    /// </summary>
    public List<List<MapNode>> GenerateMap(int seed)
    {
        CurrentSeed = seed;
        System.Random mapRandom = new System.Random(seed);

        List<List<MapNode>> entireMap = new List<List<MapNode>>();
        int globalNodeID = 0;

        //노드 뼈대 생성 (방 개수 결정)
        for (int floor = 0; floor < MAX_FLOOR; floor++)
        {
            List<MapNode> currentFloorNode = new List<MapNode>();

            //1층은 시작 노드 1~4개 랜덤 생성, 나머지는 1~6개 사이로 무작위 생성 로직 적용
            int nodeCount = (floor == 0) ? mapRandom.Next(2, MAX_START_NODES + 1) : mapRandom.Next(2, MAX_WIDTH + 1);

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode newNode = new MapNode(globalNodeID++, floor);
                
                //노드의 UI 가로 위치 비율 설정 (충돌 방지를 위한 균등 분할 기반 미세 조정)
                newNode.NormalizedX = (i + 1.0f) / (nodeCount + 1.0f) + (float)(mapRandom.NextDouble() * 0.1f - 0.05f);
                newNode.NormalizedY = (float)floor / (MAX_FLOOR - 1);
                currentFloorNode.Add(newNode);
            }
            entireMap.Add(currentFloorNode);
        }

        //노드 간 연결 로직
        ConnectNodes(entireMap, mapRandom);

        //방 종류 할당 로직
        AssignRoomTypes(entireMap, mapRandom);

        return entireMap;
    }

    /// <summary>
    /// 각 층의 노드들을 다음 층과 연결하는 로직
    /// </summary>
    private void ConnectNodes(List<List<MapNode>> map, System.Random mapRandom)
    {
        for (int floor = 0; floor < map.Count - 1; floor++)
        {
            List<MapNode> currentNodes = map[floor];
            List<MapNode> nextNodes = map[floor + 1];

            //현재 층의 모든 노드에서 다음 층으로 최소 1개 이상의 길을 연결
            for (int i = 0; i < currentNodes.Count; i++)
            {
                MapNode currentNode = currentNodes[i];
                MapNode targetNode = GetRandomClosestNode(currentNode, nextNodes, mapRandom);
                currentNode.AddNextNode(targetNode);
            }

            //고립 방지: 다음 층의 노드 중 들어오는 연결(Incoming)이 없는 노드를 찾아 반드시 연결
            for (int i = 0; i < nextNodes.Count; i++)
            {
                MapNode nextNode = nextNodes[i];
                bool hasIncoming = false;

                for (int j = 0; j < currentNodes.Count; j++)
                {
                    if (currentNodes[j].NextNodes.Contains(nextNode))
                    {
                        hasIncoming = true;
                        break;
                    }
                }

                //들어오는 길이 없다면, 이전 층의 노드 중 가장 가까운 것을 찾아 강제로 연결
                if (hasIncoming)
                {
                    MapNode sourceNode = GetRandomClosestNode(nextNode, currentNodes, mapRandom);
                    sourceNode.AddNextNode(nextNode);
                }
            }
        }
    }

    /// <summary>
    /// X 좌표를 기준으로 가장 가까운 거리에 있는 노드 후보군을 찾고 무작위로 하나를 반환
    /// </summary>
    private MapNode GetRandomClosestNode(MapNode sourceNode, List<MapNode> targetNodes, System.Random mapRandom)
    {
        float minDistance = float.MaxValue;
        List<MapNode> closestNodes = new List<MapNode>();

        //최적화: LINQ를 배제하고 O(N) 순회로 탐색하여 가비지 컬렉션(GC) 발생을 원천 차단
        for (int i = 0; i < targetNodes.Count; i++)
        {
            float distance = Math.Abs(sourceNode.NormalizedX - targetNodes[i].NormalizedX);

            //새로운 최소 거리를 갱신한 경우
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNodes.Clear();
                closestNodes.Add(targetNodes[i]);
            }

            //기존 최소 거리와 비슷하게 가까운 경우(오차 범위 내) 후보군에 추가
            else if (distance < minDistance + 0.15f) closestNodes.Add(targetNodes[i]);
        }

        //후보군 중 무작휘 선택을 통해 경로가 예층 불가능하게 꼬이는 것을 구현
        int randomIndex = mapRandom.Next(0, closestNodes.Count);
        return closestNodes[randomIndex];
    }

    /// <summary>
    /// 연결된 맵 노드들에 기획된 확률과 쿼터(할당량)에 맞춰 방 종류를 배정
    /// </summary>
    private void AssignRoomTypes(List<List<MapNode>> map, System.Random mapRandom)
    {
        Dictionary<RoomType, int> quotas = new Dictionary<RoomType, int>()
        {
            { RoomType.Shop, 2 },   //맵 전체에서 상점은 최대 2개
            { RoomType.Forge, 2 },  //맵 전체에서 대장간은 최대 2개
            { RoomType.Rest, 1 }    //맵 전체에서 휴식은 최대 1개 (보스 앞 강제 휴식 방은 별도 처리)
        };

        for (int floor = 0; floor < map.Count; floor++)
        {
            List<MapNode> nodesInFloor = map[floor];
            
            for (int i = 0; i < nodesInFloor.Count; i++)
            {
                MapNode node = nodesInFloor[i];

                //1층: 게임의 시작은 무조건 일반 몬스터 전투로 배치하여 빌드업 시작
                if (floor == 0) node.RoomType = RoomType.Normal;

                //마지막 층 (8층, index 7): 각 챕터(막) 보스 방 고정
                else if (floor == map.Count - 1) node.RoomType = RoomType.Boss;

                //마지막 직전 층 (7층, index 6): 보스 직전 강제 휴식 방 배치
                else if (floor == map.Count - 2) node.RoomType = RoomType.Rest;

                //중간 층 (2층 ~ 6층): 확률 및 쿼터 기반 절차적 무작위 생성
                else node.RoomType = GetRandomRoomTypeWithQuota(mapRandom, quotas);
            }
        }
    }

    /// <summary>
    /// 가중치(Weight) 확률에 따라 방을 무작위로 뽑되, 제한된 쿼터를 초과하면 대체 방으로 변경
    /// </summary>
    private RoomType GetRandomRoomTypeWithQuota(System.Random mapRandom, Dictionary<RoomType, int> quotas)
    {
        //방 등장 확률 가중치 (총합 100 기준, 추후 기획 밸런스에 맞춰 수지 조정 가능)
        int roll = mapRandom.Next(0, 100);

        RoomType selectedType;

        if (roll < 40) selectedType = RoomType.Normal;          //40% 확률로 일반 몬스터
        else if (roll < 65) selectedType = RoomType.Unknown;    //25% 확률로 이벤트(물음표)
        else if (roll < 80) selectedType = RoomType.Elite;      //15% 확률로 엘리트 몬스터
        else if (roll < 90) selectedType = RoomType.Shop;       //10% 확률로 상점
        else if (roll < 95) selectedType = RoomType.Forge;      //5% 확률로 대장간
        else selectedType = RoomType.Rest;                      //5% 확률로 휴식

        //쿼터(최대 개수) 초과 검증 및 대처(Fallback) 로직
        if (quotas.ContainsKey(selectedType))
        {
            if (quotas[selectedType] > 0) quotas[selectedType]--; //쿼터 1개 소모

            //제한된 개수를 모두 소모했다면, 일반 몬스터(Normal) 방이나 이벤트(Unknown) 방으로 강제 대체 (50:50 확률)
            else selectedType = mapRandom.Next(0, 2) == 0 ? RoomType.Normal : RoomType.Unknown;
        }

        return selectedType;
    }
}
