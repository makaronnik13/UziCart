using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "Game/GlobalSettings")]
public class GlobalSettings : ScriptableObject
{
    public bool testTools = false;
 
    public WindowsConfig windowsConfig;
    public InputActionsConfig inputActionsConfig;
  
    public string menuSceneName = "Menu";
    public string gameplaySceneName = "Gameplay";
   
    public List<CarConfigSO> cars = new List<CarConfigSO>();
    public List<TrackConfigSO> tracks = new List<TrackConfigSO>();

    public WindowId inGameMenuWindowId => windowsConfig != null ? windowsConfig.inGameMenuWindowId : null;
    public bool DebugTools => testTools;
  
}
