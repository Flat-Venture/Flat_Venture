using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DungeonManager;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("현재 던전 저장 데이터")]
    public DungeonMapData currentDungeonData;

    [Header("현재 테마")]
    public Enums.DungeonTheme currentTheme;
    public Enums.DungeonTheme nextTheme;

    [Header("현재 선택된 룸 타입")]
    public Enums.RoomType currentRoomType;


    [Header("플레이어 현재 장소")]
    public Enums.currentPlayerPlace currentPlayerPlace;

    [Header("유저 경험치용 변수")]
    public float userEXPClearedRoomCount = 0;
    private const float USER_EXP_PER_ROOM = 50;

    //현재 전투중인 노드 위치.
    public int currentBattleNodeFloor;
    public int currentBattleNodeColum;

    [System.Serializable]
    public struct NextNodeLinkData
    {
        public int floor;      // 다음 노드 층
        public int column;     // 다음 노드 열
    }

    [Header("Stage Info")]
    private int currentStage = 1;   // 1부터
    private int maxStage = 2;       // 1~3 가변

    //스테이지 변경시 이용할 연출용 변수.
    private bool isStageTransitionPending = false;
    public bool needStageTransitionEffect = false;

    //싱글톤 및 테마 설정.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetRandomTheme(); // 첫 진입 시 테마 설정
        Debug.Log($"[Portal] Boss clear BEFORE OnStageCleared: " +
                     $"currentStage={currentStage}, maxStage={maxStage}, currentteme={currentTheme}");
    }

    //테마 관련--------------------------------------------------------------------------------------------------------
    private void SetRandomTheme()
    {
        // 전체 테마 목록 가져와 배열로 저장.
        Enums.DungeonTheme[] themes = (Enums.DungeonTheme[])System.Enum.GetValues(typeof(Enums.DungeonTheme));

        //List형 사용가능 테마 목록 준비
        List<Enums.DungeonTheme> filteringTheme = new List<Enums.DungeonTheme>();

        //Enums의 테마들 중 None을 제외하고 필터링.
        foreach (var theme in themes)
        {
            if (theme != Enums.DungeonTheme.None)
            {
                filteringTheme.Add(theme);
            }
        }

        //랜덤 테마 선정.
        int randomIndex = Random.Range(0, filteringTheme.Count);
        currentTheme = filteringTheme[randomIndex];
        nextTheme = currentTheme;
        Debug.Log("[DungeonManager] 초기 테마 선택: " + currentTheme);

    }

    // (외부 참조용) 현재 정해진 테마 값 반환.
    public Enums.DungeonTheme GetCurrentTheme()
    {
        return currentTheme;
    }


    public void SetNextTheme()
    {
        // 전체 테마 목록 가져와 배열로 저장.
        Enums.DungeonTheme[] themes = (Enums.DungeonTheme[])System.Enum.GetValues(typeof(Enums.DungeonTheme));

        //List형 사용가능 테마 목록 준비
        List<Enums.DungeonTheme> filteringThemes = new List<Enums.DungeonTheme>();

        //Enums의 테마들 중 None과 이미 사용한 테마 필터링.
        foreach (var theme in themes)
        {
            if (theme == Enums.DungeonTheme.None) continue;
            if (theme != currentTheme) filteringThemes.Add(theme);
        }


        // 예외 방지
        if (filteringThemes.Count == 0)
        {
            Debug.LogWarning("[DungeonManager] 사용 가능한 다른 테마가 없습니다.");
            nextTheme = currentTheme;
            return;
        }

        // 랜덤으로 선택
        int randomIndex = Random.Range(0, filteringThemes.Count);
        nextTheme = filteringThemes[randomIndex];

        Debug.Log("[DungeonManager] 새로운 테마 선택(다음): " + nextTheme);
    }


    //스테이지 관련--------------------------------------------------------------------------------------------------------

    //현재 스테이지가 마지막 스테이지 인지 True/False반환.
    public bool IsLastStage()
    {
        return currentStage >= maxStage;
    }

    public bool OnStageCleared()
    {

        Debug.Log($"[DungeonManager] Stage {currentStage} 클리어");


        //마지막 스테이지 클리어 IsLastStage(Bool)로 확인
        if (currentStage >= maxStage)
        {
            OnAllStagesCleared();
            return true;
        }

        currentStage++;
        EnterNextStage();
        SetNextTheme();

        // 연출 완료 대기 상태
        isStageTransitionPending = true;
        return false; // 다음 스테이지 있음
    }

    public void EnterNextStage()
    {
        //다음 테마 확정 (SetNextTheme가 nextTheme만 뽑는 구조라면 필수)
        currentTheme = nextTheme;

        //  새 맵 생성 유도
        currentDungeonData = null;
        isStageTransitionPending = false;
    }

    private void OnAllStagesCleared()
    {
        Debug.Log("[DungeonManager] 모든 스테이지 클리어");

        // 런 정리(맵데이터/인벤/플레이어/EXP 정산)
        ResetDungeonData();

        // 스테이지/전환 플래그 리셋
        currentStage = 1;

        isStageTransitionPending = false;
        needStageTransitionEffect = false;

        // 테마도 새로 시작 상태로 리셋 (싱글톤 유지 때문에 필수)
        SetRandomTheme();          // 내부 private 그대로 사용 가능 (같은 클래스 내부 호출)
        nextTheme = currentTheme;  // 다음테마도 안전하게 동기화
    }


    //--------------------------------------------------------------------------------------------------------
    // (외부 참조용) 현재 룸타입에 대하여 반환하기.
    public Enums.RoomType GetCurrentRoomType()
    {
        return currentRoomType;
    }

    //[플레이어의 던전 위치 체크]

    // (외부 참조용) 현재 플레이어 위치 반환
    public Enums.currentPlayerPlace GetCurrentPlayerPlace()
    {
        return currentPlayerPlace;
    }


    //(외부 참조용) 던전 내부
    public void EnterDungeon()
    {
        currentPlayerPlace = Enums.currentPlayerPlace.dungeonIn;
        Debug.Log("[DungeonManager] PlayerPlace 변경: dungeonIn");
    }
    //(외부 참조용) 던전 외부
    public void ExitDungeon()
    {
        currentPlayerPlace = Enums.currentPlayerPlace.dungeonOut;
        Debug.Log("[DungeonManager] PlayerPlace 변경: dungeonOut");
    }


    // 룸 타입 값 설정(현재 마우스 클릭에서 사용되는 함수.)
    public void SetCurrentRoomType(Enums.RoomType roomType)
    {
        currentRoomType = roomType;
        Debug.Log("[DungeonManager] 현재 룸 타입 설정: " + currentRoomType);
    }

    //노드 데이터 보유-------------------------------------------------------------------------

    // 새로운 던전 데이터를 저장 (Maker에서 전달받음)
    public void SaveDungeonData(DungeonMapData data)
    {
        currentDungeonData = data;
        Debug.Log("[DungeonManager] 던전 데이터 저장 완료");
    }

    // 현재 던전 데이터를 반환
    public DungeonMapData GetDungeonData()
    {
        return currentDungeonData;
    }

    // 현재 던전 데이터가 존재하는가?
    public bool HasDungeonData()
    {
        return currentDungeonData != null && currentDungeonData.nodes.Count > 0;
    }

    // 클리어, 포기, 종료 시 초기화 (모든 것을 끝내고 Town으로 돌아올 때.)
    public void ResetDungeonData()
    {
        Debug.Log("[DungeonManager] ResetDungeonData CALLED");
        currentDungeonData = null;
        //타운 쪽으로 들어올 때 타입 초기화.
        SetCurrentRoomType(Enums.RoomType.None);
        SetRandomTheme();

        InventoryManager.Instance.ClearInventory();
        Player.Instance.ResetPlayer();
        //마무리 시 유저 경험치 적용 및 카운팅 최소화.
        UserDataManager.Instance.AddUserExp(userEXPClearedRoomCount * USER_EXP_PER_ROOM);
        userEXPClearedRoomCount = 0;

        Debug.Log("[DungeonManager] 던전 데이터 초기화 완료");
    }

    //현재 전투하는 노드 위치 기록.
    public void SetCurrentNode(int floor, int colum)
    {
        currentBattleNodeFloor = floor;
        currentBattleNodeColum = colum;
    }

    public void dungeonClearSignal()
    {
        if (currentDungeonData == null) return;

        //현재 저장된 던전의 전체 노드의 데이터들 검색.
        foreach (var node in currentDungeonData.nodes)
        {
            //클릭해서 들어 갔던 노드인지 확인.
            if (node.floor == currentBattleNodeFloor && node.col == currentBattleNodeColum)
            {
                //클리어한 노드 true를 통한 클리어 처리.
                node.isCleared = true;
                userEXPClearedRoomCount++;
                Debug.Log($"[DungeonManager] 클리어 룸 카운트: {userEXPClearedRoomCount}");
                Debug.Log($"[DungeonManager] 노드 클리어: {node.floor},{node.col}");
                return;
            }
        }
    }

    //해당 부분은 실사용시 삭제---------------------------------------------
    void Update()
    {
        //체크용 씬 으로 넘어가기
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            SceneManager.LoadScene("MapDateCheckScene");
        }

        //맵씬으로 이동.
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            SceneManager.LoadScene("02_DungeonMap");
        }
    }
    //해당 부분은 실사용시 삭제---------------------------------------------

}

[System.Serializable]
public class DungeonMapData
{
    //던전 기본 정보.
    public int maxFloor;
    public int maxColumn;
    public Enums.DungeonTheme theme;

    //[각 노드별 데이터 정보]를 리스트 형식으로 저장.
    public List<DungeonNodeData> nodes = new List<DungeonNodeData>();
}

[System.Serializable]
public class DungeonNodeData //각 노드별 데이터 정보
{
    //기본 노드 정보.
    public int floor;
    public int col;
    public Enums.RoomType roomType;
    public bool isAvailable;

    //해당 노드 클리어 여부.
    public bool isCleared;

    //노드의 좌표정보(Centor)
    public Vector2 uiPosition;
    //다음 노드 정보를 리스트 형식으로 저장.
    public List<NextNodeLinkData> nextNodesLink = new List<NextNodeLinkData>();
}