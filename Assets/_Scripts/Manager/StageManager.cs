using UnityEngine;
using System;

/// <summary>
/// 진입한 맵 노드의 RoomType에 맞춰 실제 게임 스테이지를 세팅하고 관리
/// </summary>
public class StageManager : MonoBehaviour
{
    //스테이지가 끝났을 때 호출하여 지도를 다시 열게 할 콜백 함수
    private Action onStageClearedCallback;

    /// <summary>
    /// 외부에서 맵 복귀용 콜백을 주입받아 초기화
    /// </summary>
    public void Init(Action onStageCleared)
    {
        this.onStageClearedCallback = onStageCleared;
    }

    /// <summary>
    /// 맵 시스템에서 전달받은 노드 정보를 바탕으로 스테이지를 구성
    /// </summary>
    public void EnterStage(MapNode node)
    {
        Debug.Log($"<color=orange>[StageManager]</color> {node.Floor}층 {node.RoomType} 방 세팅을 시작합니다.");

        switch (node.RoomType)
        {
            case RoomType.Normal:
                SetupBattleStage("일반 몬스터");
                break;
            case RoomType.Elite:
                SetupBattleStage("엘리트 몬스터");
                break;
            case RoomType.Boss:
                SetupBattleStage("보스 몬스터");
                break;
            case RoomType.Rest:
                SetupRestStage();
                break;
            case RoomType.Shop:
                SetupShopStage();
                break;
            case RoomType.Unknown:
                SetupEventStage();
                break;
            case RoomType.Start:
                Debug.Log("<color=orange>[StageManager]</color> 시작 방입니다. 바로 다음 노드를 선택하세요.");
                //시작 방은 전투가 없으므로 0.5초 뒤 바로 맵을 다시 오픈
                Invoke(nameof(ClearStage), 0.5f); 
                break;
        }
    }

    private void SetupBattleStage(string monsterType)
    {
        Debug.Log($"<color=orange>[StageManager]</color> {monsterType} 스폰 중... 전투 시작!");
        
        //TODO: 앞으로 만들 MonsterBase 및 프리팹 소환 로직이 들어갈 자리
        //[임시 테스트용] 3초 뒤에 전투가 끝난 것으로 간주
        //실제 게임에서는 몬스터가 모두 죽었을 때 ClearStage()를 호출하면 됨
        Invoke(nameof(ClearStage), 3.0f);
    }

    private void SetupRestStage()
    {
        Debug.Log("<color=orange>[StageManager]</color> 모닥불 UI를 엽니다. (휴식/강화 선택)");
        Invoke(nameof(ClearStage), 3.0f);
    }

    private void SetupShopStage()
    {
        Debug.Log("<color=orange>[StageManager]</color> 상점 UI를 엽니다.");
        Invoke(nameof(ClearStage), 3.0f);
    }

    private void SetupEventStage()
    {
        Debug.Log("<color=orange>[StageManager]</color> 미지(물음표) 이벤트 팝업을 엽니다.");
        Invoke(nameof(ClearStage), 3.0f);
    }

    /// <summary>
    /// 방 안의 이벤트나 전투가 모두 끝났을 때 외부에서 호출하여 맵으로 돌아감
    /// </summary>
    public void ClearStage()
    {
        Debug.Log("<color=cyan>[StageManager]</color> 스테이지 클리어! 맵 시스템에 완료 신호를 보냅니다.");
        onStageClearedCallback?.Invoke();
    }
}