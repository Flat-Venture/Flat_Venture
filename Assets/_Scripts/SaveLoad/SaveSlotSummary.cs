using System;

namespace FlatVenture.SaveLoad
{
    // 타이틀의 세이브 슬롯 UI에서 사용할 가벼운 요약 정보입니다.
    // 전체 SaveData를 UI에 직접 넘기지 않기 위해 별도 모델로 둡니다.
    [Serializable]
    public sealed class SaveSlotSummary
    {
        public int slotIndex;
        public bool exists;
        public bool isCorrupted;
        public string errorMessage;
        public string profileName;
        public string updatedAt;
        public double playTimeSeconds;
        public int userLevel;
        public string selectedCharacterId;
        public bool isInDungeon;
        public DungeonSaveState dungeonState;

        // 빈 슬롯 요약을 만듭니다.
        public static SaveSlotSummary Empty(int slotIndex)
        {
            return new SaveSlotSummary
            {
                slotIndex = slotIndex,
                exists = false,
                isCorrupted = false,
                errorMessage = string.Empty
            };
        }

        // 손상된 슬롯 요약을 만듭니다.
        public static SaveSlotSummary Corrupted(int slotIndex, string errorMessage)
        {
            return new SaveSlotSummary
            {
                slotIndex = slotIndex,
                exists = true,
                isCorrupted = true,
                errorMessage = errorMessage
            };
        }

        // SaveData에서 슬롯 UI에 필요한 정보만 뽑아냅니다.
        public static SaveSlotSummary FromSaveData(SaveData saveData)
        {
            return new SaveSlotSummary
            {
                slotIndex = saveData.slotIndex,
                exists = true,
                isCorrupted = false,
                errorMessage = string.Empty,
                profileName = saveData.user.profileName,
                updatedAt = saveData.updatedAt,
                playTimeSeconds = saveData.playTimeSeconds,
                userLevel = saveData.user.userLevel,
                selectedCharacterId = saveData.user.selectedCharacterId,
                isInDungeon = saveData.dungeon.isInDungeon,
                dungeonState = saveData.dungeon.dungeonState
            };
        }
    }
}
