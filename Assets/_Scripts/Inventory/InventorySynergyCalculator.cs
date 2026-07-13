using System.Collections.Generic;
using FlatVenture.ItemData;

namespace FlatVenture.Inventory
{
    // 인벤토리의 가로, 세로, 대각선 빙고 줄을 검사합니다.
    public sealed class InventorySynergyCalculator
    {
        private readonly GameDataCatalog catalog;

        public InventorySynergyCalculator(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        // 현재 인벤토리에서 완성된 같은 속성 줄을 계산합니다.
        public InventorySynergyResult Calculate(InventoryGrid grid)
        {
            var result = new InventorySynergyResult();
            if (grid == null)
            {
                return result;
            }

            CheckRows(grid, result);
            CheckColumns(grid, result);
            CheckDiagonals(grid, result);
            CountElementPoints(grid, result);
            result.RemoveCountSynergyForBingoElements();
            return result;
        }

        // 봉인되지 않은 슬롯의 sub 속성 포인트를 합산합니다.
        // 프레임 sub 속성은 아이템 속성을 바꾸지 않고 해당 칸에 +1 포인트를 더합니다.
        private void CountElementPoints(InventoryGrid grid, InventorySynergyResult result)
        {
            for (int i = 0; i < grid.Slots.Count; i++)
            {
                var slot = grid.Slots[i];
                if (slot == null || slot.isSealed)
                {
                    continue;
                }

                if (slot.item != null)
                {
                    for (int j = 0; j < slot.item.elements.Count; j++)
                    {
                        var element = slot.item.elements[j];
                        if (CountsForCountSynergy(element.elementId))
                        {
                            result.AddElementPoints(element.elementId, element.elementValue);
                        }
                    }
                }

                if (CountsForCountSynergy(slot.frameElementId))
                {
                    result.AddElementPoints(slot.frameElementId, 1);
                }
            }
        }

        // 모든 가로 줄을 검사합니다.
        private void CheckRows(InventoryGrid grid, InventorySynergyResult result)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var indices = new List<int>();
                for (int x = 0; x < grid.Width; x++)
                {
                    indices.Add(grid.ToIndex(x, y));
                }

                TryAddCompletedLine(grid, result, InventoryLineDirection.Row, y, indices);
            }
        }

        // 모든 세로 줄을 검사합니다.
        private void CheckColumns(InventoryGrid grid, InventorySynergyResult result)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var indices = new List<int>();
                for (int y = 0; y < grid.Height; y++)
                {
                    indices.Add(grid.ToIndex(x, y));
                }

                TryAddCompletedLine(grid, result, InventoryLineDirection.Column, x, indices);
            }
        }

        // 왼쪽 위에서 오른쪽 아래, 왼쪽 아래에서 오른쪽 위 대각선 2줄을 검사합니다.
        private void CheckDiagonals(InventoryGrid grid, InventorySynergyResult result)
        {
            if (grid.Width != grid.Height)
            {
                return;
            }

            var downIndices = new List<int>();
            var upIndices = new List<int>();

            for (int i = 0; i < grid.Width; i++)
            {
                downIndices.Add(grid.ToIndex(i, i));
                upIndices.Add(grid.ToIndex(i, grid.Height - 1 - i));
            }

            TryAddCompletedLine(grid, result, InventoryLineDirection.DiagonalDown, 0, downIndices);
            TryAddCompletedLine(grid, result, InventoryLineDirection.DiagonalUp, 1, upIndices);
        }

        // 지정한 줄에서 모든 슬롯이 공유하는 빙고 속성을 찾아 결과에 추가합니다.
        private void TryAddCompletedLine(InventoryGrid grid, InventorySynergyResult result, InventoryLineDirection direction, int lineIndex, List<int> indices)
        {
            List<string> commonElementIds = null;

            for (int i = 0; i < indices.Count; i++)
            {
                var slot = grid.GetSlot(indices[i]);
                if (slot == null)
                {
                    return;
                }

                var slotElementIds = GetBingoElementIds(slot);
                if (slotElementIds.Count == 0)
                {
                    return;
                }

                if (i == 0)
                {
                    commonElementIds = slotElementIds;
                    continue;
                }

                IntersectElements(commonElementIds, slotElementIds);
                if (commonElementIds.Count == 0)
                {
                    return;
                }
            }

            for (int i = 0; i < commonElementIds.Count; i++)
            {
                result.AddLine(new InventorySynergyLine(direction, lineIndex, commonElementIds[i], indices));
            }
        }

        // 슬롯이 빙고 판정에 제공하는 속성 중 실제 빙고 대상만 가져옵니다.
        private List<string> GetBingoElementIds(InventorySlot slot)
        {
            var elementIds = new List<string>();
            slot.AddBingoElementIds(elementIds);

            for (int i = elementIds.Count - 1; i >= 0; i--)
            {
                if (!CountsForBingo(elementIds[i]))
                {
                    elementIds.RemoveAt(i);
                }
            }

            return elementIds;
        }

        // 공통 속성 목록을 현재 슬롯이 가진 속성과 교집합으로 줄입니다.
        private static void IntersectElements(List<string> commonElementIds, List<string> slotElementIds)
        {
            for (int i = commonElementIds.Count - 1; i >= 0; i--)
            {
                if (!slotElementIds.Contains(commonElementIds[i]))
                {
                    commonElementIds.RemoveAt(i);
                }
            }
        }

        // main 속성만 빙고 줄 시너지에 포함합니다.
        private bool CountsForBingo(string elementId)
        {
            if (string.IsNullOrEmpty(elementId))
            {
                return false;
            }

            if (catalog == null || catalog.elements == null)
            {
                return true;
            }

            ElementDefinition definition;
            return !catalog.elements.TryGetValue(elementId, out definition)
                ? true
                : definition.elementRole == "main";
        }

        // sub 속성만 개수 시너지에 포함합니다.
        private bool CountsForCountSynergy(string elementId)
        {
            if (string.IsNullOrEmpty(elementId))
            {
                return false;
            }

            if (catalog == null || catalog.elements == null)
            {
                return true;
            }

            ElementDefinition definition;
            return catalog.elements.TryGetValue(elementId, out definition)
                && definition.elementRole == "sub";
        }
    }
}
