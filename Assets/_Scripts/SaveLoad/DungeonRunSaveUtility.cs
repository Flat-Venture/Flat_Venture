using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlatVenture.SaveLoad
{
    // 던전 한 판의 저장 데이터를 변경하는 공용 도구입니다.
    // 아이템 획득처럼 자주 일어나는 일은 여기서 메모리만 바꾸고, 실제 파일 저장은 체크포인트에서 호출합니다.
    public static class DungeonRunSaveUtility
    {
        public const int DefaultStartingGold = 20000;

        // 새 던전을 시작할 때 이전 던전 진행 데이터와 획득 아이템을 비운 뒤 시작 상태를 설정합니다.
        public static void StartDungeon(SaveData saveData, int seed)
        {
            if (saveData == null || saveData.dungeon == null)
                return;

            ClearDungeonProgress(saveData);

            saveData.dungeon.isInDungeon = true;
            saveData.dungeon.dungeonSeed = seed;
            saveData.dungeon.currentFloor = 1;
            saveData.dungeon.playerLevel = 1;
            saveData.dungeon.playerExp = 0;
            saveData.dungeon.currentNodeId = string.Empty;
            saveData.dungeon.inventory.gold = DefaultStartingGold;
            saveData.dungeon.dungeonState = DungeonSaveState.Map;
        }

        // 던전 진행 정보와 던전 안에서 얻은 아이템을 모두 초기화합니다.
        public static void ClearDungeonProgress(SaveData saveData)
        {
            if (saveData == null || saveData.dungeon == null)
                return;

            saveData.dungeon.isInDungeon = false;
            saveData.dungeon.dungeonSeed = 0;
            saveData.dungeon.currentFloor = 0;
            saveData.dungeon.playerLevel = 0;
            saveData.dungeon.playerExp = 0;
            saveData.dungeon.currentNodeId = string.Empty;
            EnsureLists(saveData.dungeon);
            saveData.dungeon.clearedNodeIds.Clear();
            saveData.dungeon.availableNextNodeIds.Clear();
            saveData.dungeon.acquiredItems.Clear();
            saveData.dungeon.inventory = new InventorySaveData();
            saveData.dungeon.dungeonState = DungeonSaveState.None;
        }

        // 현재 세션의 던전 획득 아이템 목록에 아이템을 추가합니다. 이 메서드는 저장 파일을 쓰지 않습니다.
        public static bool TryAddAcquiredItem(string itemId, string displayName)
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
                return false;

            return TryAddAcquiredItem(SaveGameSession.CurrentSaveData, itemId, displayName);
        }

        // 지정한 저장 데이터의 던전 획득 아이템 목록에 아이템을 추가합니다. 이 메서드는 저장 파일을 쓰지 않습니다.
        public static bool TryAddAcquiredItem(SaveData saveData, string itemId, string displayName)
        {
            if (saveData == null || saveData.dungeon == null || !saveData.dungeon.isInDungeon)
                return false;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            var itemName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
            EnsureLists(saveData.dungeon);
            var acquiredItems = saveData.dungeon.acquiredItems;
            for (var i = 0; i < acquiredItems.Count; i++)
            {
                if (!string.Equals(acquiredItems[i].itemId, itemId, StringComparison.Ordinal))
                    continue;

                acquiredItems[i].count++;
                Debug.Log("[DungeonRunSave] 던전 획득 아이템 추가: " + itemName + " x" + acquiredItems[i].count + " / 파일 저장은 아직 하지 않음");
                return true;
            }

            acquiredItems.Add(new DungeonAcquiredItemSaveData
            {
                itemId = itemId,
                displayName = itemName,
                count = 1
            });

            Debug.Log("[DungeonRunSave] 던전 획득 아이템 추가: " + itemName + " / 파일 저장은 아직 하지 않음");
            return true;
        }

        // 이전 버전 세이브를 불러왔을 때 비어 있을 수 있는 목록을 보정합니다.
        private static void EnsureLists(DungeonSaveData dungeon)
        {
            if (dungeon.clearedNodeIds == null)
                dungeon.clearedNodeIds = new List<string>();

            if (dungeon.availableNextNodeIds == null)
                dungeon.availableNextNodeIds = new List<string>();

            if (dungeon.acquiredItems == null)
                dungeon.acquiredItems = new List<DungeonAcquiredItemSaveData>();

            if (dungeon.inventory == null)
                dungeon.inventory = new InventorySaveData();
        }

        // 임시 테스트용으로 현재 던전 층을 올립니다. 이 메서드는 저장 파일을 쓰지 않습니다.
        public static bool TryIncreaseFloor(int amount, out int currentFloor)
        {
            currentFloor = 0;

            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
                return false;

            var dungeon = SaveGameSession.CurrentSaveData.dungeon;
            if (dungeon == null || !dungeon.isInDungeon)
                return false;

            dungeon.currentFloor = Mathf.Max(1, dungeon.currentFloor + amount);
            currentFloor = dungeon.currentFloor;
            Debug.Log("[DungeonRunSave] 던전 층 변경: " + currentFloor + "층 / 파일 저장은 아직 하지 않음");
            return true;
        }
    }
}
