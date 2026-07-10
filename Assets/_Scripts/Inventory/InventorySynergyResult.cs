using System;
using System.Collections.Generic;

namespace FlatVenture.Inventory
{
    public enum InventoryLineDirection
    {
        Row,
        Column,
        DiagonalDown,
        DiagonalUp
    }

    // 완성된 빙고 한 줄의 정보입니다.
    [Serializable]
    public sealed class InventorySynergyLine
    {
        public InventoryLineDirection direction;
        public int lineIndex;
        public string elementId;
        public readonly List<int> slotIndices = new List<int>();

        // IEnumerable 는 여러 컬렉션 타입을 받을 수 있음.
        public InventorySynergyLine(InventoryLineDirection direction, int lineIndex, string elementId, IEnumerable<int> slotIndices)
        {
            this.direction = direction;
            this.lineIndex = lineIndex;
            this.elementId = elementId;

            // 리스트를 복제해 외부에서 리스트가 바뀌어도 내부 정보는 변하지 않음
            this.slotIndices.AddRange(slotIndices);
        }
    }

    // 현재 인벤토리에서 활성화된 속성 시너지 결과입니다.
    [Serializable]
    public sealed class InventorySynergyResult
    {
        public readonly List<InventorySynergyLine> completedLines = new List<InventorySynergyLine>();
        public readonly Dictionary<string, int> completedLineCountsByElement = new Dictionary<string, int>();
        public readonly Dictionary<string, int> elementPointCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> countSynergyLevelsByElement = new Dictionary<string, int>();

        // 완성된 줄을 결과에 추가하고 속성별 개수를 누적합니다.
        public void AddLine(InventorySynergyLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.elementId))
            {
                return;
            }

            completedLines.Add(line);

            int count;
            completedLineCountsByElement.TryGetValue(line.elementId, out count);
            completedLineCountsByElement[line.elementId] = count + 1;
        }

        // 아이템 속성 포인트를 누적하고 5포인트당 1단계 개수 시너지로 변환합니다.
        public void AddElementPoints(string elementId, int points)
        {
            if (string.IsNullOrEmpty(elementId) || points <= 0)
            {
                return;
            }

            int currentPoints;
            elementPointCounts.TryGetValue(elementId, out currentPoints);
            currentPoints += points;
            elementPointCounts[elementId] = currentPoints;

            int synergyLevel = currentPoints / 5;
            if (synergyLevel > 0)
            {
                countSynergyLevelsByElement[elementId] = synergyLevel;
            }
            else
            {
                countSynergyLevelsByElement.Remove(elementId);
            }
        }

        // 빙고 줄로 이미 활성화된 속성은 개수 시너지에서 제외합니다. (안전장치)
        public void RemoveCountSynergyForBingoElements()
        {
            foreach (var pair in completedLineCountsByElement)
            {
                countSynergyLevelsByElement.Remove(pair.Key);
            }
        }
    }
}
