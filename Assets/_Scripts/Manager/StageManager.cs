using UnityEngine;
using System;
using FlatVenture.Enums;
using FlatVenture.SaveLoad;

/// <summary>
/// 진입한 맵 노드의 RoomType에 맞춰 실제 게임 스테이지를 세팅하고 관리
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player;

    [Header("Room Prefabs")]
    public GameObject normalRoomPrefab;
    public GameObject eliteRoomPrefab;
    public GameObject restRoomPrefab;
    public GameObject forgeRoomPrefab;
    public GameObject shopRoomPrefab;
    public GameObject unknownRoomPrefab;
    public GameObject bossRoomPrefab;

    [Header("UI Settings")]
    public GameObject mapCanvas; //맵 캔버스를 On/Off 하기 위한 변수

    private RoomController currentActiveRoom;
    private Action openMapCallback;
    private MapNode currentNode; //방금 입장한 노드를 기억해둘 변수

    /// <summary>
    /// MapTestRunner에서 호출하여 지도를 다시 여는 콜백 함수
    /// </summary>
    public void Init(Action openMapCallback)
    {
        this.openMapCallback = openMapCallback;
    }

    /// <summary>
    /// MapNode 클릭 시 호출되어 해당 방을 생성하고 입장
    /// </summary>
    /// <param name="node"></param>
    public void EnterStage(MapNode node)
    {
        //입장할 때 어떤 노드인지 기억
        this.currentNode = node;

        //맵 UI를 끄고 게임 씬으로 진입
        if (mapCanvas != null) mapCanvas.SetActive(false);

        //기존 활성화된 방이 있으면 메모리에서 제거
        if (currentActiveRoom != null) Destroy(currentActiveRoom.gameObject);

        //노드의 RoomType에 따라 방 프리팹을 생성
        GameObject roomPrefabToSpawn = GetRoomPrefab(node.RoomType);

        if (roomPrefabToSpawn != null)
        {
            //씬의 원점(0, 0, 0)에 새로운 방 생성
            GameObject newRoomObject = Instantiate(roomPrefabToSpawn, Vector3.zero, Quaternion.identity);
            currentActiveRoom = newRoomObject.GetComponent<RoomController>();

            if (currentActiveRoom != null)
            {
                //포탈 탑승 시 호출될 이벤트를 구독
                currentActiveRoom.onRoomCleared += HandleRoomCleared;

                //충돌 문제를 방지하기 위해 CharacterController를 비활성화 후 플레이어 위치 이동
                if (currentActiveRoom.playerSpawnPoint != null && player != null)
                {
                    CharacterController characterController = player.GetComponent<CharacterController>();
                    if (characterController != null) characterController.enabled = false;

                    player.transform.position = currentActiveRoom.playerSpawnPoint.position;
                    if (characterController != null) characterController.enabled = true;
                }

                //방 세팅 끝. 몬스터 스폰 이벤트 실행
                currentActiveRoom.StartRoomEvent();
            }

            else
            {
                Debug.LogWarning("[StageManager] 생성된 방 프리팹에 RoomController 컴포넌트가 없습니다.");
            }

            //플레이어를 활성화하여 전투 시작
            if (player != null) player.SetActive(true);
        }

        else
        {
            Debug.LogWarning($"[StageManager] {node.RoomType}에 해당하는 프리팹이 등록되지 않았습니다.");
            HandleRoomCleared(null);
        }

    }

    /// <summary>
    /// 전달받은 Enum 값에 대응하는 프리팹을 매핑
    /// </summary>
    private GameObject GetRoomPrefab(RoomType type)
    {
        switch (type)
        {
            case RoomType.Normal: return normalRoomPrefab;
            case RoomType.Elite: return eliteRoomPrefab;
            case RoomType.Rest: return restRoomPrefab;
            case RoomType.Forge: return forgeRoomPrefab;
            case RoomType.Shop: return shopRoomPrefab;
            case RoomType.Unknown: return unknownRoomPrefab;
            case RoomType.Boss: return bossRoomPrefab;
            default: return null;
        }
    }

    private void HandleRoomCleared(RoomController clearedRoom)
    {
        if (clearedRoom != null) 
        {
            //이벤트 구독 해제
            clearedRoom.onRoomCleared -= HandleRoomCleared;

            //포탈을 탔으므로 던전 방 오브젝트를 완전 파괴
            Destroy(clearedRoom.gameObject);
            if (currentActiveRoom == clearedRoom) currentActiveRoom = null;
        }

        Debug.Log("<color=green>[Core Loop]</color> 포탈 탑승 및 방 정리 완료! 지도로 복귀합니다.");

        //플레이어 다시 끄기 (또는 무적/대기 상태로 전환)
        if (player != null) player.SetActive(false);

        //맵 UI 다시 켜기
        if (mapCanvas != null) mapCanvas.SetActive(true);

        //포탈을 타고 지도로 돌아왔을 때 들고 있던 노드 정보를 확인/전달
        if (currentNode != null)
        {
            Debug.Log($"<color=yellow>[Save Data 준비완료]</color> 현재 층: {currentNode.Floor}, 노드 ID: {currentNode.NodeID}");
            DungeonMapSaveBridge.SaveRoomCleared(currentNode);
        }

        //다시 맵을 열기 위한 델리게이트 실행
        openMapCallback?.Invoke();
    }
}
