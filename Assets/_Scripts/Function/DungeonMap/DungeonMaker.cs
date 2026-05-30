using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static DungeonManager;

public class DungeonMaker : MonoBehaviour
{
    //노드 생성설정
    public int maxFloor = 8;
    public int maxColumn = 6;

    private int startNodeCountLimit = 3;
    private int bossNodeCountLimit = 3;
    private int eliteNodeCountLimit = 4;

    //Button의 Type에 따른 Sprite연결을 위한 Dictionary
    private Dictionary<Enums.RoomType, Sprite> iconSpriteDictionary;

    [Header("Dungeon Type Sprites")]
    public Sprite normalSprite;
    public Sprite eliteSprite;
    public Sprite shopSprite;
    public Sprite restSprite;
    public Sprite forgeSprite;
    public Sprite bossSprite;

    //Map의 Type에 따른 배경화면 구현을 위한 Sprite연결을 위한 Dictionary
    private Dictionary<Enums.DungeonTheme, Sprite> themeSpriteDictionary;

    [Header("Dungeon Theme Sprites")]
    public Sprite desertThemeSprite;
    public Sprite grassThemeSprite;
    public Sprite lavaThemeSprite;
    public Sprite snowThemeSprite;

    //지도 맵의 배경화면 연결을 위한 객체선언.
    [Header("UI")]
    public Image environmentBackgroundImage;

    //라인 처리 스크립트 inspector창에서 연결.
    public LineDrawer lineDrawer;

    //시작노드 정보(층수,열, 사용하는가?, RoomType,다음노드 정보)
    private List<NodeButton> startNodes = new List<NodeButton>();

    //룸 타입 스크립트에서 정보 가져오기.
    public RoomTypeData roomTypeData;

    //버튼 프리팹 연결 및 생성위치.
    public GameObject roomButtonPrefab;
    public Transform mapParent;

    //던전 버튼의 층수,열의 정보.;
    private NodeButton[,] dungeonButtons;

    //연출 타이밍을 위한 
    private bool isNewDungeonCreated = false;

    //노드 드러내기 중 클릭 방지용 판넬 제어용 변수.
    public GameObject ClickBlockPanel;

    SoundManager soundManager;

