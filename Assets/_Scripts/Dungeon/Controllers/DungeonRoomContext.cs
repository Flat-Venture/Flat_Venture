using System;
using FlatVenture.Enums;

// StageManager가 방 컨트롤러에 전달하는 현재 노드/방 진행 정보입니다.
public sealed class DungeonRoomContext
{
    public DungeonRoomContext(RoomType roomType, int nodeId, MapNode currentNode, Action roomCleared, Action portalGenerated)
    {
        RoomType = roomType;
        NodeId = nodeId;
        CurrentNode = currentNode;
        RoomCleared = roomCleared;
        PortalGenerated = portalGenerated;
    }

    public RoomType RoomType { get; private set; }
    public int NodeId { get; private set; }
    public MapNode CurrentNode { get; private set; }
    // StageManager.HandleRoomCleared로 이어지는 콜백입니다.
    public Action RoomCleared { get; private set; }

    // StageManager.HandlePortalGenerated로 이어지는 콜백입니다.
    public Action PortalGenerated { get; private set; }

    // 포탈 탑승처럼 방이 완전히 끝났을 때 호출합니다.
    public void NotifyRoomCleared()
    {
        if (RoomCleared != null)
        {
            RoomCleared.Invoke();
        }
    }

    // 포탈이 생성되어 체크포인트 저장이 필요할 때 호출합니다.
    public void NotifyPortalGenerated()
    {
        if (PortalGenerated != null)
        {
            PortalGenerated.Invoke();
        }
    }
}
