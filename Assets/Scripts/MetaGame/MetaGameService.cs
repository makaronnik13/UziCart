using UnityEngine;

public class MetaGameService : MonoBehaviour, IRuntimeResettable
{
    const string SelectedCarIndexKey = "MetaGame.SelectedCarIndex";
    const string SelectedTrackIndexKey = "MetaGame.SelectedTrackIndex";

    public int SelectedCarIndex { get; private set; }
    public int SelectedTrackIndex { get; private set; }

    public void Start()
    {
        Load();
    }

    public void SelectCar(int index)
    {
        SelectedCarIndex = Mathf.Max(0, index);
        PlayerPrefs.SetInt(SelectedCarIndexKey, SelectedCarIndex);
        PlayerPrefs.Save();
    }

    public void SelectTrack(int index)
    {
        SelectedTrackIndex = Mathf.Max(0, index);
        PlayerPrefs.SetInt(SelectedTrackIndexKey, SelectedTrackIndex);
        PlayerPrefs.Save();
    }

    public void ResetRuntimeState()
    {
        Load();
    }

    void Load()
    {
        SelectedCarIndex = PlayerPrefs.GetInt(SelectedCarIndexKey, 0);
        SelectedTrackIndex = PlayerPrefs.GetInt(SelectedTrackIndexKey, 0);
    }
}
