using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 지도 UI에 대한 인풋 입력을 전담하는 컨트롤러
/// </summary>
public class MapInputController : MonoBehaviour
{
    private MapPresenter presenter;

    [Header("Input Action")]
    [Tooltip("Tab 키에 바인딩된 인풋 액션을 할당해주세요.")]
    [SerializeField] private InputAction tabAction;

    //Init 매서드를 통해 GameManager 등에서 Presenter를 주입받음
    public void Init(MapPresenter presenter)
    {
        this.presenter = presenter;
    }

    private void OnEnable()
    {
        tabAction.Enable();
        tabAction.started += OnTabStarted;
        tabAction.canceled += OnTabCanceled;
    }

    private void OnDisable()
    {
        tabAction.started -= OnTabStarted;
        tabAction.canceled -= OnTabCanceled;
        tabAction.Disable();
    }

    private void OnTabStarted(InputAction.CallbackContext context)
    {
        if (presenter != null) presenter.HandleTabStarted();
    }

    private void OnTabCanceled(InputAction.CallbackContext context)
    {
        if (presenter != null) presenter.HandleTabCanceled();
    }
}
