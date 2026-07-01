using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시드값을 기반으로 지도의 전체 구조와 방 배치를 생성하는 클래스
/// </summary>
public class MapGenerator
{
    private const int MAX_FLOOR = 9;        //세로 노드 수
    private const int MAX_WIDTH = 6;        //가로 최대 노드 수
    private const int MAX_START_NODES = 3;  //시작 최대 노드 수

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
            int nodeCount;

            //0층: 플레이어 최초 시작점
            if (floor == 0) nodeCount = 1;

            //1층: 시작점에서 뻗어나가는 첫 선택지 (2~4개)
            else if (floor == 1) nodeCount = mapRandom.Next(2, MAX_START_NODES + 1);

            //8층: 보스 방
            else if (floor == MAX_FLOOR - 1) nodeCount = 1;

            //나머지 층: 일반 맵 진행 (1~6개)
            else nodeCount = mapRandom.Next(2, MAX_WIDTH + 1);

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode newNode = new MapNode(globalNodeID++, floor);

                //시작 노드는 맵의 맨 아래(Y=0), 정중앙(X=0.5)에 고정 배치
                if (floor == 0)
                {
                    newNode.NormalizedX = 0.5f;
                    newNode.NormalizedY = 0.0f;
                }

                //보스 방
                else if (floor == MAX_FLOOR - 1)
                {
                    newNode.NormalizedX = 0.5f;
                    newNode.NormalizedY = 1.0f;
                }

                else
                {
                    //노드의 UI 가로 위치 비율 설정 (충돌 방지를 위한 균등 분할 기반 미세 조정)
                    newNode.NormalizedX = (i + 1.0f) / (nodeCount + 1.0f) + (float)(mapRandom.NextDouble() * 0.04f - 0.02f);
                    newNode.NormalizedY = (float)floor / (MAX_FLOOR - 1);
                }

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

            //X좌표 기준 오름차순 정렬
            currentNodes.Sort((a, b) => a.NormalizedX.CompareTo(b.NormalizedX));
            nextNodes.Sort((a, b) => a.NormalizedX.CompareTo(b.NormalizedX));

            int currentCount = currentNodes.Count;
            int nextCount = nextNodes.Count;

            //연결 개수를 엄격하게 추적하기 위한 카운터 배열
            int[] outCount = new int[currentCount];
            int[] inCount = new int[nextCount];

            //다음 층이 보스 방(마지막 층)인지 확인
            bool isBossLayer = (floor == map.Count - 2);

            //1:1 비례 뼈대 생성
            if (currentCount <= nextCount)
            {
                //윗층이 더 많거나 같으면: 윗층 노드들에게 아랫층 부모를 공평하게 할당
                for (int j = 0; j < nextCount; j++)
                {
                    int i = j * currentCount / nextCount;
                    AddEdge(i, j, currentNodes, nextNodes, outCount, inCount);
                }
            }

            else
            {
                //아랫층이 더 많으면: 아랫층 노드들에게 윗층 자식을 공평하게 할당
                for (int i = 0; i < currentCount; i++)
                {
                    int j = i * nextCount / currentCount;
                    AddEdge(i, j, currentNodes, nextNodes, outCount, inCount);
                }
            }

