using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    public enum VolumeChannel
    {
        Music,
        Sfx
    }

    [SerializeField] VolumeChannel _channel;
    [SerializeField] Slider _slider;
    [SerializeField] Toggle _toggle;
    [SerializeField] TMP_Text _valueText;

    SoundService _soundService;
    bool _initialized;

    public void Construct(SoundService soundService)
    {
        Initialize(soundService);
    }

    void Awake()
    {
        AutoBind();
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(SoundService soundService)
    {
        _soundService = soundService;
        AutoBind();
        Subscribe();
        Refresh();
    }

    void Subscribe()
    {
        if (_initialized)
        {
            return;
        }

        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(SetVolume);
        }

        if (_toggle != null)
        {
            _toggle.onValueChanged.AddListener(SetEnabled);
        }

        _initialized = true;
    }

    void Unsubscribe()
    {
        if (!_initialized)
        {
            return;
        }

        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(SetVolume);
        }

        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(SetEnabled);
        }

        _initialized = false;
    }

    void Refresh()
    {
        if (_soundService == null)
        {
            _soundService = FindFirstObjectByType<SoundService>();
        }

        float volume = GetVolume();
        bool muted = IsMuted();

        if (_slider != null)
        {
            _slider.SetValueWithoutNotify(volume);
        }

        if (_toggle != null)
        {
            _toggle.SetIsOnWithoutNotify(!muted);
        }

        SetValueText(volume, muted);
    }

    void SetVolume(float value)
    {
        if (_soundService == null)
        {
            return;
        }

        value = Mathf.Clamp01(value);
        if (_channel == VolumeChannel.Music)
        {
            _soundService.SetMusicVolume(value);
        }
        else
        {
            _soundService.SetSfxVolume(value);
        }

        SetValueText(value, IsMuted());
    }

    void SetEnabled(bool enabled)
    {
        if (_soundService == null)
        {
            return;
        }

        if (_channel == VolumeChannel.Music)
        {
            _soundService.SetMusicMuted(!enabled);
        }
        else
        {
            _soundService.SetSfxMuted(!enabled);
        }

        SetValueText(GetVolume(), !enabled);
    }

    float GetVolume()
    {
        if (_soundService == null)
        {
            return _slider != null ? _slider.value : 1f;
        }

        return _channel == VolumeChannel.Music
            ? _soundService.GetMusicVolume()
            : _soundService.GetSfxVolume();
    }

    bool IsMuted()
    {
        if (_soundService == null)
        {
            return _toggle != null && !_toggle.isOn;
        }

        return _channel == VolumeChannel.Music
            ? _soundService.IsMusicMuted()
            : _soundService.IsSfxMuted();
    }

    void SetValueText(float volume, bool muted)
    {
        string value = muted ? "Off" : Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f) + "%";
        if (_valueText != null)
        {
            _valueText.text = value;
        }
    }

    void AutoBind()
    {
        if (_slider == null)
        {
            _slider = GetComponentInChildren<Slider>(true);
        }

        if (_toggle == null)
        {
            _toggle = GetComponentInChildren<Toggle>(true);
        }

        if (_valueText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name.IndexOf("Value", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _valueText = texts[i];
                    break;
                }
            }
        }

    }
}
