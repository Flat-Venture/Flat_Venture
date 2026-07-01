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
    }

    private void OnNodeClicked(int nodeID)
    {
        Debug.Log($"Node clicked: {nodeID}");

        //이동이 유효하다면 던전으로 진입 상태 변경 및 뷰 닫기
        //EnterNode();
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
