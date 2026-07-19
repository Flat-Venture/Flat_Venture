using UnityEngine;

namespace FlatVenture.Inventory
{
    // 던전 인벤토리에서 사용하는 골드를 관리합니다.
    public sealed class InventoryWallet
    {
        public int Gold { get; private set; }

        // 저장 데이터 복원이나 던전 시작처럼 기준 골드를 직접 지정할 때 사용합니다.
        public void SetGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
        }

        // 골드를 증가시킵니다.
        public void AddGold(int amount)
        {
            Gold += Mathf.Max(0, amount);
        }

        // 골드를 지불합니다. 부족하면 false를 반환합니다.
        public bool TrySpendGold(int amount, out string message)
        {
            amount = Mathf.Max(0, amount);
            if (Gold < amount)
            {
                message = "\uACE8\uB4DC\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4. \uD544\uC694 \uACE8\uB4DC: " + amount;
                return false;
            }

            Gold -= amount;
            message = amount + " \uACE8\uB4DC\uB97C \uC0AC\uC6A9\uD588\uC2B5\uB2C8\uB2E4.";
            return true;
        }

        // 골드를 0으로 초기화합니다.
        public void Clear()
        {
            Gold = 0;
        }
    }
}
