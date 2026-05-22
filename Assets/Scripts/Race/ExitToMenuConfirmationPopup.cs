using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class ExitToMenuConfirmationPopup : BaseWindow
{
    [SerializeField] Button _confirmButton;
    [SerializeField] Button _cancelButton;

    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] GlobalSettings _settings;

    protected override void Awake()
    {
        base.Awake();
        AutoBindButtons();
        _confirmButton?.onClick.AddListener(ConfirmExitToMenu);
        _cancelButton?.onClick.AddListener(Cancel);
    }

    void OnDestroy()
    {
        _confirmButton?.onClick.RemoveListener(ConfirmExitToMenu);
        _cancelButton?.onClick.RemoveListener(Cancel);
    }

    void ConfirmExitToMenu()
    {
        string sceneName = _settings != null ? _settings.MenuSceneName : null;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"{nameof(ExitToMenuConfirmationPopup)} has no menu scene name in GlobalSettings.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
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

    void AutoBindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name;
            if (_confirmButton == null &&
                (buttonName.IndexOf("Confirm", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 buttonName.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 buttonName.IndexOf("Yes", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _confirmButton = buttons[i];
                continue;
            }

            if (_cancelButton == null &&
                (buttonName.IndexOf("Cancel", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 buttonName.IndexOf("No", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _cancelButton = buttons[i];
            }
        }

        if (_confirmButton == null && buttons.Length > 0)
        {
            _confirmButton = buttons[0];
        }
        if (_cancelButton == null && buttons.Length > 1)
        {
            _cancelButton = buttons[1];
        }
    }
}
