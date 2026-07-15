using UnityEngine;
using System;
using System.Collections.Generic;
using FlatVenture.NUH.Seed;
using FlatVenture.SaveLoad;

/// <summary>
/// MapModel과 MapUIManager를 연결하는 Presenter
/// </summary>
public class MapPresenter
{
    private readonly MapModel model;
    private readonly MapUIManager view;
    private readonly MapGenerator generator;
    private readonly Action<MapNode, bool> onNodeEntered;

    //의존성 주입 (DI) 패턴을 활용하여 Model과 View를 생성자에서 주입받음
    public MapPresenter(MapModel model, MapUIManager view, MapGenerator generator, Action<MapNode, bool> onNodeEntered = null)
    {
        this.model = model;
        this.view = view;
        this.generator = generator;
        this.onNodeEntered = onNodeEntered;
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

            DungeonMapSaveBridge.SaveNodeSelected(model, targetNode);

            //타겟 노드 정보를 넘겨주며 EnterNode 호출
            EnterNode(targetNode);
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

            //탭으로 지도를 열었을 대도 플레이어의 현재 위치를 포커싱
            MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
            if (currentNode != null) view.FocusCamera(currentNode.NormalizedY);
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

        //현재 플레이어가 위치한 노드의 Y좌표를 찾아 카메라에게 이동 명령을 내림
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);

        if (currentNode != null) view.FocusCamera(currentNode.NormalizedY);
    }

    /// <summary>
    /// 노드를 선택하고 던전으로 진입할 때 지도를 닫음
    /// </summary>
    public void EnterNode(MapNode targetNode)
    {
        model.isInDungeon = true;
        view.HideMap();

        //어떤 씬을 불러울지 외부 시스템에게 위임
        onNodeEntered?.Invoke(targetNode, false);
    }
    
    /// <summary>
    /// 마을에서 던전 입구를 밟아 새로운 던전 맵을 생성할 때 호출
    /// </summary>
    public void GenerateNewDungeonMap()
    {
        //무작위 시드값 생성
        int newSeed = FlatVenture.NUH.Seed.SeedValue.Generate();

        GenerateDungeonMap(newSeed, true);
    }

    /// <summary>
    /// 외부에서 전달받은 시드로 던전 맵을 생성합니다. SaveLoad가 있으면 저장된 dungeonSeed를 전달받아 사용합니다.
    /// </summary>
    public void GenerateDungeonMap(int seed, bool openForSelection)
    {
        Debug.Log($"<color=magenta>[MapSystem]</color> 시드({seed})로 던전을 생성합니다.");

        //새롭게 생성된 시드를 Generateor에 넘김
        SeedService tempSeedService = new SeedService(seed);
        var mapData = generator.GenerateMap(tempSeedService);

        //모델에 새 시드와 맵 데이터를 젖당
        model.SetMapData(seed, mapData);

        //View를 통해 화면에 렌더링
        view.DrawMap(model.EntireMap, OnNodeClicked);

        //그린 직후 노드들의 잠금/개방 상태와 선 투명도 업데이트
        UpdateMapState();

        //최초 생성 직후 맵 열기
        if (openForSelection)
        {
            OpenMapForSelection();
        }
    }

    /// <summary>
    /// 저장된 현재 노드/방문 노드 목록을 모델과 화면에 반영합니다.
    /// </summary>
    public void RestoreDungeonProgress(int currentNodeID, IEnumerable<int> visitedNodeIDs, bool isInDungeon)
    {
        model.RestoreProgress(currentNodeID, visitedNodeIDs, isInDungeon);
        model.EnsureValidCurrentNode();
        UpdateMapState();

        if (isInDungeon)
        {
            view.HideMap();
            return;
        }

        OpenMapForSelection();
    }

    /// <summary>
    /// 저장 상태가 노드 내부라면 현재 노드 방으로 다시 진입합니다.
    /// </summary>
    public void EnterRestoredCurrentNode(bool startFromPortalCheckpoint)
    {
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
        if (currentNode == null)
        {
            return;
        }

        model.isInDungeon = true;
        view.HideMap();

        onNodeEntered?.Invoke(currentNode, startFromPortalCheckpoint);
    }

    public void ResetMapSystem()
    {
        //지도 닫기
        view.HideMap();

        //UI 파괴
        view.ClearMap();

        //모델 데이터 초기화
        model.VisitedNodeIDs.Clear();
        model.CurrentNodeID = -1;
        model.isInDungeon = false;

        Debug.Log("<color=yellow>[MapSystem]</color> 지도 시스템이 초기화되었습니다.");
    }
}
