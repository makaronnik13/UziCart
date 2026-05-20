using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _nameText;
    [SerializeField] Slider _speedSlider;
    [SerializeField] Slider _handlingSlider;
    [SerializeField] Slider _lethalitySlider;
    [SerializeField, Min(0.01f)] float _animationDuration = 0.25f;
    [SerializeField] AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    readonly SerialDisposable _animationDisposable = new SerialDisposable();

    void Awake()
    {
        AutoBind();
        SetStatsInteractable(false);
    }

    void OnDestroy()
    {
        _animationDisposable.Dispose();
    }

    public void SetCharacter(CarConfigSO character)
    {
        if (_nameText != null)
        {
            _nameText.text = character != null ? character.CarName : "No character";
        }
      

        AnimateSliders(
            NormalizeStat(character != null ? character.Speed : 0),
            NormalizeStat(character != null ? character.Handling : 0),
            NormalizeStat(character != null ? character.Lethality : 0));
    }

    void AnimateSliders(float speed, float handling, float lethality)
    {
        _animationDisposable.Disposable = null;

        float startSpeed = GetSliderValue(_speedSlider);
        float startHandling = GetSliderValue(_handlingSlider);
        float startLethality = GetSliderValue(_lethalitySlider);
        float duration = Mathf.Max(0.01f, _animationDuration);
        float elapsed = 0f;

        IDisposable subscription = null;
        subscription = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float t = _animationCurve != null ? _animationCurve.Evaluate(normalizedTime) : normalizedTime;

                SetSlider(_speedSlider, Mathf.Lerp(startSpeed, Mathf.Clamp01(speed), t));
                SetSlider(_handlingSlider, Mathf.Lerp(startHandling, Mathf.Clamp01(handling), t));
                SetSlider(_lethalitySlider, Mathf.Lerp(startLethality, Mathf.Clamp01(lethality), t));

                if (normalizedTime >= 1f)
                {
                    subscription?.Dispose();
                    if (ReferenceEquals(_animationDisposable.Disposable, subscription))
                    {
                        _animationDisposable.Disposable = null;
                    }
                }
            });
        _animationDisposable.Disposable = subscription;
    }

    void SetStatsInteractable(bool interactable)
    {
        if (_speedSlider != null) _speedSlider.interactable = interactable;
        if (_handlingSlider != null) _handlingSlider.interactable = interactable;
        if (_lethalitySlider != null) _lethalitySlider.interactable = interactable;
    }

    void AutoBind()
    {
        if (_nameText == null)
        {
            _nameText = GetComponentInChildren<TMP_Text>(true);
        }


        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            string sliderName = sliders[i].name;
            if (_speedSlider == null && sliderName.IndexOf("Speed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _speedSlider = sliders[i];
            }
            else if (_handlingSlider == null && sliderName.IndexOf("Handling", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _handlingSlider = sliders[i];
            }
            else if (_lethalitySlider == null && sliderName.IndexOf("Lethality", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _lethalitySlider = sliders[i];
            }
        }
    }

    static float GetSliderValue(Slider slider)
    {
        return slider != null ? slider.value : 0f;
    }

    static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }

    static float NormalizeStat(int value)
    {
        return Mathf.Clamp(value, 0, 5) / 5f;
    }
}
