using TMPro;
using UnityEngine;

public class FinishRaceItem : MonoBehaviour
{
    [SerializeField] TMP_Text _placeText;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _timeText;

    void Awake()
    {
        AutoBind();
    }

    public void Initialize(RaceParticipant participant)
    {
        AutoBind();
        if (participant == null)
        {
            return;
        }

        if (_placeText != null)
        {
            _placeText.text = participant.FinishPlace.ToString();
        }
        if (_nameText != null)
        {
            _nameText.text = participant.DisplayName;
        }
        if (_timeText != null)
        {
            _timeText.text = FormatTime(participant.TotalTime);
        }
    }

    void AutoBind()
    {
        if (_placeText != null && _nameText != null && _timeText != null)
        {
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0 && _placeText == null)
        {
            _placeText = texts[0];
        }
        if (texts.Length > 1 && _nameText == null)
        {
            _nameText = texts[1];
        }
        if (texts.Length > 2 && _timeText == null)
        {
            _timeText = texts[2];
        }
    }

    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds - minutes * 60f);
        int centiseconds = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
        return $"{minutes:00}:{wholeSeconds:00}:{centiseconds:00}";
    }
}
