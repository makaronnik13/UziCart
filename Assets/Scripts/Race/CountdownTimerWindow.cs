using System;
using TMPro;
using UniRx;
using UnityEngine;

public class CountdownTimerWindow : BaseWindow
{
    [SerializeField] TMP_Text _timerText;
    [SerializeField, Min(0.05f)] float _stepDuration = 1f;
    [SerializeField, Min(0.05f)] float _scaleReturnDuration = 0.35f;
    [SerializeField, Min(1f)] float _popScale = 1.8f;

    readonly string[] _steps = { "3", "2", "1", "GO" };
    readonly SerialDisposable _scaleAnimation = new SerialDisposable();

    protected override void Awake()
    {
        base.Awake();
        if (_timerText == null)
        {
            _timerText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    void OnDestroy()
    {
        _scaleAnimation.Dispose();
    }

    public IObservable<Unit> PlayCountdown()
    {
        return ShowRoutine(null, false)
            .SelectMany(_ => PlaySteps())
            .SelectMany(_ => HideRoutine(false));
    }

    IObservable<Unit> PlaySteps()
    {
        IObservable<Unit> sequence = Observable.ReturnUnit();
        for (int i = 0; i < _steps.Length; i++)
        {
            string step = _steps[i];
            sequence = sequence
                .Do(_ => ShowStep(step))
                .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(_stepDuration)).AsUnitObservable());
        }

        return sequence;
    }

    void ShowStep(string value)
    {
        if (_timerText == null)
        {
            Debug.LogError($"{nameof(CountdownTimerWindow)} has no TMP text.", this);
            return;
        }

        _timerText.text = value;
        _timerText.transform.localScale = Vector3.one * _popScale;
        _scaleAnimation.Disposable = AnimateScale(_timerText.transform, Vector3.one, _scaleReturnDuration)
            .Subscribe()
            .AddTo(this);
    }

    static IObservable<Unit> AnimateScale(Transform target, Vector3 endScale, float duration)
    {
        return Observable.Create<Unit>(observer =>
        {
            if (target == null)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }

            Vector3 startScale = target.localScale;
            float elapsed = 0f;
            IDisposable subscription = null;
            subscription = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (target == null)
                {
                    observer.OnCompleted();
                    subscription.Dispose();
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                target.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);

                if (t >= 1f)
                {
                    target.localScale = endScale;
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                    subscription.Dispose();
                }
            });

            return subscription;
        });
    }
}
