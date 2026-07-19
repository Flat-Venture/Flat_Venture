using UnityEngine;

// 제련소 테스트용 임시 OnGUI 화면입니다. 실제 기능은 ForgeRoomController가 담당합니다.
public sealed class ForgeRoomDebugView : MonoBehaviour
{
    [SerializeField] private ForgeRoomController controller;

    // 같은 오브젝트에 붙은 제련소 컨트롤러를 자동으로 연결합니다.
    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<ForgeRoomController>();
        }
    }

    // 제련소 임시 UI를 화면에 그립니다.
    private void OnGUI()
    {
        if (controller == null || !controller.IsOpen || controller.InventoryRuntime == null || FlatVenture.SaveLoad.PauseMenuBehaviour.IsAnyOpen)
        {
            return;
        }

        var rect = new Rect((Screen.width - 760f) * 0.5f, (Screen.height - 520f) * 0.5f, 760f, 520f);
        GUI.Box(rect, "\uC81C\uB828\uC18C");
        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 34f, rect.width - 40f, rect.height - 50f));
        GUILayout.Label("\uACE8\uB4DC: " + controller.InventoryRuntime.Wallet.Gold);

        if (controller.Mode == ForgeRoomController.ForgeMode.Menu)
        {
            DrawMenu();
        }
        else
        {
            DrawForgeSubMode();
        }

        GUILayout.Space(10f);
        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }

    // 제련소의 세 가지 선택지 버튼을 그립니다.
    private void DrawMenu()
    {
        GUILayout.Label("\uD558\uB098\uC758 \uAE30\uB2A5\uB9CC \uC0AC\uC6A9\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
        if (GUILayout.Button("\uC2AC\uB86F \uAC15\uD654 (" + controller.SlotUpgradeCostValue + " \uACE8\uB4DC)", GUILayout.Height(36f)))
        {
            controller.OpenSlotUpgradeMode();
        }

        if (GUILayout.Button("\uD504\uB808\uC784 \uC18D\uC131 \uBD80\uC5EC (" + controller.FrameElementCostValue + " \uACE8\uB4DC)", GUILayout.Height(36f)))
        {
            controller.OpenFrameElementMode();
        }

        if (GUILayout.Button("\uC544\uC774\uD15C \uB4F1\uAE09\uC5C5 (" + controller.ItemRarityUpgradeCostValue + " \uACE8\uB4DC)", GUILayout.Height(36f)))
        {
            controller.OpenItemRarityUpgradeMode();
        }

        if (GUILayout.Button("\uB2EB\uAE30", GUILayout.Height(32f)))
        {
            controller.CloseUI();
        }
    }

    // 현재 제련소 하위 모드에 맞는 임시 UI를 그립니다.
    private void DrawForgeSubMode()
    {
        if (controller.Mode == ForgeRoomController.ForgeMode.FrameElement)
        {
            DrawElementButtons();
            return;
        }

        GUILayout.Label("\uC778\uBCA4\uD1A0\uB9AC \uD328\uB110\uC5D0\uC11C \uC2AC\uB86F\uC744 \uC120\uD0DD\uD558\uC138\uC694.");
        if (GUILayout.Button("\uC120\uD0DD \uCDE8\uC18C", GUILayout.Width(160f), GUILayout.Height(32f)))
        {
            controller.ReturnToForgeMenu();
        }
    }

    // 프레임에 부여할 속성 선택 버튼을 그립니다.
    private void DrawElementButtons()
    {
        GUILayout.Label("\uC120\uD0DD \uC2AC\uB86F: " + controller.SelectedSlot);
        var elements = controller.InventoryRuntime.SortedElementIds;
        for (int i = 0; i < elements.Count; i++)
        {
            if (GUILayout.Button(controller.InventoryRuntime.GetElementDisplayName(elements[i]), GUILayout.Height(30f)))
            {
                controller.SelectFrameElement(elements[i]);
            }
        }

        if (GUILayout.Button("\uD504\uB808\uC784 \uC18D\uC131 \uC81C\uAC70", GUILayout.Height(30f)))
        {
            controller.ClearFrameElement();
        }

        if (GUILayout.Button("\uC120\uD0DD \uCDE8\uC18C", GUILayout.Height(30f)))
        {
            controller.ReturnToForgeMenu();
        }
    }
}
