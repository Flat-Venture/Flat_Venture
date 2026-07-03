using System;
using UnityEngine;

/// <summary>
/// 아키텍처 조립 후 맵 생성을 테스트하기 위한 임시 실행기
/// </summary>
public class MapTestRunner : MonoBehaviour
{
    [SerializeField] private MapUIManager uiManager;
    [SerializeField] private StageManager stageManager;

    private MapModel model;
    private MapGenerator generator;
    private MapPresenter presenter;    

    private void Start()
    {
        //객체 생성 및 조립
        model = new MapModel();
        generator = new MapGenerator();
        presenter = new MapPresenter(model, uiManager, generator, HandleNodeEntered);

        if (stageManager != null) stageManager.Init(presenter.OpenMapForSelection);

        //presenter를 통해 새로운 맵 생성
        presenter.GenerateNewDungeonMap();
    }

    private void HandleNodeEntered(MapNode node)
    {
        if (stageManager != null) stageManager.EnterStage(node);
    }

    /// <summary>
    /// 디버그용 맵 즉시 초기화(Reset)
    /// </summary>
    [ContextMenu("Debug Map Reset")]
    public void DebugMapReset()
    {
        if (presenter == null) return;

        //기존 맵 데이터와 UI를 파괴
        presenter.ResetMapSystem();

        //새로운 맵 생성
        presenter.GenerateNewDungeonMap();
    }
}