            //맵을 다채롭게 만들기 위해 2번 반복하며 여분의 선을 금
            for (int step = 0; step < 2; step++)
            {
                for (int i = 0; i < currentCount; i++)
                {
                    //40% 확률로 새로운 갈래길 시도
                    if (mapRandom.NextDouble() < 0.4f)
                    {
                        //선 교차를 막기 위해 연결 가능한 최소/최대 인덱스(Valid Range)를 동적으로 계산
                        int minJ = 0;
                        int maxJ = nextCount - 1;

                        if (i > 0)
                        {
                            foreach (var target in currentNodes[i - 1].NextNodes)
                            {
                                int idx = nextNodes.IndexOf(target);
                                if (idx > minJ) minJ = idx;
                            }
                        }

                        if (i < currentCount - 1)
                        {
                            int tempMax = nextCount - 1;
                            foreach (var target in currentNodes[i + 1].NextNodes)
                            {
                                int idx = nextNodes.IndexOf(target);
                                if (idx < tempMax) tempMax = idx;
                            }
                            maxJ = tempMax;
                        }

                        //유효한 범위 내에서 무작위 타겟 선택
                        if (minJ <= maxJ)
                        {
                            int targetJ = mapRandom.Next(minJ, maxJ + 1);

                            //보내는 쪽 3개 미만 && (받는 쪽 3개 미만 OR 보스 방) 일 때만 연결 허용
                            if (outCount[i] < 3 && (inCount[targetJ] < 3 || isBossLayer))
                            {
                                AddEdge(i, targetJ, currentNodes, nextNodes, outCount, inCount);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 중복 연결을 방지하고 카운트를 올려주는 헬퍼
    /// </summary>
    private void AddEdge(int c, int n, List<MapNode> currentNodes, List<MapNode> nextNodes, int[] outCount, int[] inCount)
    {
        if (!currentNodes[c].NextNodes.Contains(nextNodes[n]))
        {
            currentNodes[c].AddNextNode(nextNodes[n]);
            outCount[c]++;
            inCount[n]++;
        }
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

                //0층: 게임 시작 지점
                if (floor == 0) node.RoomType = RoomType.Start;

                //1층: 게임의 시작은 무조건 일반 몬스터 전투로 배치하여 빌드업 시작
                else if (floor == 1) node.RoomType = RoomType.Normal;

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

    /// <summary>
    /// 각 층의 노드들을 다음 층과 연결
    /// </summary>
    private void ConnectNode(List<List<MapNode>> map, System.Random mapRandom)
    {
        for (int floor = 0; floor < map.Count - 1; floor++)
        {
            List<MapNode> currentNodes = map[floor];
            List<MapNode> nextNodes = map[floor + 1];

            //노드 꼬임 방지를 위해 각 층의 노드들을 X좌표 기준으로 오름차순 정렬
            currentNodes.Sort((a, b) => a.NormalizedX.CompareTo(b.NormalizedX));
            nextNodes.Sort((a, b) => a.NormalizedX.CompareTo(b.NormalizedX));

            //현재 노드보다 오른쪽에 있는 노드는 현재 노드가 연결된 다음 층 노드보다 왼쪽에 있는 노드와 연결할 수 없음
            int nextNodeStartIndex = 0;

            //현재 층을 순회하며 다음 층으로 길을 염
            for (int i = 0; i < currentNodes.Count; i++)
            {
                MapNode currentNode = currentNodes[i];

                //현재 노드에더 다음 층으로 뻗어나갈 길의 개수를 결정
                int connectionCount = mapRandom.Next(1, 3); //1~2개 연결

                for (int j = 0; j < connectionCount; j++)
                {
                    //교차 방지: 항상 이전 노드가 연결했던 인덱스 이상만 연결
                    int targetIndex = nextNodeStartIndex + mapRandom.Next(0, 2);

                    //인덱스가 다음 층 녿드의 최대 개수를 넘지 않도로 제한
                    if (targetIndex >= nextNodes.Count) targetIndex = nextNodes.Count - 1;

                    MapNode targetNode = nextNodes[targetIndex];
                    currentNode.AddNextNode(targetNode);

                    //다음 노드는 최소한 현재 타겟 노드와 같은 위치거나 그 오른쪽과 연결되어 있어야 함
                    nextNodeStartIndex = targetIndex;
                }
            }

            //고립 방지: 다음 층 노드 중 들어오는 길이 없는 노드를 구제
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

                //들어오는 길이 없다면 이전 층의 노드 중 수 X좌표 거리가 가장 가까운 노드를 찾아 강제로 연결
                if (!hasIncoming)
                {
                    int closestIndex = GetClosestNodeIndex(nextNode, currentNodes);
                    currentNodes[closestIndex].AddNextNode(nextNode);
                }
            }
        }
    }

    /// <summary>
    /// X좌표를 기준으로 가장 가까운 거리에 있는 노드의 인덱스를 O(N) 순회로 탐색
    /// </summary>
    private int GetClosestNodeIndex(MapNode targetNode, List<MapNode> candidates)
    {
        int bestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            float distance = Math.Abs(candidates[i].NormalizedX - targetNode.NormalizedX);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
