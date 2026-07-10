using System;

namespace FlatVenture.Inventory
{
    // 인벤토리 한 칸의 좌표와 1차원 인덱스를 함께 보관합니다.
    [Serializable]
    public struct InventoryPosition
    {
        public int index;
        public int x;
        public int y;

        public InventoryPosition(int index, int x, int y)
        {
            this.index = index;
            this.x = x;
            this.y = y;
        }

        // 로그와 디버그 UI에서 좌표를 보기 좋게 표시합니다.
        public override string ToString()
        {
            return "(" + x + ", " + y + ") #" + index;
        }
    }
}
