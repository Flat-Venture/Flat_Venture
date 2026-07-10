using System;
using System.Collections.Generic;

namespace FlatVenture.Inventory
{
    // 인벤토리 한 칸의 상태입니다. 아이템과 프레임 정보는 서로 분리해서 관리합니다.
    [Serializable]
    public sealed class InventorySlot
    {
        public readonly InventoryPosition position;
        public InventoryItem item;
        public string frameElementId;
        public int upgradeLevel;
        public bool isSealed;

        public InventorySlot(InventoryPosition position)
        {
            this.position = position;
        }

        public bool HasItem
        {
            get { return item != null; }
        }

        // 프레임 속성을 지정합니다. 이 속성은 아이템 효과가 아니라 빙고 판정에만 사용합니다.
        public void SetFrameElement(string elementId)
        {
            frameElementId = elementId;
        }

        // 슬롯 강화 수치를 변경합니다. UI에서는 +1, +2처럼 오른쪽 위에 표시하면 됩니다.
        public void SetUpgradeLevel(int level)
        {
            upgradeLevel = Math.Max(0, level);
        }

        // 현재 강화 수치에 지정한 변화량을 더합니다. 음수 값이 들어와도 최종 수치는 0 아래로 내려가지 않습니다.
        public void AddUpgradeLevel(int upgradeAmount)
        {
            SetUpgradeLevel(upgradeLevel + upgradeAmount);
        }

        // 보스 패턴 등으로 슬롯을 봉인하거나 해제합니다.
        public void SetSealed(bool sealedState)
        {
            isSealed = sealedState;
        }

        // 빙고 판정에 사용할 모든 속성을 목록에 추가합니다.
        // 프레임 속성은 아이템 속성을 덮어쓰지 않고, 같은 칸에 속성을 하나 더 추가하는 방식으로 해석합니다.
        public void AddBingoElementIds(List<string> elementIds)
        {
            if (elementIds == null || isSealed)
            {
                return;
            }

            if (item != null)
            {
                for (int i = 0; i < item.elements.Count; i++)
                {
                    AddUniqueElementId(elementIds, item.elements[i].elementId);
                }
            }

            AddUniqueElementId(elementIds, frameElementId);
        }

        // 같은 속성이 중복으로 들어가지 않도록 추가합니다.
        private static void AddUniqueElementId(List<string> elementIds, string elementId)
        {
            if (string.IsNullOrEmpty(elementId) || elementIds.Contains(elementId))
            {
                return;
            }

            elementIds.Add(elementId);
        }
    }
}
