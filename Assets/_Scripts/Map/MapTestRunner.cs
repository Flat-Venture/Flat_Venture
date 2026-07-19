using System;
using UnityEngine;
using FlatVenture.SaveLoad;

/// <summary>
/// 아키텍처 조립과 맵 생성을 테스트하기 위한 임시 실행기입니다.
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
        // 객체를 생성하고 연결합니다.
        model = new MapModel();
        generator = new MapGenerator();
        presenter = new MapPresenter(model, uiManager, generator, HandleNodeEntered);

        if (stageManager != null) stageManager.Init(presenter.OpenMapForSelection);

        DungeonMapSaveBridge.DungeonMapRestoreState restoreState;
        if (DungeonMapSaveBridge.TryGetMapRestoreState(out restoreState))
        {
            presenter.GenerateDungeonMap(restoreState.dungeonSeed, !restoreState.hasCurrentNode);

            if (restoreState.hasCurrentNode)
            {
                presenter.RestoreDungeonProgress(restoreState.currentNodeId, restoreState.visitedNodeIds, restoreState.isInNode);

                if (restoreState.isInNode)
                {
                    presenter.EnterRestoredCurrentNode(restoreState.isPortalGenerated);
                }
            }
        }
        else
        {
            // presenter를 통해 새 맵을 생성합니다.
            presenter.GenerateNewDungeonMap();
        }
    }

    private void HandleNodeEntered(MapNode node, bool startFromPortalCheckpoint)
    {
        if (stageManager != null) stageManager.EnterStage(node, startFromPortalCheckpoint);
    }

    public bool OpenReadOnlyMap()
    {
        if (presenter != null)
        {
            return presenter.OpenReadOnlyMap();
        }

        return false;
    }

    public void CloseReadOnlyMap()
    {
        if (presenter != null)
        {
            presenter.CloseReadOnlyMap();
        }
    }

    /// <summary>
    /// 디버그용 맵 즉시 초기화입니다.
    /// </summary>
    [ContextMenu("Debug Map Reset")]
    public void DebugMapReset()
    {
        if (presenter == null) return;

        // 기존 맵 데이터와 UI를 정리합니다.
        presenter.ResetMapSystem();

        // 새 맵을 생성합니다.
        presenter.GenerateNewDungeonMap();
    }
}
