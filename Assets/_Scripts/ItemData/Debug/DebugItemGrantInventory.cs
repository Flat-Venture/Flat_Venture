using System.Collections.Generic;

namespace FlatVenture.ItemData
{
    // 치트 인벤토리에 들어온 아이템 한 칸의 임시 데이터입니다.
    public sealed class DebugAcquiredItem
    {
        public readonly string itemId;
        public readonly string displayName;
        public readonly ItemRecord itemRecord;

        public DebugAcquiredItem(ItemRecord itemRecord)
        {
            this.itemRecord = itemRecord;
            itemId = itemRecord.definition.itemId;
            displayName = string.IsNullOrEmpty(itemRecord.definition.displayName)
                ? itemId
                : itemRecord.definition.displayName;
        }
    }

    // 치트 아이템 지급 테스트에서만 사용하는 임시 보관함입니다.
    // 실제 인벤토리/세이브로드 시스템이 붙기 전까지 획득한 아이템의 최소 정보만 저장합니다.
    public sealed class DebugItemGrantInventory
    {
        private readonly List<DebugAcquiredItem> acquiredItems = new List<DebugAcquiredItem>();

        public int Capacity { get; private set; }

        public IReadOnlyList<DebugAcquiredItem> AcquiredItems
        {
            get { return acquiredItems; }
        }

        public int Count
        {
            get { return acquiredItems.Count; }
        }

        public bool IsFull
        {
            get { return acquiredItems.Count >= Capacity; }
        }

        public DebugItemGrantInventory(int capacity)
        {
            Capacity = capacity;
        }

        // 아이템 표시 이름과 원본 레코드를 임시 보관함에 추가합니다.
        public bool TryAdd(ItemRecord item, out string message)
        {
            if (item == null || item.definition == null)
            {
                message = "아이템 데이터가 비어 있습니다.";
                return false;
            }

            if (IsFull)
            {
                message = "인벤토리가 가득 찼습니다. (" + Count + "/" + Capacity + ")";
                return false;
            }

            var acquiredItem = new DebugAcquiredItem(item);
            acquiredItems.Add(acquiredItem);
            message = acquiredItem.displayName + " 획득";
            return true;
        }

        // 테스트를 다시 시작할 수 있게 임시 보관함을 비웁니다.
        public void Clear()
        {
            acquiredItems.Clear();
        }
    }
}
