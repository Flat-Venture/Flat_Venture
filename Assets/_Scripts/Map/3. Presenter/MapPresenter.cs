using UnityEngine;
using System;
using System.Collections.Generic;
using FlatVenture.NUH.Seed;
using FlatVenture.SaveLoad;

/// <summary>
/// MapModel과 MapUIManager를 연결하는 Presenter입니다.
/// </summary>
public class MapPresenter
{
    private readonly MapModel model;
    private readonly MapUIManager view;
    private readonly MapGenerator generator;
    private readonly Action<MapNode, bool> onNodeEntered;

    // 의존성 주입 패턴을 사용하여 Model과 View를 생성자에서 주입받습니다.
    public MapPresenter(MapModel model, MapUIManager view, MapGenerator generator, Action<MapNode, bool> onNodeEntered = null)
    {
        this.model = model;
        this.view = view;
        this.generator = generator;
        this.onNodeEntered = onNodeEntered;
    }

    /// <summary>
    /// 게임 시작 후 맵이 생성된 직후 호출되며 Model 데이터를 View에 전달해 렌더링을 초기화합니다.
    /// </summary>
    public void InitializeMapRendering()
    {
        // Model에서 맵 데이터를 가져와 View에 전달합니다.
        view.DrawMap(model.EntireMap, OnNodeClicked);

        // 최초 생성 직후 시각 상태를 업데이트합니다.
        UpdateMapState();
    }

    private void OnNodeClicked(int nodeID)
    {
        // 플레이어의 현재 위치 노드를 가져옵니다.
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);

        if (currentNode == null) return;

        // 클릭한 노드가 현재 위치에서 이동 가능한 다음 노드인지 확인합니다.
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

        if (isValidMove)
        {
            Debug.Log($"<color=green>[이동 승인]</color> 플레이어가 {currentNode.NodeID}번에서 {nodeID}번({targetNode.RoomType}) 노드로 전진합니다.");

            // 현재 위치를 방문한 노드 목록에 추가합니다.
            model.VisitedNodeIDs.Add(model.CurrentNodeID);

            // 모델의 현재 위치를 다음 방으로 갱신합니다.
            model.CurrentNodeID = nodeID;

            // 갱신된 상태를 바탕으로 맵 전체의 시각 피드백을 업데이트합니다.
            UpdateMapState();

            DungeonMapSaveBridge.SaveNodeSelected(model, targetNode);

            // 선택한 노드 정보를 넘겨주며 실제 방 입장을 요청합니다.
            EnterNode(targetNode);
        }
        else
        {
            // 이동 실패: 연결되지 않은 방을 클릭했거나 이전 층을 누른 경우입니다.
            Debug.LogWarning($"<color=red>[이동 불가]</color> {nodeID}번 노드는 현재 위치({currentNode.NodeID}번)에서 갈 수 없는 경로입니다.");

            // TODO: 실패 사운드, 화면 흔들림 등 View 연출을 추가할 수 있습니다.
        }
    }

    /// <summary>
    /// 현재 모델의 위치 정보를 기반으로 View의 노드 상태를 업데이트합니다.
    /// </summary>
    private void UpdateMapState()
    {
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
        view.UpdateNodeVisuals(model.CurrentNodeID, model.VisitedNodeIDs, currentNode?.NextNodes);
    }

    /// <summary>
    /// 맵 입력 시스템에서 맵 열기 입력이 발생했을 때 호출합니다.
    /// </summary>
    public void HandleTabStarted()
    {
        if (model.isInDungeon)
        {
            view.isMapOpendByTab = true;

            // 가시성만 켜고 노드 클릭은 막습니다.
            view.ShowMap(false);

            // 열린 지도에서 플레이어의 현재 위치를 포커스합니다.
            MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
            if (currentNode != null) view.FocusCamera(currentNode.NormalizedY);
        }
    }

    /// <summary>
    /// 방 안에서 지도를 확인할 때 사용하는 읽기 전용 지도 모드입니다.
    /// </summary>
    public bool OpenReadOnlyMap()
    {
        if (!model.isInDungeon && model.CurrentNodeID < 0)
        {
            return false;
        }

        view.isMapOpendByTab = true;
        view.ShowMapPreview();

        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);
        if (currentNode != null) view.FocusCamera(currentNode.NormalizedY);
        return true;
    }

    public void HandleTabCanceled()
    {
        view.isMapOpendByTab = false;
        view.HideMap();
    }

    public void CloseReadOnlyMap()
    {
        view.isMapOpendByTab = false;
        view.HideMap();
    }

    /// <summary>
    /// 지도를 열어두고 다음 노드를 선택해야 할 때 맵을 표시합니다.
    /// </summary>
    public void OpenMapForSelection()
    {
        model.isInDungeon = false;
        view.isMapOpendByTab = false;

        // 가시성을 켜고 노드 클릭도 허용합니다.
        view.ShowMap(true);
        UpdateMapState();

        // 현재 플레이어가 위치한 노드의 Y좌표를 찾아 카메라를 이동합니다.
        MapNode currentNode = model.GetNodeByID(model.CurrentNodeID);

        if (currentNode != null) view.FocusCamera(currentNode.NormalizedY);
    }

    /// <summary>
    /// 노드를 선택하고 던전 방으로 진입할 때 지도를 닫습니다.
    /// </summary>
    public void EnterNode(MapNode targetNode)
    {
        model.isInDungeon = true;
        view.HideMap();

        // 어떤 방을 불러올지 던전 시스템에 위임합니다.
        onNodeEntered?.Invoke(targetNode, false);
    }

    /// <summary>
    /// 마을에서 던전 입구를 통해 새 던전 맵을 생성할 때 호출합니다.
    /// </summary>
    public void GenerateNewDungeonMap()
    {
        // 무작위 시드를 생성합니다.
        int newSeed = FlatVenture.NUH.Seed.SeedValue.Generate();

        GenerateDungeonMap(newSeed, true);
    }

    /// <summary>
    /// 외부에서 전달받은 시드로 던전 맵을 생성합니다. SaveLoad가 있으면 저장된 dungeonSeed를 전달받아 사용합니다.
    /// </summary>
    public void GenerateDungeonMap(int seed, bool openForSelection)
    {
        Debug.Log($"<color=magenta>[MapSystem]</color> 시드({seed})로 던전을 생성합니다.");

        // 새로 생성한 시드를 Generator에 전달합니다.
        SeedService tempSeedService = new SeedService(seed);
        var mapData = generator.GenerateMap(tempSeedService);

        // 모델에 시드와 맵 데이터를 할당합니다.
        model.SetMapData(seed, mapData);

        view.DrawMap(model.EntireMap, OnNodeClicked);

        // 그린 직후 노드들의 잠금/개방 상태와 투명도를 업데이트합니다.
        UpdateMapState();

        // 최초 생성 직후 맵을 엽니다.
        if (openForSelection)
        {
            OpenMapForSelection();
        }
    }

    /// <summary>
    /// 저장된 현재 노드와 방문 노드 목록을 모델과 화면에 반영합니다.
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
    /// 저장 상태가 노드 안이라면 현재 노드 방으로 다시 진입합니다.
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
        // 지도를 닫습니다.
        view.HideMap();

        // UI를 정리합니다.
        view.ClearMap();

        // 모델 데이터를 초기화합니다.
        model.VisitedNodeIDs.Clear();
        model.CurrentNodeID = -1;
        model.isInDungeon = false;

        Debug.Log("<color=yellow>[MapSystem]</color> 지도 시스템이 초기화되었습니다.");
    }
}
