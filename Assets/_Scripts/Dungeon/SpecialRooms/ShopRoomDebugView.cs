using FlatVenture.ItemData;
using UnityEngine;

// 상점 테스트용 임시 OnGUI 화면입니다. 실제 구매/판매 기능은 ShopRoomController가 담당합니다.
public sealed class ShopRoomDebugView : MonoBehaviour
{
    [SerializeField] private ShopRoomController controller;

    // 같은 오브젝트에 붙은 상점 컨트롤러를 자동으로 연결합니다.
    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<ShopRoomController>();
        }
    }

    // 상점 임시 UI를 화면에 그립니다.
    private void OnGUI()
    {
        if (controller == null || !controller.IsOpen || controller.InventoryRuntime == null || FlatVenture.SaveLoad.PauseMenuBehaviour.IsAnyOpen)
        {
            return;
        }

        var rect = new Rect((Screen.width - 900f) * 0.5f, (Screen.height - 500f) * 0.5f, 900f, 500f);
        GUI.Box(rect, controller.IsSellMode ? "\uC544\uC774\uD15C \uD310\uB9E4" : "\uC0C1\uC810");

        if (controller.IsSellMode)
        {
            DrawSellMode(rect);
        }
        else
        {
            DrawShopMode(rect);
        }
    }

    // 상품 카드와 판매/닫기 버튼이 있는 기본 상점 화면을 그립니다.
    private void DrawShopMode(Rect rect)
    {
        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 34f, rect.width - 40f, rect.height - 50f));
        GUILayout.Label("\uACE8\uB4DC: " + controller.InventoryRuntime.Wallet.Gold);
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();

        for (int i = 0; i < controller.ShopItems.Count; i++)
        {
            DrawShopCard(i, controller.ShopItems[i]);
            GUILayout.Space(8f);
        }

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("\uC544\uC774\uD15C \uD310\uB9E4", GUILayout.Width(150f), GUILayout.Height(36f)))
        {
            controller.OpenSellMode();
        }

        if (GUILayout.Button("\uB2EB\uAE30", GUILayout.Width(150f), GUILayout.Height(36f)))
        {
            controller.CloseUI();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }

    // 상품 하나의 카드 UI를 그리고 구매 버튼을 컨트롤러에 연결합니다.
    private void DrawShopCard(int index, ItemRecord item)
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(160f), GUILayout.Height(280f));
        GUILayout.Label(item.definition.displayName);
        GUILayout.Label("\uB4F1\uAE09: " + item.definition.rarityId);
        GUILayout.Label("\uAC00\uACA9: " + controller.GetItemPrice(item));
        GUILayout.Label("\uD6A8\uACFC: " + item.effects.Count + "\uAC1C");
        GUILayout.Label("\uC2A4\uD0EF: " + item.stats.Count + "\uAC1C");
        GUILayout.FlexibleSpace();

        if (controller.IsItemSold(index))
        {
            GUILayout.Label("SOLD");
            GUI.enabled = false;
            GUILayout.Button("\uAD6C\uB9E4");
            GUI.enabled = true;
        }
        else if (GUILayout.Button("\uAD6C\uB9E4"))
        {
            controller.BuyItem(index);
        }

        GUILayout.EndVertical();
    }

    // 판매 모드 안내와 상점으로 돌아가기 버튼을 그립니다.
    private void DrawSellMode(Rect rect)
    {
        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 34f, rect.width - 40f, rect.height - 50f));
        GUILayout.Label("\uACE8\uB4DC: " + controller.InventoryRuntime.Wallet.Gold);
        GUILayout.Label("\uC5F4\uB9B0 \uC778\uBCA4\uD1A0\uB9AC\uC5D0\uC11C \uD310\uB9E4\uD560 \uC544\uC774\uD15C\uC744 \uD074\uB9AD\uD558\uC138\uC694.");
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("\uC0C1\uC810\uC73C\uB85C \uB3CC\uC544\uAC00\uAE30", GUILayout.Width(180f), GUILayout.Height(36f)))
        {
            controller.ReturnToShop();
        }

        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }
}
