using System;
using System.Collections.Generic;
using UnityEngine;
using FlatVenture.Enums;

/// <summary>
/// 개별 방 프리팹의 루트에 부착되어 몬스터 스폰과 클리어 상태를 관리
/// </summary>
public class RoomController : MonoBehaviour
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

    [Header("Reward & Portal")]
    [Tooltip("전투 종료 시 생성될 포탈 프리팹")]
    public GameObject portalPrefab;

    //현재 방 타입
    public RoomType currentRoomType;

    //방이 클리어되었을 때 스테이지 매니터에게 알리는 콜백
    public Action<RoomController> onRoomCleared;

    private List<MonsterController> activeMonsters = new List<MonsterController>();
    private bool isRoomCleared = false;
    
    //매니저에서 방을 세팅할 때 호출하는 시작 지점
    public void StartRoomEvent()
    {
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
        
        if (portalPrefab != null)
        {
            //방 중앙(원점) 또는 특정 위치에 포탈 생성
            GameObject portalObj = Instantiate(portalPrefab, transform.position, Quaternion.identity, transform);
            Portal portal = portalObj.GetComponent<Portal>();

            if (portal != null)
            {
                //포탈을 타면 onRoomCleared 콜백이 실행되도록 연결
                portal.onPortalEntered = () => 
                {
                    onRoomCleared?.Invoke(this); //이게 실행되면 StageManager가 지도를 염
                };
            }
        }
        else
        {
            Debug.LogWarning("포탈 프리팹이 연결되지 않아 즉시 지도를 엽니다.");
            onRoomCleared?.Invoke(this);
        }
    }
}
