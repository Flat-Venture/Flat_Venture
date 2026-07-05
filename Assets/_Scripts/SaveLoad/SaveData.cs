using System;
using System.Collections.Generic;

namespace FlatVenture.SaveLoad
{
    // 던전 저장 위치를 구분하기 위한 상태값입니다.
    // 로드 후 어떤 화면이나 진행 지점으로 복원할지 판단하는 기준으로 사용합니다.
    public enum DungeonSaveState
    {
        None = 0,
        Map = 1,
        InNode = 2
    }

    // 저장 파일 하나의 최상위 데이터입니다.
    // 세이브 슬롯 하나가 이 구조 하나를 JSON으로 저장합니다.
    [Serializable]
    public sealed class SaveData
    {
        public int saveVersion = 1;
        public int slotIndex;
        public string createdAt;
        public string updatedAt;
        public double playTimeSeconds;
        public UserSaveData user = new UserSaveData();
        public UnlockSaveData unlocks = new UnlockSaveData();
        public AchievementSaveData achievements = new AchievementSaveData();
        public TraitSaveData traits = new TraitSaveData();
        public DungeonSaveData dungeon = new DungeonSaveData();
    }

    // 유저 프로필과 계정성 진행 정보를 저장합니다.
    [Serializable]
    public sealed class UserSaveData
    {
        public string profileName = "Player";
        public int userLevel = 1;
        public int userExp;
        public string selectedCharacterId = "character_warrior";
        public int gold;
    }

    // 사용할 수 있게 열린 콘텐츠 목록을 저장합니다.
    [Serializable]
    public sealed class UnlockSaveData
    {
        public List<string> unlockedCharacterIds = new List<string>();
        public List<string> unlockedItemIds = new List<string>();
        public List<string> unlockedTraitIds = new List<string>();
    }

    // 달성한 업적 ID 목록을 저장합니다.
    [Serializable]
    public sealed class AchievementSaveData
    {
        public List<string> achievedAchievementIds = new List<string>();
    }

    // 현재 선택해서 적용 중인 특성 ID 목록을 저장합니다.
    [Serializable]
    public sealed class TraitSaveData
    {
        public List<string> selectedTraitIds = new List<string>();
    }

    // 던전 진행 상황과 시드 정보를 저장합니다.
    [Serializable]
    public sealed class DungeonSaveData
    {
        public bool isInDungeon;
        public int dungeonSeed;
        public int currentFloor;
        public string currentNodeId;
        public List<string> clearedNodeIds = new List<string>();
        public List<string> availableNextNodeIds = new List<string>();
        public List<DungeonAcquiredItemSaveData> acquiredItems = new List<DungeonAcquiredItemSaveData>();
        public DungeonSaveState dungeonState = DungeonSaveState.None;
    }

    // 던전 한 판 안에서 획득한 아이템 정보를 저장합니다.
    // 던전에서 나가면 이 목록은 비워지고, 정식 인벤토리가 생기면 슬롯/옵션 정보로 확장할 수 있습니다.
    [Serializable]
    public sealed class DungeonAcquiredItemSaveData
    {
        public string itemId;
        public string displayName;
        public int count;
    }
}
