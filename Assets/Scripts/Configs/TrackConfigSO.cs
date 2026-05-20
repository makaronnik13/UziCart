using UnityEngine;

[CreateAssetMenu(fileName = "TrackConfig", menuName = "Meta/Track Config")]
public class TrackConfigSO : ScriptableObject
{
    public string TrackName = "Track";
    public string SceneName;
    public Sprite Preview;
}
