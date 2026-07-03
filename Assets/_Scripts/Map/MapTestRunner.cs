using System;
using UnityEngine;

/// <summary>
/// 아키텍처 조립 후 맵 생성을 테스트하기 위한 임시 실행기
/// </summary>
public class MapTestRunner : MonoBehaviour
{
    [SerializeField] private MapUIManager uiManager;

    private MapModel model;
    private MapGenerator generator;
    private MapPresenter presenter;    

    private void Start()
    {
        //객체 생성 및 조립
        model = new MapModel();
        generator = new MapGenerator();
        presenter = new MapPresenter(model, uiManager, HandleNodeEntered);

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
        Debug.Log($"<color=yellow>[메인 시스템]</color> {node.RoomType} 방 진입 처리를 시작합니다 (ID: {node.NodeID})");

        switch (node.RoomType)
        {
            case RoomType.Normal:
            case RoomType.Elite:
            case RoomType.Boss:
                //TODO: 전투 씬 비동기 로드 및 몬스터 시폰 데이터 전달
                //SceneManager.LoadeScene("BattleScene");
                break;
            case RoomType.Shop:
                //TODO: 상점 UI 팝업 오픈
                break;
            case RoomType.Rest:
                //TODO: 모닥불(휴식) UI 오픈
                break;
            case RoomType.Unknown:
                //TODO: 랜덤 이벤트 로직 실행
                break;
        }

        //2초 뒤에 전투를 클리어했다 가정 후 맵을 다시 오픈
        Invoke(nameof(SimulateClearRoom), 2.0f);
    }

    private void SimulateClearRoom()
    {
        Debug.Log("<color=cyan>[메인 시스템]</color> 방을 클리어했습니다. 다시 지도를 엽니다.");
        presenter.OpenMapForSelection();
    }
}
