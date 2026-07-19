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

    private IDungeonRoomController currentActiveRoom;
    private GameObject currentActiveRoomObject;
    private Action openMapCallback;
    private MapNode currentNode; //방금 입장한 노드를 기억해둘 변수

    public bool IsInActiveRoom
    {
        get { return currentActiveRoomObject != null; }
    }

    public bool IsInCombatRoom
    {
        get
        {
            if (currentActiveRoomObject == null || currentNode == null)
            {
                return false;
            }

            return currentNode.RoomType == RoomType.Normal
                || currentNode.RoomType == RoomType.Elite
                || currentNode.RoomType == RoomType.Boss;
        }
    }

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
    public void EnterStage(MapNode node, bool startFromPortalCheckpoint = false)
    {
        //입장할 때 어떤 노드인지 기억
        this.currentNode = node;

        //맵 UI를 끄고 게임 씬으로 진입
        if (mapCanvas != null) mapCanvas.SetActive(false);

        //기존 활성화된 방이 있으면 메모리에서 제거
        if (currentActiveRoomObject != null) Destroy(currentActiveRoomObject);

        //노드의 RoomType에 따라 방 프리팹을 생성
        GameObject roomPrefabToSpawn = GetRoomPrefab(node.RoomType);

        if (roomPrefabToSpawn != null)
        {
            //씬의 원점(0, 0, 0)에 새로운 방 생성
            GameObject newRoomObject = Instantiate(roomPrefabToSpawn, Vector3.zero, Quaternion.identity);
            currentActiveRoomObject = newRoomObject;
            currentActiveRoom = newRoomObject.GetComponent<IDungeonRoomController>();

            if (currentActiveRoom != null)
            {
                // 방 컨트롤러가 직접 StageManager를 참조하지 않도록, 필요한 진행 콜백만 컨텍스트로 전달합니다.
                var context = new DungeonRoomContext(node.RoomType, node.NodeID, node, HandleRoomCleared, HandlePortalGenerated);
                currentActiveRoom.InitializeRoom(context);

                //충돌 문제를 방지하기 위해 CharacterController를 비활성화 후 플레이어 위치 이동
                if (currentActiveRoom.PlayerSpawnPoint != null && player != null)
                {
                    CharacterController characterController = player.GetComponent<CharacterController>();
                    if (characterController != null) characterController.enabled = false;

                    player.transform.position = currentActiveRoom.PlayerSpawnPoint.position;
                    if (characterController != null) characterController.enabled = true;
                }

                //방 세팅 끝. 방 타입별 시작 이벤트 실행
                currentActiveRoom.StartRoomEvent(startFromPortalCheckpoint);
            }

            else
            {
                Debug.LogWarning("[StageManager] 생성된 방 프리팹에 IDungeonRoomController 컴포넌트가 없습니다.");
            }

            //플레이어를 활성화하여 방 진행 시작
            if (player != null) player.SetActive(true);
        }

        else
        {
            Debug.LogWarning($"[StageManager] {node.RoomType}에 해당하는 프리팹이 등록되지 않았습니다.");
            HandleRoomCleared();
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

    private void HandleRoomCleared()
    {
        if (currentActiveRoomObject != null)
        {
            //포탈을 탔으므로 던전 방 오브젝트를 완전 파괴
            Destroy(currentActiveRoomObject);
            currentActiveRoomObject = null;
            currentActiveRoom = null;
        }

        Debug.Log("<color=green>[Core Loop]</color> 포탈 탑승 및 방 정리 완료! 지도로 복귀합니다.");

        //플레이어 다시 끄기 (또는 무적/대기 상태로 전환)
        if (player != null) player.SetActive(false);

        //맵 UI 다시 켜기
        if (mapCanvas != null) mapCanvas.SetActive(true);

        //다시 맵을 열기 위한 델리게이트 실행
        // 포탈 탑승 후 다음 노드를 선택할 수 있는 지도 화면으로 돌아갑니다.
        openMapCallback?.Invoke();
    }

    private void HandlePortalGenerated()
    {
        if (currentNode == null)
        {
            return;
        }

        Debug.Log($"<color=yellow>[Save Data 준비완료]</color> 포탈 생성 저장. 현재 층: {currentNode.Floor}, 노드 ID: {currentNode.NodeID}");
        // 전투방처럼 포탈 생성 시점이 체크포인트인 방에서만 호출됩니다.
        // 특수 방은 PortalSpawner에서 이 호출을 끄고, 다음 노드 선택 시점에 저장합니다.
        DungeonMapSaveBridge.SavePortalGenerated(currentNode);
    }
}
