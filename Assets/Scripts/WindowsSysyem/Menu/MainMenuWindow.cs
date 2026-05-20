using Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MainMenuWindow : BaseWindow
{
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
        if (_settings?.windowsConfig?.carSelectionWindowId != null)
        {
            _windowsService?.Open(_settings.windowsConfig.carSelectionWindowId);
        }
    }

    void OpenSettings()
    {
        if (_settings?.windowsConfig?.settingsWindowId != null)
        {
            _windowsService?.OpenPopup(_settings.windowsConfig.settingsWindowId);
        }
    }

    void OpenExitConfirmation()
    {
        if (_settings?.windowsConfig?.exitConfirmationPopupId != null)
        {
            _windowsService?.OpenPopup(_settings.windowsConfig.exitConfirmationPopupId);
        }
    }
}
