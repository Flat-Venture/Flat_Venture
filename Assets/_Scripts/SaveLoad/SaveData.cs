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
        public int jewel;
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
        public List<TraitLevelSaveData> selectedUserTraits = new List<TraitLevelSaveData>();
        public List<JobTraitSelectionSaveData> jobTraitSelections = new List<JobTraitSelectionSaveData>();
    }

    // 선택한 특성 하나의 ID와 현재 레벨을 저장합니다.
    // 최대 레벨, 선행 조건, 효과 수치 같은 정의 정보는 CSV에서 읽고, 세이브에는 실제 찍은 결과만 남깁니다.
    [Serializable]
    public sealed class TraitLevelSaveData
    {
        public string traitId;
        public int level;
    }

    // 직업별로 선택한 특성 ID 목록을 저장합니다.
    // 직업을 바꿨다가 다시 돌아와도 이전에 찍은 직업 특성을 복원하기 위한 데이터입니다.
    [Serializable]
    public sealed class JobTraitSelectionSaveData
    {
        public string jobId;
        public List<TraitLevelSaveData> selectedTraits = new List<TraitLevelSaveData>();
    }

    // 던전 진행 상황과 시드 정보를 저장합니다.
    [Serializable]
    public sealed class DungeonSaveData
    {
        public bool isInDungeon;
        public int dungeonSeed;
        public int currentFloor;
        public int playerLevel = 1;
        public int playerExp;
        public int gold;
        public string currentNodeId;
        public List<string> clearedNodeIds = new List<string>();
        public List<string> availableNextNodeIds = new List<string>();
        public List<DungeonAcquiredItemSaveData> acquiredItems = new List<DungeonAcquiredItemSaveData>();
        public InventorySaveData inventory = new InventorySaveData();
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

    // 던전 안에서 사용하는 5x5 인벤토리 상태를 저장합니다.
    [Serializable]
    public sealed class InventorySaveData
    {
        public int width = 5;
        public int height = 5;
        public int debugGold;
        public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>();
    }

    // 인벤토리 슬롯 하나의 아이템, 프레임, 강화, 봉인 상태를 저장합니다.
    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public int slotIndex;
        public string frameElementId;
        public int upgradeLevel;
        public bool isSealed;
        public InventoryItemSaveData item;
    }

    // 인벤토리에 들어온 아이템 인스턴스 정보를 저장합니다.
    [Serializable]
    public sealed class InventoryItemSaveData
    {
        public string instanceId;
        public string itemId;
        public string displayName;
        public string rarityId;
        public bool isCursed;
        public bool isUnique;
        public int sellPrice;
        public List<InventoryItemElementSaveData> elements = new List<InventoryItemElementSaveData>();
    }

    // 아이템 인스턴스가 가진 속성 포인트를 저장합니다.
    [Serializable]
    public sealed class InventoryItemElementSaveData
    {
        public string elementId;
        public int elementValue;
    }
}
