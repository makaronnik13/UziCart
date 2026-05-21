using Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MainMenuWindow : BaseWindow
{
    const string LogPrefix = "[MainMenuWindow]";

    [SerializeField] Button _newGameButton;
    [SerializeField] Button _settingsButton;
    [SerializeField] Button _exitButton;
    [SerializeField] MusicAsset _music;
    [SerializeField] bool _playMusicInstant;

    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] GlobalSettings _settings;
    [Inject(Optional = true)] SoundService _soundService;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"{LogPrefix} Awake. Buttons: newGame={Describe(_newGameButton)}, settings={Describe(_settingsButton)}, exit={Describe(_exitButton)}", this);
        _newGameButton?.onClick.AddListener(OpenCarSelection);
        _settingsButton?.onClick.AddListener(OpenSettings);
        _exitButton?.onClick.AddListener(OpenExitConfirmation);
    }

    void OnDestroy()
    {
        _newGameButton?.onClick.RemoveListener(OpenCarSelection);
        _settingsButton?.onClick.RemoveListener(OpenSettings);
        _exitButton?.onClick.RemoveListener(OpenExitConfirmation);
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        Debug.Log($"{LogPrefix} OnShow. windowsService={Describe(_windowsService)}, settings={Describe(_settings)}, windowsConfig={Describe(_settings != null ? _settings.windowsConfig : null)}", this);
        PlayMenuMusic();
    }

    void PlayMenuMusic()
    {
        if (_music == null)
        {
            return;
        }

        if (_soundService == null)
        {
            _soundService = FindFirstObjectByType<SoundService>();
        }

        _soundService?.PlayMusic(_music, _playMusicInstant);
    }

    void OpenCarSelection()
    {
        Debug.Log($"{LogPrefix} New Game clicked. windowsService={Describe(_windowsService)}, carSelectionWindowId={Describe(_settings?.windowsConfig?.carSelectionWindowId)}", this);
        if (!CanOpenWindow(_settings?.windowsConfig?.carSelectionWindowId, "car selection"))
        {
            return;
        }

        OpenConfiguredWindow(_settings.windowsConfig.carSelectionWindowId);
    }

    void OpenSettings()
    {
        Debug.Log($"{LogPrefix} Settings clicked. windowsService={Describe(_windowsService)}, settingsWindowId={Describe(_settings?.windowsConfig?.settingsWindowId)}", this);
        if (!CanOpenWindow(_settings?.windowsConfig?.settingsWindowId, "settings"))
        {
            return;
        }

        OpenConfiguredWindow(_settings.windowsConfig.settingsWindowId);
    }

    void OpenExitConfirmation()
    {
        Debug.Log($"{LogPrefix} Exit clicked. windowsService={Describe(_windowsService)}, exitConfirmationPopupId={Describe(_settings?.windowsConfig?.exitConfirmationPopupId)}", this);
        if (!CanOpenWindow(_settings?.windowsConfig?.exitConfirmationPopupId, "exit confirmation"))
        {
            return;
        }

        OpenConfiguredWindow(_settings.windowsConfig.exitConfirmationPopupId);
    }

    void OpenConfiguredWindow(WindowId windowId)
    {
        if (_settings.windowsConfig.IsPopup(windowId))
        {
            _windowsService.OpenPopup(windowId);
            return;
        }

        _windowsService.Open(windowId, keepPrevious: true);
    }

    bool CanOpenWindow(WindowId windowId, string windowName)
    {
        if (_windowsService == null)
        {
            Debug.LogError($"{LogPrefix} Cannot open {windowName}: IWindowsService was not injected.", this);
            return false;
        }

        if (_settings == null)
        {
            Debug.LogError($"{LogPrefix} Cannot open {windowName}: GlobalSettings was not injected.", this);
            return false;
        }

        if (_settings.windowsConfig == null)
        {
            Debug.LogError($"{LogPrefix} Cannot open {windowName}: GlobalSettings.windowsConfig is null.", this);
            return false;
        }

        if (windowId == null)
        {
            Debug.LogError($"{LogPrefix} Cannot open {windowName}: WindowId is null in WindowsConfig.", this);
            return false;
        }

        return true;
    }

    static string Describe(Object target)
    {
        return target != null ? target.name : "null";
    }

    static string Describe(object target)
    {
        return target != null ? target.GetType().Name : "null";
    }

    static string Describe(WindowId windowId)
    {
        return windowId != null
            ? windowId.name
            : "null";
    }

    static string Describe(IWindowsService service)
    {
        return service != null ? service.GetType().Name : "null";
    }
}
