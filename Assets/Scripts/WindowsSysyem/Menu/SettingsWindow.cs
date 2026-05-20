using Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SettingsWindow : BaseWindow
{
    [SerializeField] VolumeController[] _volumeControllers;
    [SerializeField] Button _backButton;

    [Inject(Optional = true)] SoundService _soundService;
    [Inject(Optional = true)] IWindowsService _windowsService;

    protected override void Awake()
    {
        base.Awake();
        _backButton?.onClick.AddListener(Close);
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        if (_soundService == null)
        {
            _soundService = FindFirstObjectByType<SoundService>();
        }

        RefreshControllers();
    }

    void OnDestroy()
    {
        _backButton?.onClick.RemoveListener(Close);
    }

    void RefreshControllers()
    {
        if (_volumeControllers == null || _volumeControllers.Length == 0)
        {
            _volumeControllers = GetComponentsInChildren<VolumeController>(true);
        }

        for (int i = 0; i < _volumeControllers.Length; i++)
        {
            if (_volumeControllers[i] != null)
            {
                _volumeControllers[i].Initialize(_soundService);
            }
        }
    }

    void Close()
    {
        if (_windowsService != null && WindowId != null)
        {
            _windowsService.Close(WindowId);
            return;
        }

        HideInstant();
    }
}
