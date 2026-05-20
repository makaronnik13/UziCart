using Sound;
using UnityEngine;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    [SerializeField] SoundService _soundService;
    [SerializeField] MetaGameService _metaGameService;
    [SerializeField] GlobalSettings _globalSettings;
    [SerializeField] PauseService _pauseService = new PauseService();

    public override void InstallBindings()
    {
        BindGlobalSettings();
        BindPauseService();
        BindSoundService();
        BindMetaGameService();
        BindWindowsService();
    }

    void BindGlobalSettings()
    {
        Container.Bind<GlobalSettings>()
            .FromInstance(_globalSettings)
            .AsSingle();
    }

    void BindPauseService()
    {
        if (_pauseService == null)
        {
            Debug.LogError($"{nameof(ProjectContextInstaller)} has no {nameof(PauseService)} reference.", this);
            _pauseService = new PauseService();
        }

        Container.BindInterfacesAndSelfTo<PauseService>()
            .FromInstance(_pauseService)
            .AsSingle();
    }

    void BindSoundService()
    {
        if (_soundService == null)
        {
            Debug.LogError($"{nameof(ProjectContextInstaller)} has no {nameof(SoundService)} reference.", this);
            return;
        }

        Container.Bind<SoundService>()
            .FromInstance(_soundService)
            .AsSingle();
        Container.QueueForInject(_soundService);
    }

    void BindMetaGameService()
    {
        if (_metaGameService == null)
        {
            Debug.LogError($"{nameof(ProjectContextInstaller)} has no {nameof(MetaGameService)} reference.", this);
            return;
        }

        Container.Bind<MetaGameService>()
            .FromInstance(_metaGameService)
            .AsSingle();
        Container.Bind<IRuntimeResettable>()
            .To<MetaGameService>()
            .FromResolve();
        Container.QueueForInject(_metaGameService);
    }

    void BindWindowsService()
    {
        if (_globalSettings == null)
        {
            Debug.LogError($"{nameof(ProjectContextInstaller)} has no {nameof(GlobalSettings)} reference.", this);
        }

        Container.BindInterfacesAndSelfTo<WindowsService>()
            .AsSingle();
    }
}
