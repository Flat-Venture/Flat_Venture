using System.Collections.Generic;
using UnityEngine;
using FlatVenture.Enums;
using FlatVenture.Reward;

/// <summary>
/// 개별 방 프리팹의 루트에 부착되어 몬스터 스폰과 클리어 상태를 관리
/// </summary>
public class RoomController : MonoBehaviour, IDungeonRoomController
{
    [Header("Room Settings")]
    [Tooltip("플레이어가 이 방에 들어왔을 때 시작할 위치")]
    public Transform playerSpawnPoint;

    [Tooltip("몬스터들이 생성될 위치")]
    public Transform[] monsterSpawnPoints;

    [Tooltip("이 방에서 등장할 몬스터 프리팹")]
    public GameObject[] monsterPrefabs;

    [Header("Elite Random Traits")]
    [Tooltip("엘리트 방일 경우 몬스터에게 무작위로 달아줄 특성 프리팹들")]
    public GameObject[] randomEliteTraitPrefabs;

    [Header("Portal")]
    [Tooltip("포탈 생성/저장 체크포인트 호출을 담당하는 공용 생성기")]
    [SerializeField] private PortalSpawner portalSpawner;

    //현재 방 타입
    public RoomType currentRoomType;
    public int currentNodeId;

    private List<MonsterController> activeMonsters = new List<MonsterController>();
    private bool isRoomCleared = false;
    private DungeonRoomContext roomContext;

    public Transform PlayerSpawnPoint
    {
        get { return playerSpawnPoint; }
    }

    // StageManager가 현재 노드 정보를 전달합니다.
    // 이 컨텍스트 안의 콜백을 통해 포탈 생성/방 클리어 이벤트가 다시 StageManager로 돌아갑니다.
    public void InitializeRoom(DungeonRoomContext context)
    {
        if (context == null)
        {
            return;
        }

        currentRoomType = context.RoomType;
        currentNodeId = context.NodeId;
        roomContext = context;
        EnsurePortalSpawner();
    }
    
    //매니저에서 방을 세팅할 때 호출하는 시작 지점
    public void StartRoomEvent(bool startCleared = false)
    {
        if (startCleared)
        {
            // 포탈 체크포인트에서 이어하기를 하면 몬스터 스폰 없이 클리어 상태만 복원합니다.
            ClearRoom();
            return;
        }

        Invoke(nameof(SpawnMonsters), 0.1f);
    }

    private void SpawnMonsters()
    {
        for (int i = 0; i < monsterSpawnPoints.Length; i++)
        {
            if (i >= monsterPrefabs.Length) break;

            GameObject monsterObject = Instantiate(monsterPrefabs[i], monsterSpawnPoints[i].position, Quaternion.identity, transform);
            MonsterController monster = monsterObject.GetComponent<MonsterController>();
            
            if (monster != null)
            {
                activeMonsters.Add(monster);

                //만약 현재 방이 엘리트 방이고, 설정해둔 특성 프리팹이 있다면 무작위로 하나를 골라 부착
                if (currentRoomType == RoomType.Elite && randomEliteTraitPrefabs != null && randomEliteTraitPrefabs.Length > 0)
                {
                    int randIdx = UnityEngine.Random.Range(0, randomEliteTraitPrefabs.Length);
                    
                    //몬스터를 부모로 하여 특성 오브젝트 생성
                    GameObject traitObj = Instantiate(randomEliteTraitPrefabs[randIdx], monster.transform);
                    MonsterTrait extraTrait = traitObj.GetComponent<MonsterTrait>();
                    
                    if (extraTrait != null)
                    {
                        monster.AttachDynamicTrait(extraTrait);
                    }
                }

                //몬스터 코어의 onDeathCallback을 구독하여 죽음을 감지
                monster.onDeathCallback += CheckRoomClear;
            }
        }

        //스폰될 몬스터가 없는 방이거나 모두 스폰했는데 0마리라면 즉시 클리어
        if (activeMonsters.Count == 0) ClearRoom();
    }

    private void CheckRoomClear(MonsterController deadMonster)
    {
        //이벤트 구독 해제 및 리스트에서 제거
        deadMonster.onDeathCallback -= CheckRoomClear;
        activeMonsters.Remove(deadMonster);

        //남은 몬스터가 없으면 클리어
        if (activeMonsters.Count == 0 && !isRoomCleared) ClearRoom();
    }

    private void ClearRoom()
    {
        isRoomCleared = true;
        Debug.Log($"<color=cyan>[RoomController]</color> 방 클리어. 포탈을 생성합니다.");
        
        EnsurePortalSpawner();
        if (portalSpawner != null)
        {
            // 전투방은 포탈이 생성되는 순간을 체크포인트로 저장합니다.
            portalSpawner.SetSaveWhenPortalGenerated(true);
            portalSpawner.Initialize(roomContext);

            if (portalSpawner.SpawnPortal())
            {
                if (ShouldShowReward())
                {
                    // 보상 선택 UI는 포탈 생성 이후에 표시되므로, 선택 결과는 즉시 저장하지 않습니다.
                    DungeonRewardSelectionController.ShowRewards(currentRoomType, currentNodeId);
                }

                return;
            }
        }

        Debug.LogWarning("포탈 프리팹이 연결되지 않아 즉시 지도를 엽니다.");
        if (roomContext != null)
        {
            roomContext.NotifyRoomCleared();
        }
    }

    private void EnsurePortalSpawner()
    {
        if (portalSpawner == null)
        {
            // 기존 전투방 프리팹에 직접 연결하지 못한 경우를 대비해 자식에서도 찾습니다.
            portalSpawner = GetComponentInChildren<PortalSpawner>(true);
        }
    }

    // 일반/엘리트/보스 전투방만 아이템 보상 선택 UI를 표시합니다.
    private bool ShouldShowReward()
    {
        return currentRoomType == RoomType.Normal
            || currentRoomType == RoomType.Elite
            || currentRoomType == RoomType.Boss;
    }
}
