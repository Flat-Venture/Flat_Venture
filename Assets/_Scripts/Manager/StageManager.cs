using UnityEngine;
using System;
using FlatVenture.Enums;

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

    private RoomController currentActiveRoom;
    private Action openMapCallback;

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
        //기존 활성화된 방이 있으면 메모리에서 제거
        if (currentActiveRoom != null) Destroy(currentActiveRoom.gameObject);

        //노드의 RoomType에 따라 방 프리팹을 생성
        GameObject roomPrefabToSpawn = GetRoomPrefab(node.RoomType);

        if (roomPrefabToSpawn != null)
        {
            //씬의 원점(0, 0, 0)에 새로운 방 생성
            GameObject newRoomObject = Instantiate(roomPrefabToSpawn, Vector3.zero, Quaternion.identity);
            currentActiveRoom = newRoomObject.GetComponent<RoomController>();

            //충돌 문제를 방지하기 위해 CharacterController를 비활성화 후 플레이어 위치 이동
            if (currentActiveRoom.playerSpawnPoint != null && player !=null)
            {
                CharacterController characterController = player.GetComponent<CharacterController>();
                if (characterController != null) characterController.enabled = false;

                player.transform.position = currentActiveRoom.playerSpawnPoint.position;
                if (characterController != null) characterController.enabled = true;
            }
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
        if (clearedRoom != null) clearedRoom.onRoomCleared -= HandleRoomCleared;

        //다시 맵을 열기 위한 델리게이트 실행
        openMapCallback?.Invoke();
    }
}