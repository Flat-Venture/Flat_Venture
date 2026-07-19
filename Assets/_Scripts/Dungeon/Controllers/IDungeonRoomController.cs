using UnityEngine;

// StageManager가 전투/휴식/상점/제련소 방을 같은 방식으로 다루기 위한 공통 계약입니다.
public interface IDungeonRoomController
{
    Transform PlayerSpawnPoint { get; }

    // StageManager가 노드 정보와 진행 콜백을 방 컨트롤러에 전달합니다.
    void InitializeRoom(DungeonRoomContext context);

    // 방 입장 직후 실행됩니다. 포탈 체크포인트에서 시작하면 true가 전달됩니다.
    void StartRoomEvent(bool startFromPortalCheckpoint = false);
}
