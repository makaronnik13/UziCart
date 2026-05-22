using UnityEngine;

public class RaceParticipant
{
    public RaceParticipant(CarConfigSO carConfig, PrometeoCarController controller, bool isPlayer)
    {
        CarConfig = carConfig;
        Controller = controller;
        IsPlayer = isPlayer;
    }

    public CarConfigSO CarConfig { get; }
    public PrometeoCarController Controller { get; }
    public bool IsPlayer { get; }
    public int CompletedLaps { get; set; }
    public int NextCheckpointIndex { get; set; }
    public float TotalTime { get; set; }
    public int FinishPlace { get; set; }
    public bool Finished { get; set; }
    public string DisplayName => CarConfig != null ? CarConfig.CarName : Controller != null ? Controller.name : "Car";
}
