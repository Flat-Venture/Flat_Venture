using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using static Enums; // RoomType Enum 접근

public class Portal : MonoBehaviour
{
    [Serializable]
    public struct RoomSettings
    {
        public Enums.RoomType roomType;
        [Tooltip("SceneNames 클래스에 정의된 변수명을 입력하세요.")]
        public string sceneFieldName;

        [Header("옵션 설정")]
        [Tooltip("체크 시 해당 노드를 클리어 처리합니다.")]
        public bool isClear;
        [Tooltip("체크 시 던전 데이터를 초기화하고 플레이어/인벤토리를 리셋합니다.")]
        public bool isReset;
    }

    [Header("방 타입별 상세 설정")]
    public RoomSettings[] roomSettingsList;

    private int playerLayer;
    private bool isPlayerInZone = false; // 플레이어가 포탈 범위 내에 있는지 확인
    private bool isTransitioning = false; // 씬 전환 중 중복 호출 방지 플래그

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1)
        {
            Debug.LogError("Portal: 'Player' 레이어가 존재하지 않습니다.");
        }
    }

    private void Update()
    {
        // 플레이어가 포탈 안에 있고, 엔터 키를 눌렀으며, 현재 이동 중이 아닐 때 실행
        if (isPlayerInZone && !isTransitioning && Input.GetKeyDown(KeyCode.Return))
        {
            TryMoveToNextScene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트의 최상위 루트가 Player 레이어인지 확인
        if (other.transform.root.gameObject.layer == playerLayer)
        {
            isPlayerInZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 포탈 밖으로 나가면 상태 해제
        if (other.transform.root.gameObject.layer == playerLayer)
        {
            isPlayerInZone = false;
        }
    }

    private void TryMoveToNextScene()
    {
        // 중복 실행 방지
        isTransitioning = true;

        SoundManager.Instance.PlaySFX("moveToDungeonMap");

        // 마을에서 출발할 때 아이템 지급 로직 (기존 로직 유지)
        if (SceneLoaderManager.Instance.GetCurrentSceneName() == SceneNames.Town)
        {
            for (int i = 0; i < Player.Instance.finalStats.startItemCount; i++)
            {
                InventoryManager.Instance.AddItemToInventory(
                    ItemManager.Instance.GetRandomItemDataByRank(
                        ItemManager.Instance.GetRandomItemRank()));
            }
        }

        HandlePortalLogic();
    }

    private void HandlePortalLogic()
    {
        DungeonManager dungeonManager = DungeonManager.Instance;
        if (dungeonManager == null)
        {
            isTransitioning = false;
            return;
        }

        Enums.RoomType currentType = dungeonManager.GetCurrentRoomType();
        RoomSettings settings = GetSettingsForType(currentType);

        if (settings.isClear)
        {
            dungeonManager.dungeonClearSignal();
            Debug.Log($"Portal: {currentType} 노드 클리어 처리 완료.");
        }

        // 보스 + 리셋 처리
        if (settings.isReset && currentType == Enums.RoomType.Boss)
        {
            bool isAllStagesCleared = dungeonManager.OnStageCleared();
            dungeonManager.needStageTransitionEffect = !isAllStagesCleared;

            MoveToScene(isAllStagesCleared ? "Town" : "DungeonMap");
            return;
        }

        // 일반 이동
        if (!string.IsNullOrEmpty(settings.sceneFieldName))
        {
            MoveToScene(settings.sceneFieldName);
        }
        else
        {
            // 이동할 씬 설정이 없을 경우 다시 입력 가능하도록 해제
            isTransitioning = false;
        }
    }

    private RoomSettings GetSettingsForType(Enums.RoomType type)
    {
        foreach (var setting in roomSettingsList)
        {
            if (setting.roomType == type) return setting;
        }

        // 매핑 정보가 없을 경우 기본값 반환
        return new RoomSettings { sceneFieldName = "DungeonMap", isClear = false, isReset = false };
    }

    private void MoveToScene(string targetField)
    {
        var field = typeof(SceneNames).GetField(targetField);
        string actualSceneName = (field != null) ? field.GetValue(null).ToString() : targetField;

        // 포탈로 넘어가는 곳은 DungeonMap 밖에 없음 => 넘어갈 때, 룸타입 변경해줌으로써 BGM 재생
        DungeonManager.Instance.SetCurrentRoomType(RoomType.None);

        // 인벤토리 모드 None으로 되돌리기
        if (ModeManager.Instance.GetCurrentMode() != InventoryMode.None)
        {
            ModeManager.Instance.SetMode(InventoryMode.None);
        }

        if (SceneLoaderManager.Instance != null)
        {
            SceneLoaderManager.Instance.LoadScene(actualSceneName);
        }
        else
        {
            SceneManager.LoadScene(actualSceneName);
        }

        // 씬 로드가 시작되었으므로 Update에서 더 이상 로직이 타지 않음
        // (실제 새로운 씬이 로드되면 이 오브젝트는 파괴될 것이므로 true 상태로 유지해도 무방함)
    }
}