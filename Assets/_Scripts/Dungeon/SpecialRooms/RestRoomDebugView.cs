using UnityEngine;

// 휴식 방 테스트용 임시 OnGUI 화면입니다. 실제 기능은 RestRoomController에 위임합니다.
public sealed class RestRoomDebugView : MonoBehaviour
{
    [SerializeField] private RestRoomController controller;

    // 같은 오브젝트에 붙은 휴식 방 컨트롤러를 자동으로 연결합니다.
    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<RestRoomController>();
        }
    }

    // 휴식 방 임시 선택지 UI를 화면에 그립니다.
    private void OnGUI()
    {
        if (controller == null || !controller.IsOpen || FlatVenture.SaveLoad.PauseMenuBehaviour.IsAnyOpen)
        {
            return;
        }

        var rect = new Rect((Screen.width - 420f) * 0.5f, (Screen.height - 280f) * 0.5f, 420f, 280f);
        GUI.Box(rect, "\uD734\uC2DD \uC7A5\uC18C");

        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 38f, rect.width - 40f, rect.height - 56f));
        GUILayout.Label("\uD558\uB098\uC758 \uC120\uD0DD\uC9C0\uB9CC \uACE0\uB97C \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
        GUILayout.Space(12f);

        if (GUILayout.Button("\uD734\uC2DD (HP \uD68C\uBCF5 - \uCD94\uD6C4 \uC5F0\uACB0)", GUILayout.Height(36f)))
        {
            controller.SelectRest();
        }

        if (GUILayout.Button("\uC544\uC774\uD15C \uC704\uCE58 \uC2A4\uC651 " + controller.SwapCount + "\uD68C", GUILayout.Height(36f)))
        {
            controller.SelectSwap();
        }

        if (GUILayout.Button("\uB2EB\uAE30", GUILayout.Height(32f)))
        {
            controller.CloseUI();
        }

        GUILayout.Space(8f);
        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }
}
