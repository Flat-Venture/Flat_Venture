using System.Collections.Generic;
using FlatVenture.Inventory;
using UnityEngine;

namespace FlatVenture.SaveLoad
{
    // 지도/스테이지 시스템과 SaveLoad를 느슨하게 연결하는 브리지입니다.
    // MapPresenter, StageManager가 SaveData 구조를 직접 알지 않도록 중간에서 필요한 값만 변환합니다.
    // 활성 세이브가 없으면 아무 것도 하지 않으므로 지도 담당자가 단독 테스트해도 기존 흐름이 유지됩니다.
    public static class DungeonMapSaveBridge
    {
        // SaveData.dungeon 전체를 맵 시스템에 노출하지 않기 위해 지도 복원에 필요한 값만 담습니다.
        // 저장용 데이터가 아니라 MapTestRunner가 시작 시 맵을 복원할지 판단하기 위한 읽기 전용 전달 객체입니다.
        public sealed class DungeonMapRestoreState
        {
            public int dungeonSeed;
            public int currentNodeId;
            public bool hasCurrentNode;
            public bool isInNode;
            public bool isPortalGenerated;
            public List<int> visitedNodeIds = new List<int>();
        }

        // 활성 던전 세이브가 있으면 지도 시작에 필요한 상태를 가져옵니다.
        // false가 반환되면 세이브/로드 흐름이 아니므로 기존 지도 테스트처럼 새 맵을 생성하면 됩니다.
        public static bool TryGetMapRestoreState(out DungeonMapRestoreState state)
        {
            state = null;

            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                return false;
            }

            var dungeon = SaveGameSession.CurrentSaveData.dungeon;
            if (dungeon == null || !dungeon.isInDungeon || dungeon.dungeonSeed == 0)
            {
                return false;
            }

            state = new DungeonMapRestoreState
            {
                dungeonSeed = dungeon.dungeonSeed,
                isInNode = dungeon.dungeonState == DungeonSaveState.InNode || dungeon.dungeonState == DungeonSaveState.PortalGenerated,
                isPortalGenerated = dungeon.dungeonState == DungeonSaveState.PortalGenerated,
                visitedNodeIds = ParseNodeIds(dungeon.clearedNodeIds)
            };

            int currentNodeId;
            state.hasCurrentNode = int.TryParse(dungeon.currentNodeId, out currentNodeId);
            state.currentNodeId = currentNodeId;
            return true;
        }

        // 노드 선택 직후 저장합니다.
        // 이 저장 지점은 "해당 노드 방에 들어갔다"까지만 기록하고, 전투 내부의 세부 진행은 저장하지 않습니다.
        public static void SaveNodeSelected(global::MapModel model, global::MapNode selectedNode)
        {
            if (!TryGetWritableDungeon(out var saveData))
            {
                return;
            }

            ApplyMapState(saveData, model, selectedNode, DungeonSaveState.InNode);
            SaveCurrentSlot(saveData);
        }

        // 방 클리어/포탈 생성 후 저장합니다.
        // 이후 보상 선택 결과는 다음 노드를 선택하기 전까지 저장하지 않아, 재접속 시 보상을 다시 고를 수 있게 합니다.
        public static void SavePortalGenerated(global::MapNode clearedNode)
        {
            if (!TryGetWritableDungeon(out var saveData))
            {
                return;
            }

            var dungeon = saveData.dungeon;
            EnsureDungeonLists(dungeon);
            dungeon.dungeonState = DungeonSaveState.PortalGenerated;

            if (clearedNode != null)
            {
                dungeon.currentNodeId = clearedNode.NodeID.ToString();
                dungeon.currentFloor = clearedNode.Floor + 1;

                dungeon.availableNextNodeIds.Clear();
                for (int i = 0; i < clearedNode.NextNodes.Count; i++)
                {
                    dungeon.availableNextNodeIds.Add(clearedNode.NextNodes[i].NodeID.ToString());
                }
            }

            CaptureInventoryIfExists(saveData);
            SaveCurrentSlot(saveData);
        }

        // 기존 호출부 호환용입니다. 포탈 생성 체크포인트 저장과 같은 의미로 처리합니다.
        public static void SaveRoomCleared(global::MapNode clearedNode)
        {
            SavePortalGenerated(clearedNode);
        }

