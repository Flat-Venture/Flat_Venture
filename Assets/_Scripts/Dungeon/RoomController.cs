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

    //방이 클리어되었을 때 스테이지 매니터에게 알리는 콜백
    public Action<RoomController> onRoomCleared;

    private List<MonsterController> activeMonsters = new List<MonsterController>();
    private bool isRoomCleared = false;

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
        Debug.Log($"<color=cyan>[RoomController]</color> 방 클리어! 다음 노드로 진행 가능.");
        onRoomCleared?.Invoke(this);
    }
}
