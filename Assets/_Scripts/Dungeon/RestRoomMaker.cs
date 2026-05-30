using UnityEngine;
using static Enums;

// 던전 매니저의 RoomType 설정에 따라 휴게실(상점, 휴식, 강화) 오브젝트를 활성화하는 스크립트
public class RestRoomMaker : MonoBehaviour
{
    [Header("룸 타입별 오브젝트 그룹")]
    public GameObject shopGroup;  // 상점 관련 오브젝트
    public GameObject restGroup;  // 휴식 관련 오브젝트
    public GameObject forgeGroup; // 강화 관련 오브젝트

    [Header("예외 처리용 기본 설정")]
    [Tooltip("DungeonManager 참조 실패 시 기본으로 보여줄 타입")]
    public RoomType defaultTypeOnFailure = RoomType.Rest;

    private void Start()
    {
        // 던전 입장 상태임을 DungeonManager에 알림
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.EnterDungeon();
        }

        // 씬이 로드되면 DungeonManager로부터 현재 룸 타입을 받아와 적용합니다.
        ApplyRoomTypeFromManager();
    }

    /// <summary>
    /// DungeonManager에서 현재 룸 타입을 받아와 적용하거나, 실패 시 기본 타입을 적용합니다.
    /// </summary>
    public void ApplyRoomTypeFromManager()
    {
        RoomType typeToApply = RoomType.None;
        DungeonManager manager = DungeonManager.Instance;

        // 1. DungeonManager 인스턴스 확인
        if (manager != null)
        {
            typeToApply = manager.GetCurrentRoomType(); //
            Debug.Log($"[RestRoomMaker] DungeonManager에서 룸 타입 **{typeToApply}**를 받아왔습니다.");
        }
        else
        {
            Debug.LogError("[RestRoomMaker] DungeonManager.Instance를 찾을 수 없습니다. 기본 설정을 사용합니다.");
        }

        // 2. 유효하지 않은 타입일 경우(전투용 노드 등) 예외 처리
        if (typeToApply == RoomType.None || typeToApply == RoomType.Normal || typeToApply == RoomType.Elite || typeToApply == RoomType.Boss)
        {
            Debug.LogWarning($"[RestRoomMaker] 현재 타입({typeToApply})은 RestRoom 전용이 아닙니다. 기본값인 {defaultTypeOnFailure}로 설정합니다.");
            typeToApply = defaultTypeOnFailure;
        }

        // 3. 최종 오브젝트 활성화 실행
        ActivateRoomObject(typeToApply);
    }

    /// <summary>
    /// 선택된 룸 타입에 해당하는 그룹만 활성화하고 나머지는 비활성화합니다.
    /// </summary>
    private void ActivateRoomObject(RoomType type)
    {
        // 모든 그룹을 먼저 끕니다.
        DisableAllGroups();

        switch (type)
        {
            case RoomType.Shop:
                if (shopGroup) shopGroup.SetActive(true);
                break;

            case RoomType.Rest:
                if (restGroup) restGroup.SetActive(true);
                break;

            case RoomType.Forge:
                if (forgeGroup) forgeGroup.SetActive(true);
                break;
        }

        Debug.Log($"[RestRoomMaker] {type} 관련 오브젝트를 활성화했습니다.");
    }

    private void DisableAllGroups()
    {
        if (shopGroup) shopGroup.SetActive(false);
        if (restGroup) restGroup.SetActive(false);
        if (forgeGroup) forgeGroup.SetActive(false);
    }
}