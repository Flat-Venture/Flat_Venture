using UnityEngine;

public class CanvasRegistrar : MonoBehaviour
{
    private RectTransform canvas;

    UIManager uiManager;
    private void Awake()
    {
        canvas = GetComponent<RectTransform>();
        if(canvas == null)
        {
            Debug.LogError("canvas�� ã�� �� ����.");
        }
    }

    private void Start()
    {
        uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.RegisterCanvas(canvas);
        }
    }

    private void OnDisable()
    {
        if (uiManager != null)
        {
            uiManager.UnRegisterCanvas(canvas);
        }
    }
}
