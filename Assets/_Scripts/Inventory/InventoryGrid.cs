using System;
using System.Collections.Generic;

namespace FlatVenture.Inventory
{
    // 5x5 인벤토리의 순수 규칙을 담당합니다. UI와 세이브는 이 클래스를 이용해서 연결합니다.
    public sealed class InventoryGrid
    {
        public const int DefaultWidth = 5;
        public const int DefaultHeight = 5;

        private readonly InventorySlot[] slots;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Capacity { get { return slots.Length; } }
        public int TemporarySwapCount { get; private set; }

        public IReadOnlyList<InventorySlot> Slots
        {
            get { return slots; }
        }

        public InventoryGrid(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            slots = new InventorySlot[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = ToIndex(x, y);
                    slots[index] = new InventorySlot(new InventoryPosition(index, x, y));
                }
            }
        }

        // 해당 장소에서만 사용할 수 있는 임시 스왑 횟수를 설정합니다.
        public void SetTemporarySwapCount(int count)
        {
            TemporarySwapCount = Math.Max(0, count);
        }

        // 장소를 벗어날 때 임시 스왑 횟수를 제거합니다.
        public void ClearTemporarySwapCount()
        {
            TemporarySwapCount = 0;
        }

        // 모든 아이템과 슬롯 상태를 초기화합니다.
        public void ClearAll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].item = null;
                slots[i].SetFrameElement(null);
                slots[i].SetUpgradeLevel(0);
                slots[i].SetSealed(false);
            }

            TemporarySwapCount = 0;
        }

        // 좌표를 인덱스로 변환합니다.
        public int ToIndex(int x, int y)
        {
            return y * Width + x;
        }

        // 인덱스가 인벤토리 범위 안인지 확인합니다.
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < slots.Length;
        }

        // 지정한 인덱스의 슬롯을 가져옵니다.
        public InventorySlot GetSlot(int index)
        {
            return IsValidIndex(index) ? slots[index] : null;
        }

        // 비어 있는 랜덤 위치에 아이템을 넣습니다.
        public bool TryAddItemRandom(InventoryItem item, out InventoryPosition position)
        {
            position = new InventoryPosition();

            if (item == null)
            {
                return false;
            }

            var emptyIndices = GetEmptyIndices();
            if (emptyIndices.Count == 0)
            {
                return false;
            }

            int index = emptyIndices[UnityEngine.Random.Range(0, emptyIndices.Count)];
            slots[index].item = item;
            position = slots[index].position;
            return true;
        }

        // 같은 item_id를 가진 아이템이 이미 들어있는지 확인합니다.
        public bool ContainsItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && slots[i].item.itemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        // 지정한 위치에 아이템을 넣습니다. 세이브 로드 복원용으로 사용할 수 있습니다.
        public bool TryAddItemAt(int index, InventoryItem item)
        {
            if (!IsValidIndex(index) || item == null || slots[index].HasItem)
            {
                return false;
            }

            slots[index].item = item;
            return true;
        }

        // 아이템을 인벤토리에서 제거합니다. 판매와 버리기에서 공통으로 사용합니다.
        public bool TryRemoveItem(int index, out InventoryItem removedItem)
        {
            removedItem = null;

            if (!IsValidIndex(index) || !slots[index].HasItem)
            {
                return false;
            }

            removedItem = slots[index].item;
            slots[index].item = null;
            return true;
        }

        // 두 아이템 위치를 교환합니다. 기본적으로 임시 스왑 횟수를 1회 소모합니다.
        public bool TrySwapItems(int firstIndex, int secondIndex, out string message)
        {
            message = null;

            if (!IsValidIndex(firstIndex) || !IsValidIndex(secondIndex))
            {
                message = "스왑할 슬롯이 인벤토리 범위를 벗어났습니다.";
                return false;
            }

            if (firstIndex == secondIndex)
            {
                message = "서로 다른 슬롯 2개를 선택해야 합니다.";
                return false;
            }

            if (TemporarySwapCount <= 0)
            {
                message = "사용 가능한 스왑 횟수가 없습니다.";
                return false;
            }

            if (!slots[firstIndex].HasItem && !slots[secondIndex].HasItem)
            {
                message = "빈 칸 2개는 스왑할 필요가 없습니다.";
                return false;
            }

            var temp = slots[firstIndex].item;
            slots[firstIndex].item = slots[secondIndex].item;
            slots[secondIndex].item = temp;
            TemporarySwapCount--;
            message = "아이템 위치를 교환했습니다. 남은 스왑: " + TemporarySwapCount;
            return true;
        }

        // 슬롯 강화 수치를 설정합니다.
        public bool TrySetSlotUpgrade(int index, int level)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            slots[index].SetUpgradeLevel(level);
            return true;
        }

        // 슬롯 강화 수치에 변화량을 더합니다. 정식 강화 시스템에서는 이 변화량만 결정해서 넘기면 됩니다.
        public bool TryAddSlotUpgrade(int index, int upgradeAmount)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            slots[index].AddUpgradeLevel(upgradeAmount);
            return true;
        }

        // 슬롯 프레임 속성을 설정합니다.
        public bool TrySetFrameElement(int index, string elementId)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            slots[index].SetFrameElement(elementId);
            return true;
        }

        // 슬롯 봉인 상태를 설정합니다.
        public bool TrySetSealed(int index, bool sealedState)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            slots[index].SetSealed(sealedState);
            return true;
        }

        // 현재 비어 있는 슬롯 인덱스 목록을 반환합니다.
        private List<int> GetEmptyIndices()
        {
            var emptyIndices = new List<int>();

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].HasItem)
                {
                    emptyIndices.Add(i);
                }
            }

            return emptyIndices;
        }
    }
}
