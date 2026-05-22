using System.ComponentModel;
using UnityEngine;
using Zenject;

public class RaceInstaller : MonoInstaller
{
    [SerializeField] private RaceController raceController;

    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<RaceController>().FromInstance(raceController).AsSingle();
    }

    
}
