using Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PauseScreen : BaseWindow
{
    [SerializeField] VolumeController[] _volumeControllers;
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _exitToMenuButton;

    [Inject(Optional = true)] IPauseService _pauseService;
    [Inject(Optional = true)] SoundService _soundService;
    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] GlobalSettings _settings;

    bool _pauseRequested;

    protected override void Awake()
    {
        base.Awake();
        _resumeButton?.onClick.AddListener(Resume);
        _exitToMenuButton?.onClick.AddListener(OpenExitToMenuConfirmation);
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        ResolveServices();
        RequestPause();
        RefreshControllers();
    }

    protected override void OnHide()
    {
        base.OnHide();
        ReleasePause();
    }

    void OnDestroy()
    {
        _resumeButton?.onClick.RemoveListener(Resume);
        _exitToMenuButton?.onClick.RemoveListener(OpenExitToMenuConfirmation);
        ReleasePause();
    }

    void Resume()
    {
        if (_windowsService != null && WindowId != null)
        {
            _windowsService.Close(WindowId);
            return;
        }

        HideInstant();
    }

    void OpenExitToMenuConfirmation()
    {
        ResolveServices();
        WindowId exitToMenuId = _settings?.windowsConfig?.exitToMenuConfirmationPopupId;
        if (_windowsService != null && exitToMenuId != null)
        {
            _windowsService.OpenPopup(exitToMenuId);
            return;
        }

        Debug.LogError($"{nameof(PauseScreen)} cannot open exit to menu confirmation popup.", this);
    }

    void RequestPause()
    {
        if (_pauseRequested)
        {
            return;
        }

        _pauseService?.Pause();
        _pauseRequested = true;
    }

    void ReleasePause()
    {
        if (!_pauseRequested)
        {
            return;
        }

        _pauseService?.Resume();
        _pauseRequested = false;
    }

    void RefreshControllers()
    {
        if (_volumeControllers == null || _volumeControllers.Length == 0)
        {
            _volumeControllers = GetComponentsInChildren<VolumeController>(true);
        }

        for (int i = 0; i < _volumeControllers.Length; i++)
        {
            _volumeControllers[i]?.Initialize(_soundService);
        }
    }

    void ResolveServices()
    {
        _soundService ??= FindFirstObjectByType<SoundService>();
    }
}
