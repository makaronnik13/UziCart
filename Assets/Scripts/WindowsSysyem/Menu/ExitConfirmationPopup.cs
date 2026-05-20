using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ExitConfirmationPopup : BaseWindow
{
    [SerializeField] Button _confirmButton;
    [SerializeField] Button _cancelButton;

    [Inject(Optional = true)] IWindowsService _windowsService;

    protected override void Awake()
    {
        base.Awake();
        _confirmButton?.onClick.AddListener(ConfirmExit);
        _cancelButton?.onClick.AddListener(Cancel);
    }

    void OnDestroy()
    {
        _confirmButton?.onClick.RemoveListener(ConfirmExit);
        _cancelButton?.onClick.RemoveListener(Cancel);
    }

    void ConfirmExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Cancel()
    {
        if (_windowsService != null && WindowId != null)
        {
            _windowsService.Close(WindowId);
            return;
        }

        HideInstant();
    }
}