        // MapModel과 현재 노드 정보를 SaveData.dungeon에 반영합니다.
        // 노드 ID는 팀원 지도 시스템의 int ID를 SaveData 저장 형식에 맞춰 string으로 보관합니다.
        private static void ApplyMapState(SaveData saveData, global::MapModel model, global::MapNode currentNode, DungeonSaveState state)
        {
            var dungeon = saveData.dungeon;
            EnsureDungeonLists(dungeon);
            dungeon.dungeonState = state;

            if (currentNode != null)
            {
                dungeon.currentNodeId = currentNode.NodeID.ToString();
                dungeon.currentFloor = currentNode.Floor + 1;

                dungeon.availableNextNodeIds.Clear();
                for (int i = 0; i < currentNode.NextNodes.Count; i++)
                {
                    dungeon.availableNextNodeIds.Add(currentNode.NextNodes[i].NodeID.ToString());
                }
            }

            dungeon.clearedNodeIds.Clear();
            if (model != null)
            {
                for (int i = 0; i < model.VisitedNodeIDs.Count; i++)
                {
                    AddUnique(dungeon.clearedNodeIds, model.VisitedNodeIDs[i].ToString());
                }
            }

            CaptureInventoryIfExists(saveData);
        }

        // 저장 가능한 활성 던전 세이브가 있는지 확인합니다.
        // 팀원 테스트처럼 SaveGameSession이 없으면 저장을 건너뛰기 위해 false를 반환합니다.
        private static bool TryGetWritableDungeon(out SaveData saveData)
        {
            saveData = null;

            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                return false;
            }

            saveData = SaveGameSession.CurrentSaveData;
            return saveData.dungeon != null && saveData.dungeon.isInDungeon;
        }

        // 현재 활성 슬롯에 SaveData를 파일로 씁니다.
        private static void SaveCurrentSlot(SaveData saveData)
        {
            SaveLoadService.Save(SaveGameSession.CurrentSlotIndex, saveData);
        }

        // 씬에 인벤토리 런타임이 있으면 지도 체크포인트 저장과 함께 인벤토리도 저장합니다.
        // 인벤토리 시스템이 없는 지도 단독 테스트에서는 아무 것도 하지 않습니다.
        private static void CaptureInventoryIfExists(SaveData saveData)
        {
            var runtimes = Object.FindObjectsByType<InventoryRuntimeBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var inventoryRuntime = runtimes.Length > 0 ? runtimes[0] : null;
            if (inventoryRuntime != null)
            {
                var capturedInventory = inventoryRuntime.CaptureSnapshot();
                if (!InventorySaveMapper.HasMeaningfulState(capturedInventory)
                    && InventorySaveMapper.HasMeaningfulState(saveData.dungeon.inventory))
                {
                    Debug.LogWarning("[DungeonMapSaveBridge] 빈 인벤토리 캡처가 기존 저장 인벤토리를 덮어쓰지 않도록 건너뜁니다.");
                    return;
                }

                saveData.dungeon.inventory = capturedInventory;
            }
        }

        // SaveData에는 노드 ID를 string으로 저장하지만, 지도 시스템은 int를 사용하므로 복원 시 변환합니다.
        private static List<int> ParseNodeIds(List<string> nodeIds)
        {
            var result = new List<int>();
            if (nodeIds == null)
            {
                return result;
            }

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int nodeId;
                if (int.TryParse(nodeIds[i], out nodeId) && !result.Contains(nodeId))
                {
                    result.Add(nodeId);
                }
            }

            return result;
        }

        // 이전 버전 세이브나 수동 생성 데이터에서 목록이 null일 수 있으므로 저장 전에 보정합니다.
        private static void EnsureDungeonLists(DungeonSaveData dungeon)
        {
            if (dungeon.clearedNodeIds == null)
            {
                dungeon.clearedNodeIds = new List<string>();
            }

            if (dungeon.availableNextNodeIds == null)
            {
                dungeon.availableNextNodeIds = new List<string>();
            }

            if (dungeon.acquiredItems == null)
            {
                dungeon.acquiredItems = new List<DungeonAcquiredItemSaveData>();
            }

            if (dungeon.inventory == null)
            {
                dungeon.inventory = new InventorySaveData();
            }
        }

        // 같은 노드 ID가 중복 저장되지 않도록 추가합니다.
        private static void AddUnique(List<string> values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value) || values.Contains(value))
            {
                return;
            }

            values.Add(value);
        }
    }
}
