using UnityEngine;

/// <summary>
/// MapModel과 MapUIManager를 연결하는 Presenter
/// </summary>
public class MapPresenter
{
    private readonly MapModel model;
    private readonly MapUIManager view;

    //의존성 주입 (DI) 패턴을 활용하여 Model과 View를 생성자에서 주입받음
    public MapPresenter(MapModel model, MapUIManager view)
    {
        this.model = model;
        this.view = view;
    }

    /// <summary>
    /// 게임 시작 시 맵이 생성된 직후 호출해 Model의 데이터를 View에 전달하여 랜더링을 초기화
    /// </summary>
    public void InitializeMapRendering()
    {
        //Model에서 맵 데이터를 가져와 View에 전달
        view.DrawMap(model.EntireMap, OnNodeClicked);

        //최초 생성 직후 시각적 상태 업데이트
        UpdateMapState();
    }

    private void OnNodeClicked(int nodeID)
    {
        //플레이어의 현재 위치 노드를 가져옴
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);

        if (currentNode == null) return;

        //클릭한 노드가 '현재 위치에서 선으로 이어진 다음 노드'인지 확인
        bool isValidMove = false;
        MapNode targetNode = null;

        for (int i = 0; i < currentNode.NextNodes.Count; i++)
        {
            if (currentNode.NextNodes[i].NodeID == nodeID)
            {
                isValidMove = true;
                targetNode = currentNode.NextNodes[i];
                break;
            }
        }

        //결과 처리
        if (isValidMove)
        {
            Debug.Log($"<color=green>[이동 승인]</color> 플레이어가 {currentNode.NodeID}번에서 {nodeID}번({targetNode.RoomType}) 노드로 전진합니다.");

            //현재 위치를 지안 노드 목록에 추가
            model.VisitedNodeIDs.Add(model.CurrentNodeID);

            //모델의 현재 위치 상태를 다음 방으로 갱신
            model.CurrentNodeID = nodeID;

            //갱신된 상태를 바탕으로 맵 전체 시작적 피드백 업데이트
            UpdateMapState();

            //TODO: 실제 던전 씬 로드, 몬스터 스폰 등 게임 시스템 호출
            EnterNode();
        }

        else
        {
            //이동 실패 (연결되지 않은 방을 눌렀거나, 이전 층을 누른 경우)
            Debug.LogWarning($"<color=red>[이동 불가]</color> {nodeID}번 노드는 현재 위치({currentNode.NodeID}번)에서 갈 수 없는 경로입니다.");

            // TODO: 실패 삑- 소리 재생, 화면 흔들림 효과 등 View에 요청
        }
    }

    /// <summary>
    /// 현재 모델의 위치 정보를 기반으로 뷰에게 상태 업데이트를 지시
    /// </summary>
    private void UpdateMapState()
    {
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
        view.UpdateNodeVisuals(model.CurrentNodeID, model.VisitedNodeIDs, currentNode?.NextNodes);
    }

    /// <summary>
    /// 외부 인풋 시스템에서 탭 열기(Tab키 누름 등) 이벤트 발생시 호출
    /// </summary>
    public void HandleTabStarted()
    {
        if (model.isInDungeon)
        {
            view.isMapOpendByTab = true;

            //가시성만 켜고, 노드 클릭은 불가
            view.ShowMap(false);
        }
    }

    /// <summary>
    /// 외부 인풋 시스템에서 탭 닫기(Tab키 누름 등) 이벤트 발생시 호출
    /// </summary>
    public void HandleTabCanceled()
    {
        if (model.isInDungeon && view.isMapOpendByTab)
        {
            view.isMapOpendByTab = false;
            view.HideMap();
        }
    }

    /// <summary>
    /// 지역을 클리어하고 다음 노드를 선택해야 할 때 강제로 맵을 호출
    /// </summary>
    public void OpenMapForSelection()
    {
        model.isInDungeon = false;

        //가시성을 켜고, 노드 클릭 활성화
        view.ShowMap(true);
    }

    /// <summary>
    /// 노드를 선택하고 던전으로 진입할 때 지도를 닫음
    /// </summary>
    public void EnterNode()
    {
        model.isInDungeon = true;
        view.HideMap();
    }
}