    //---------------------------------------------------------------------------------------------
    //Start는 Scene이동 후 돌아올 때마다 실행됨.
    void Start()
    {
        soundManager = SoundManager.Instance;
        DungeonManager.Instance.EnterDungeon();

        //테마 Sprite Inspector로 설정.
        themeSpriteDictionary = new Dictionary<Enums.DungeonTheme, Sprite>()
        {
            { Enums.DungeonTheme.Desert, desertThemeSprite },
            //{ Enums.DungeonTheme.Grass,  grassThemeSprite },
            //{ Enums.DungeonTheme.Lava,   lavaThemeSprite },
            { Enums.DungeonTheme.Snow,   snowThemeSprite }
        };

        //노드 아이콘 Sprite Inspector로 설정
        iconSpriteDictionary = new Dictionary<Enums.RoomType, Sprite>()
        {
            { Enums.RoomType.None, null },
            { Enums.RoomType.Normal, normalSprite },
            { Enums.RoomType.Elite, eliteSprite },
            { Enums.RoomType.Shop, shopSprite },
            { Enums.RoomType.Rest, restSprite },
            { Enums.RoomType.Forge, forgeSprite },
            { Enums.RoomType.Boss, bossSprite }
        };

        //NodeButton타입을 가진 2차원 배열 생성
        dungeonButtons = new NodeButton[maxFloor, maxColumn];
        //테마와 가져오기.
        ApplyEnvironmentTheme(DungeonManager.Instance.GetCurrentTheme());

        // 기존 던전 유/무 확인
        if (DungeonManager.Instance != null && DungeonManager.Instance.HasDungeonData())
        {
            //기존 던전 데이터 가져오기
            LoadDungeonFromData(DungeonManager.Instance.GetDungeonData());
            //클리어한 노드 상태 변경.
            applyClearNodeState();

            Debug.Log("[DungeonMaker] 기존 던전 데이터 로드 완료");
            isNewDungeonCreated = false;
            SetClickBlock(false);
        }
        else
        {
            //[[[신규 던전 생성.]]]

            //DungeonManager에서 테마 종류 가져오기.
            Enums.DungeonTheme dungeonTheme = DungeonManager.Instance.GetCurrentTheme();
            ApplyEnvironmentTheme(dungeonTheme);

            //던전 노드 생성
            GenerateDungeon();
            //던전 선 연결
            GenerateNodeConnections();

            //생성된 던전 데이터 DungeonManager쪽에 저장.
            DungeonMapData saveData = ExportDungeonData();
            saveData.theme = dungeonTheme;
            DungeonManager.Instance?.SaveDungeonData(saveData);
            Debug.Log("[DungeonMaker] 신규 던전 생성 및 저장 완료");
            isNewDungeonCreated = true;
        }

        //모든 노드, 선 정보를 기반으로 선그리기.
        lineDrawer.DrawAllConnections(dungeonButtons, maxFloor, maxColumn);
        // PrintDungeonToConsole();


        //던전 알파값 조정
        SetAllNodesAlpha(0.3f);

        //클리어 후 다음 갈 던전 노드 OFF 작업.(이 작업 안하면 클리어 클리어한 노드 계속 켜져있을 거임.)
        ResetAllNextNodeFlags();
        // 클리어된 노드 기준으로 다음 노드 열기
        applyClearNodeState();

        // 3. 클리어가 하나도 없으면 시작층 열기
        if (!HasAnyClearedNode())
        {
            OpenStartFloor();
        }

        if (isNewDungeonCreated)
        {
            HideAllMapElements();
            MapEffectController.Instance.MapUpEffect();
        }

    }

    //연출을 위한 코드

    public void SetClickBlock(bool value)
    {
        if (ClickBlockPanel != null)
            ClickBlockPanel.SetActive(value);
    }


    public void StartRevealMap()
    {
        StartCoroutine(RevealMapByFloor());
    }


    private void HideAllMapElements()
    {
        for (int f = 0; f < maxFloor; f++)
        {
            for (int c = 0; c < maxColumn; c++)
            {
                if (dungeonButtons[f, c] != null)
                    dungeonButtons[f, c].SetVisible(false);
            }
        }

        lineDrawer.HideAllLines();
    }

    private IEnumerator RevealMapByFloor()
    {
        for (int floor = 0; floor < maxFloor; floor++)
        {
            // 노드 표시
            for (int c = 0; c < maxColumn; c++)
            {
                NodeButton node = dungeonButtons[floor, c];
                if (node != null && node.isAvailable)
                    node.SetVisible(true);
            }

            // 선 표시
            lineDrawer.ShowLinesForFloor(floor);

            yield return new WaitForSeconds(0.1f);
        }

        SetClickBlock(false);
    }


    //클리어 관련 작동 코드----------------------------------------------------------------------------------


