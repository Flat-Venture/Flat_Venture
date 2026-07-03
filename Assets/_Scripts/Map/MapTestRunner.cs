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
        presenter = new MapPresenter(model, uiManager, HandleNodeEntered);

        if (stageManager != null) stageManager.Init(presenter.OpenMapForSelection);

        //임시 시드로 데이터 생성
        int testSeed = 12345;
        var mapData = generator.GenerateMap(testSeed);

        //model에 생성된 데이터 저장
        model.SetMapData(testSeed, mapData);

        //presenter를 통해 view에 데이터 전달 및 랜더링 초기화
        presenter.InitializeMapRendering();

        //테스트를 위해 맵을 강제로 오픈
        presenter.OpenMapForSelection();

        Debug.Log($"테스트 맵 랜더링 완료. 시드: {testSeed}");
    }

    private void HandleNodeEntered(MapNode node)
    {
        if (stageManager != null) stageManager.EnterStage(node);
    }
}