    //알파값 전체 작업 코드
    private void SetAllNodesAlpha(float alpha)
    {
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int column = 0; column < maxColumn; column++)
            {
                NodeButton node = dungeonButtons[floor, column];
                if (node == null || !node.isAvailable) continue;

                node.SetAlpha(alpha);
            }
        }
    }

    //해당 다음 갈 길 체크하는 변수(isGoingNextNode) 최초 OFF시켜놓기.
    private void ResetAllNextNodeFlags()
    {
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int column = 0; column < maxColumn; column++)
            {
                NodeButton node = dungeonButtons[floor, column];
                if (node == null) continue;

                node.isGoingNextNode = false;
            }
        }
    }
    //던전의 모든 노드의 (isCleared)클리어 여부 확인.
    private bool HasAnyClearedNode()
    {
        var data = DungeonManager.Instance.GetDungeonData();
        if (data == null) return false;

        foreach (var node in data.nodes)
        {
            if (node.isCleared)
                return true;
        }
        return false;
    }

    //(HasAnyClearedNode 연계)클리어 노드가 없음으로 시작행의 모든 노드 Open
    private void OpenStartFloor()
    {
        for (int column = 0; column < maxColumn; column++)
        {
            NodeButton node = dungeonButtons[0, column];
            if (node == null || !node.isAvailable) continue;

            node.isGoingNextNode = true;
            node.SetAlpha(1f);
        }
    }

    //이제 진행해야 하는 노드 열어놓기(알파값 100%, 하이라이트 및 클릭 ON)
    private void OpenNode(NodeButton node)
    {
        if (node == null || !node.isAvailable) return;

        node.isGoingNextNode = true;
        node.SetAlpha(1f);
    }

    //노드 닫기(알파값 30%, 하이라이트 및 클릭 OFF)
    private void LockNode(NodeButton node)
    {
        if (node == null || !node.isAvailable) return;

        node.isGoingNextNode = false;
        node.SetAlpha(0.3f);
    }

    //매니저에서 테마값 가져와 스프라이트에 적용.
    private void ApplyEnvironmentTheme(Enums.DungeonTheme theme)
    {
        if (environmentBackgroundImage == null) return;

        if (themeSpriteDictionary.TryGetValue(theme, out var sprite))
        {
            environmentBackgroundImage.sprite = sprite;
            environmentBackgroundImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[DungeonMaker] 테마 스프라이트 없음: {theme}");
        }
    }

    //시작노드의 정보만 가져옴.
    private void CollectStartNodes()
    {
        startNodes.Clear();
        for (int column = 0; column < maxColumn; column++)
        {
            NodeButton node = dungeonButtons[0, column];
            if (node != null && node.isAvailable)
            {
                startNodes.Add(node);
            }
        }
    }


    //던전 재생성.-------------------------------------------------------------------------------------------

    //저장데이터 가공(DungeonManager에 보낼 정보)
    private DungeonMapData ExportDungeonData()
    {

        DungeonMapData saveData = new DungeonMapData();

        //기본정보인 최대 층,열 저장
        saveData.maxFloor = maxFloor;
        saveData.maxColumn = maxColumn;


        //각 노드 별의 내부 정보 저장.
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int colum = 0; colum < maxColumn; colum++)
            {
                //현재 노드 체크.
                NodeButton node = dungeonButtons[floor, colum];
                if (node == null) continue;

                //데이터 및 위치 저장.
                DungeonNodeData nodeData = new DungeonNodeData()
                {
                    floor = floor,
                    col = colum,
                    roomType = node.CurrentRoomType,
                    isAvailable = node.isAvailable,
                    uiPosition = node.GetComponent<RectTransform>().anchoredPosition
                };

                // 다음 연결된 노드의 좌표 저장
                foreach (var next in node.nextNodes)
                {
                    nodeData.nextNodesLink.Add(new NextNodeLinkData { floor = next.floor, column = next.col }
                    );
                }
                saveData.nodes.Add(nodeData);
            }
        }
        return saveData;
    }

    //던전 데이터 DungeonManager에서 가져오기.
    private void LoadDungeonFromData(DungeonMapData data)
    {
        //던전 매니저에 저장되어있는 노드별 데이터 다시 불러오기.
        foreach (var nodeData in data.nodes)
        {
            GameObject nodePrefab = Instantiate(roomButtonPrefab, mapParent);
            NodeButton node = nodePrefab.GetComponent<NodeButton>();
            if (node == null) continue;

            node.floor = nodeData.floor;
            node.col = nodeData.col;
            node.isAvailable = nodeData.isAvailable;
            if (nodeData.roomType == Enums.RoomType.None)
            {
                node.SetRoomType(Enums.RoomType.None, null);
            }
            else
            {
                node.SetRoomType(nodeData.roomType, iconSpriteDictionary[nodeData.roomType]);
            }

            RectTransform rect = nodePrefab.GetComponent<RectTransform>();
            rect.anchoredPosition = nodeData.uiPosition;

            dungeonButtons[nodeData.floor, nodeData.col] = node;
        }

        // 다음노드 연결 정보 복원
        foreach (var nodeData in data.nodes)
        {
            NodeButton currentNode = dungeonButtons[nodeData.floor, nodeData.col];
            if (currentNode == null) continue;

            foreach (var next in nodeData.nextNodesLink)
            {
                NodeButton nextNode =
                    dungeonButtons[next.floor, next.column];

                if (nextNode == null) continue;

                if (!currentNode.nextNodes.Contains(nextNode))
                {
                    currentNode.nextNodes.Add(nextNode);
                    nextNode.prevNodes.Add(currentNode);
                }
            }
        }
        CollectStartNodes();
    }

    //클리어 관련
    private void applyClearNodeState()
    {
        //매니저에서 전체 데이터 가져오기.
        var data = DungeonManager.Instance.GetDungeonData();
        if (data == null) return;

        //공간 선언
        HashSet<int> clearedFloors = new HashSet<int>();

        //클리어한 노드 수집.
        foreach (var nodeData in data.nodes)
        {
            //클리어 상태 표시된 노드만 추가.
            if (nodeData.isCleared)
                clearedFloors.Add(nodeData.floor);
        }

        //
        foreach (var nodeData in data.nodes)
        {
            //미클리어 노드 continue
            if (!nodeData.isCleared) continue;

            //공간 선언.
            NodeButton cleared = dungeonButtons[nodeData.floor, nodeData.col];
            if (cleared == null) continue;

            // 지도에 X표시(노드 프리팹의 X 스프라이트 SetActive(True))
            ApplyClearPrint_X(cleared);

            // 다음 노드 열기
            foreach (var next in nodeData.nextNodesLink)
            {
                // NextNodeData 기반 접근
                NodeButton nextNode = dungeonButtons[next.floor, next.column];
                if (nextNode == null || !nextNode.isAvailable) continue;

                OpenNode(nextNode);
            }
        }

        // 3. 같은 층의 다른 노드 전부 잠금
        foreach (int floor in clearedFloors)
        {
            for (int c = 0; c < maxColumn; c++)
            {
                NodeButton node = dungeonButtons[floor, c];
                if (node == null || !node.isAvailable) continue;

                LockNode(node);
            }
        }
    }

    //클리어한 노드 X표기하고 잠궈놓기.
    private void ApplyClearPrint_X(NodeButton node)
    {
        // Map X 사인 소리
        StartCoroutine(MapXSignSound());

        //노드 프리펩에 꺼놓은 X표시 스프라이트 켜기
        var clear = node.transform.Find("ClearMarkX");
        if (clear != null)
            clear.gameObject.SetActive(true);

        //노드 잠궈서 사용불가 처리
        LockNode(node);
    }

    IEnumerator MapXSignSound()
    {
        soundManager.PlaySFX("mapXSign");

        yield return new WaitForSeconds(soundManager.GetSoundData("mapXSign").clip.length);

        soundManager.PlaySFX("mapXSign");
    }


    //노드 생성부---------------------------------------------------------------------------------------------
    private void GenerateDungeon()
    {
        // [0,0노드]로 부터 위치 조정
        Vector2 startPos = new Vector2(-250f, -350f);
        float xSpacing = 100f;
        float ySpacing = 100f;

        //2차원 배열을 이용한 랜덤Type 노드 생성.
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int col = 0; col < maxColumn; col++)
            {

                Enums.RoomType type;
                type = roomTypeData.GetRoomType(floor, col);


                GameObject nodePrefab = Instantiate(roomButtonPrefab, mapParent);
                NodeButton nodeButton = nodePrefab.GetComponent<NodeButton>();

                if (nodeButton == null)
                {
                    Debug.LogError("RoomButton Prefab에 NodeButton 스크립트가 없습니다!");
                    continue;
                }

                //NodeButton.cs에 각 노드 정보 저장.----------------------------------------------------------
                nodeButton.floor = floor;
                nodeButton.col = col;
                nodeButton.isAvailable = (type != Enums.RoomType.None);//RoomType이 None이 아니면 True저장.

                //NodeButton.cs - SetRoomType() 버튼에 이미지 붙이기.
                nodeButton.SetRoomType(type, type != Enums.RoomType.None ? iconSpriteDictionary[type] : null);

                dungeonButtons[floor, col] = nodeButton;

                //노드 일정간격 배치.
                RectTransform nodeTransform = nodePrefab.GetComponent<RectTransform>();
                nodeTransform.anchoredPosition = new Vector2(startPos.x + col * xSpacing, startPos.y + floor * ySpacing);

                //---------------------------------------------------------------------------------------------
            }
        }
        //모든 노드 생성 후 선택한 층 [개수 제한].
        LimitFloorNodeCount(0, startNodeCountLimit);
        LimitFloorNodeCount(maxFloor - 3, eliteNodeCountLimit);
        LimitFloorNodeCount(maxFloor - 1, bossNodeCountLimit);


        CollectStartNodes();
    }

    /// <summary>
    /// 지정한 층의 노드 개수 조정
    /// </summary>
    /// <param name="floor">층수 선택</param>
    /// <param name="limitCount">총 개수 선택</param>
    private void LimitFloorNodeCount(int floor, int limitCount)
    {

        List<NodeButton> activeNode = new List<NodeButton>();

        // 1. 해당 층에서 현재 활성 노드 수집
        for (int col = 0; col < maxColumn; col++)
        {
            NodeButton node = dungeonButtons[floor, col];

            if (node != null && node.isAvailable)
                activeNode.Add(node);
        }

        // 남길 개수보다 작으면 아무 작업 안함
        if (activeNode.Count <= limitCount)
            return;

        // 2. 남길 개수만큼 랜덤으로 선택
        List<NodeButton> randomSelecrtNode = new List<NodeButton>();
        for (int currentCount = 0; currentCount < limitCount; currentCount++)
        {
            //활성노드 중 하나 선택
            int selectNode = Random.Range(0, activeNode.Count);
            randomSelecrtNode.Add(activeNode[selectNode]);

            //선택된 노드는 activeNode리스트에서 제거하여 다시 선택 금지.
            activeNode.RemoveAt(selectNode);
        }

        // 3. 선택되지 않은 노드는 모두 None 처리
        foreach (NodeButton notChoiceNode in activeNode)
        {
            notChoiceNode.isAvailable = false;
            notChoiceNode.SetRoomType(Enums.RoomType.None, null);
        }
    }




    //-------------------------------------------------------------------------------------------------------

    //라인 생성부---------------------------------------------------------------------------------------------

    //노드 라인연결 로직.
    private void GenerateNodeConnections()
    {
        //startNodes안에있는 노드의 값을 startNode에 입력
        foreach (var entryNode in startNodes)
        {
            //현재 사용중인 노드 조작을 위해  NodeButton current 
            NodeButton currentNode = entryNode;
            //연속 노드 체크
            int straightCount = 1;


            //현재 노드의 floor으로 부터 maxFloor까지 순회하면서 체크
            for (int floor = currentNode.floor; floor < maxFloor - 1; floor++)
            {
                //다음 층 확인.
                int nextFloor = floor + 1;

                //다음 층의 연결 가능 노드 찾기(None노드 인가? 체크)
                NodeButton picked = FindNextAvailableNode(currentNode, nextFloor);
                if (picked == null) break;

                //선택한 노드와 현재 노드가 같은 열인가?
                bool isStraight = picked.col == currentNode.col;

                // 직진 3번 이상이면 강제 좌우 이동 아니면 초기화.
                if (straightCount >= 2 && isStraight)
                {
                    //GetSideCandidates를 이용해 다음 층(nextFloor)의 좌우 노드 값 가져오기.
                    var sides = GetSideCandidates(currentNode, nextFloor);

                    //좌우 노드가 한개라도 있으면  둘중하나 연결.
                    if (sides.Count > 0)
                    {
                        picked = sides[Random.Range(0, sides.Count)];
                        // 틀어졌으므로 연속연결 초기화
                        straightCount = 1;
                    }
                }
                else
                {
                    straightCount = isStraight ? straightCount + 1 : 1;
                }
                //안전 연결을 위한 코드.
                ConnectSafe(currentNode, picked);
                //다음 노드(picked)를 currentNode에 넣어 해당 과정 반복.
                currentNode = picked;
            }
        }

        ConnectIsolatedNodes();
    }

    /// <summary>
    /// 다음 층에서 노드 찾기
    /// </summary>
    /// <param name="currentNode">현재 노드</param>
    /// <param name="nextFloor">검색할 다음 층 </param>
    /// <returns></returns>
    private NodeButton FindNextAvailableNode(NodeButton currentNode, int nextFloor)
    {

        for (int checkRange = 1; checkRange < maxColumn; checkRange++)
        {
            //Column의 검색 구간 최소, 최대 범위 설정.
            int startCol = Mathf.Max(0, currentNode.col - checkRange);
            int endCol = Mathf.Min(maxColumn - 1, currentNode.col + checkRange);

            //범위 설정.
            NodeButton found = FindNodeWithinRange(nextFloor, startCol, endCol, currentNode.transform, true);
            if (found != null) return found;
        }
        return null;
    }


    private NodeButton FindNodeWithinRange(int targetFloor, int startCol, int endCol, Transform origin, bool useDistance)
    {
        //노드 
        NodeButton bestNode = null;
        float minDistSqr = float.MaxValue;

        if (targetFloor < 0 || targetFloor >= maxFloor)
            return null;

        // 랜덤 방향 (50%확률로 좌,우 -> 좌측 쏠림 완화.)
        bool searchLeftToRight = Random.value < 0.5f;

        //해당 층의 노드 연결 후보 평가.
        if (searchLeftToRight)
        {
            for (int columRange = startCol; columRange <= endCol; columRange++) EvaluateCandidate(columRange);
        }
        else
        {
            for (int columRange = endCol; columRange >= startCol; columRange--) EvaluateCandidate(columRange);
        }

        return bestNode;

        void EvaluateCandidate(int selectColum)
        {
            //다음층(targetFloor)의 해당 열의 후보노드가 쓸만한가?
            var candidateNode = dungeonButtons[targetFloor, selectColum];
            if (candidateNode == null || !candidateNode.isAvailable) return;

            // 거리계산 모드 체크 : 거리 계산을 사용하지 않는 모드일 경우, 첫 번째 유효 노드를 바로 선택 후 종료.
            if (!useDistance)
            {
                bestNode = candidateNode;
                return;
            }

            // 거리 비교 (sqrMagnitude)
            Vector2 delta = ((Vector2)candidateNode.transform.position - (Vector2)origin.position);
            float distSqr = delta.sqrMagnitude;

            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                bestNode = candidateNode;
            }
        }
    }

    // sideList를 미리 2개 지정해 놓아서 좌우Node체크용으로 재활용(생성 -> 제거 리소스 아끼기)
    private readonly List<NodeButton> sideList = new List<NodeButton>(2);

    private List<NodeButton> GetSideCandidates(NodeButton currentNode, int nextFloor)
    {
        //재활용하기 위해 Clear
        sideList.Clear();

        //현재 노드의 열을 넣고
        int colum = currentNode.col;
        //다음층 의 좌우 확인.
        if (colum - 1 >= 0)
        {
            var left = dungeonButtons[nextFloor, colum - 1];
            if (left != null && left.isAvailable) sideList.Add(left);
        }

        if (colum + 1 < maxColumn)
        {
            var right = dungeonButtons[nextFloor, colum + 1];
            if (right != null && right.isAvailable) sideList.Add(right);
        }

        return sideList;
    }

    //격리 노드 연결시키기.
    private void ConnectIsolatedNodes()
    {
        //모든 노드 순회하며 확인.
        for (int floor = 0; floor < maxFloor; floor++)
        {
            for (int colum = 0; colum < maxColumn; colum++)
            {

                NodeButton checkNode = dungeonButtons[floor, colum];

                //현재 노드가 없으면 지나가기.
                if (checkNode == null || !checkNode.isAvailable) continue;

                //현재 노드가 시작노드보다 높고 이전노드 연결이 없을 경우.
                if (checkNode.prevNodes.Count == 0 && floor > 0)
                {
                    NodeButton prev = FindNearestNode(floor, colum, true);
                    if (prev != null) ConnectSafe(prev, checkNode);
                }

                //현재 노드가 보스방직전보다 낮고고 다음노드와 연결이 없을 경우.
                if (checkNode.nextNodes.Count == 0 && floor < maxFloor - 1)
                {
                    NodeButton next = FindNearestNode(floor, colum, false);
                    if (next != null) ConnectSafe(checkNode, next);
                }
            }
        }
    }

    /// <summary>
    /// 가까운 노드 검색 공용함수 (위, 아래 양방향)
    /// </summary>
    /// <param name="floor">가까운 노드의 층</param>
    /// <param name="col">가까운 노드의 열</param>
    /// <param name="isPrev"> [True이면 Pre floor체크,False이면 Next floor체크] => 리소스 아끼기 위해 Bool사용</param>
    /// <returns></returns>
    private NodeButton FindNearestNode(int floor, int col, bool isPrev)
    {
        int targetFloor = isPrev ? floor - 1 : floor + 1;

        //노드들이 범위를 벗어나면 Null
        if (targetFloor < 0 || targetFloor >= maxFloor) return null;

        //내부에 있으면 origin위치 설정.
        NodeButton origin = dungeonButtons[floor, col];
        if (origin == null) return null;

        return FindNodeWithinRange(targetFloor, 0, maxColumn - 1, origin.transform, true);
    }

    private void ConnectSafe(NodeButton currentNode, NodeButton nextNode)
    {
        //둘 중 하나라도 null이면 함수 종료.
        if (currentNode == null || nextNode == null) return;

        //[중복연결 방지]
        // currentNode노드의 다음 연결nextNode가 이미 있는지 확인.
        if (!currentNode.nextNodes.Contains(nextNode)) currentNode.nextNodes.Add(nextNode);
        //'nextNode' 노드의 이전 연결(prevNodes)에 'currentNode'이 없다면 추가.
        if (!nextNode.prevNodes.Contains(currentNode)) nextNode.prevNodes.Add(currentNode);
    }

    //이펙트 연출을 위해서 던전메이커의 테마 스프라이트 던지기.
    public Sprite GetThemeSprite(Enums.DungeonTheme theme)
    {
        if (themeSpriteDictionary != null &&
            themeSpriteDictionary.TryGetValue(theme, out var sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[DungeonMaker] Theme Sprite not found: {theme}");
        return null;
    }


    // 내부 지도 Type 콘솔로 확인.
    private void PrintDungeonToConsole()
    {
        Debug.Log("===== Dungeon Result =====");
        for (int floor = 0; floor < maxFloor; floor++)
        {
            string line = $"Floor {floor}: ";
            for (int col = 0; col < maxColumn; col++)
            {
                NodeButton nb = dungeonButtons[floor, col];
                line += (nb.isAvailable ? nb.CurrentRoomType.ToString() : "None").PadRight(8);
            }
            Debug.Log(line);
        }
    }
}
