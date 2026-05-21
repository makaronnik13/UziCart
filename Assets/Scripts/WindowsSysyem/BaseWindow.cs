using System;
using Sound;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class BaseWindow : MonoBehaviour
{
    const string ShowTrigger = "Show";
    const string HideTrigger = "Hide";
    const string ShowState = "Show";
    const string ShowingState = "Showing";
    const string HideState = "Hide";
    const string HiddenState = "Hidden";

    [SerializeField]
    private WindowId _windowId;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private bool _useUnscaledTime = true;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private GameObject _selectOnShow;

    [SerializeField]
    protected bool showOnStart = false;

    [SerializeField]
    protected bool preventHide = false;

    [SerializeField]
    protected bool blocksCameraInput = true;

    [SerializeField] private UISound _openSound, _closeSound;
    
    public WindowId WindowId => _windowId;
    public bool PreventHide => preventHide;
    public bool BlocksCameraInput => blocksCameraInput;
    public bool IsVisible { get; private set; }
    public virtual bool AddToWindowStack => true;
    public virtual bool CloseByBack => true;
    protected virtual bool DisableGameObjectWhenHidden => true;

    [Inject] protected SoundService soundService;

    IDisposable _transitionSubscription;
    
    protected virtual void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("_animator is null in BaseWindow.Awake");
            }
        }
        if (_animator != null && _useUnscaledTime)
        {
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    protected virtual void Start()
    {
        if (showOnStart)
        {
            ShowRoutine(null, true).Subscribe().AddTo(this);
        }
    }

    public void ShowInstant(object payload = null)
    {
        StopTransition();
        ShowRoutine(payload, true).Subscribe();
    }

    public void HideInstant()
    {
        StopTransition();
        HideRoutine(true).Subscribe();
    }

    protected virtual void OnDisable()
    {
        StopTransition();
    }

    public IObservable<Unit> ShowRoutine(object payload, bool instant)
    {
        return Observable.Create<Unit>(observer =>
        {
            BaseWindow window = this;
            window.StopTransition();
            window.OnShow(payload);

            if (!window.gameObject.activeSelf)
            {
                window.gameObject.SetActive(true);
            }

            if (window._canvasGroup != null)
            {
                window._canvasGroup.alpha = 1f;
                window._canvasGroup.interactable = false;
                window._canvasGroup.blocksRaycasts = false;
            }

            if (instant || window._animator == null)
            {
                if (window._animator != null)
                {
                    window._animator.Play(ShowState, 0, 1f);
                    window._animator.Update(0f);
                }

                window.ApplyShownState();
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            }

            window._animator.ResetTrigger(HideTrigger);
            window._animator.SetTrigger(ShowTrigger);

            string targetState = window.GetShownStateName();
            IDisposable waitSubscription = null;
            waitSubscription = window.WaitForState(targetState).Subscribe(
                _ => { },
                observer.OnError,
                () =>
                {
                    if (ReferenceEquals(window._transitionSubscription, waitSubscription))
                    {
                        window._transitionSubscription = null;
                    }

                    window.ApplyShownState();
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });

            window._transitionSubscription = waitSubscription;
            return Disposable.Create(() =>
            {
                waitSubscription.Dispose();
                if (ReferenceEquals(window._transitionSubscription, waitSubscription))
                {
                    window._transitionSubscription = null;
                }
            });
        });
    }

    public IObservable<Unit> HideRoutine(bool instant)
    {
        return Observable.Create<Unit>(observer =>
        {
            BaseWindow window = this;
            window.StopTransition();
            window.OnHide();

            if (window._canvasGroup != null)
            {
                window._canvasGroup.interactable = false;
                window._canvasGroup.blocksRaycasts = false;
            }

            if (instant || window._animator == null)
            {
                if (window._animator != null)
                {
                    window._animator.Play(HiddenState, 0, 1f);
                    window._animator.Update(0f);
                }

                window.ApplyHiddenState();
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            }

            window._animator.ResetTrigger(ShowTrigger);
            window._animator.SetTrigger(HideTrigger);

            string targetState = window.GetHiddenStateName();
            IDisposable waitSubscription = null;
            waitSubscription = window.WaitForState(targetState).Subscribe(
                _ => { },
                observer.OnError,
                () =>
                {
                    if (ReferenceEquals(window._transitionSubscription, waitSubscription))
                    {
                        window._transitionSubscription = null;
                    }

                    window.ApplyHiddenState();
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });

            window._transitionSubscription = waitSubscription;
            return Disposable.Create(() =>
            {
                waitSubscription.Dispose();
                if (ReferenceEquals(window._transitionSubscription, waitSubscription))
                {
                    window._transitionSubscription = null;
                }
            });
        });
    }

    IObservable<Unit> WaitForState(string stateName)
    {
        return Observable.Create<Unit>(observer =>
        {
            BaseWindow window = this;
            if (window._animator == null)
            {
                Debug.LogError("_animator is null in BaseWindow.WaitForState");
                observer.OnCompleted();
                return Disposable.Empty;
            }

            if (!window.HasState(stateName))
            {
                Debug.LogError($"State '{stateName}' is not available on animator in BaseWindow.WaitForState");
                observer.OnCompleted();
                return Disposable.Empty;
            }

            bool TryComplete()
            {
                AnimatorStateInfo info = window._animator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(stateName))
                {
                    return false;
                }

                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return true;
            }

            if (TryComplete())
            {
                return Disposable.Empty;
            }

            IDisposable subscription = null;
            subscription = window.gameObject.UpdateAsObservable().Subscribe(_ =>
            {
                if (TryComplete())
                {
                    subscription.Dispose();
                }
            });
            return subscription;
        });
    }

    void StopTransition()
    {
        _transitionSubscription?.Dispose();
        _transitionSubscription = null;
    }

    string GetShownStateName()
    {
        if (HasState(ShowingState))
        {
            return ShowingState;
        }

        return ShowState;
    }

    string GetHiddenStateName()
    {
        if (HasState(HiddenState))
        {
            return HiddenState;
        }

        return HideState;
    }

    bool HasState(string stateName)
    {
        if (_animator == null)
        {
            Debug.LogError("_animator is null in BaseWindow.HasState");
            return false;
        }

        if (_animator.HasState(0, Animator.StringToHash(stateName)))
        {
            return true;
        }

        string layerStateName = $"{_animator.GetLayerName(0)}.{stateName}";
        return _animator.HasState(0, Animator.StringToHash(layerStateName));
    }

    void ApplyShownState()
    {
        IsVisible = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        SelectDefault();
        
        if (soundService != null)
        {
            soundService.PlayUiSoundEffect(_openSound);
        }
    }

    void ApplyHiddenState()
    {
        IsVisible = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (soundService != null)
        {
            soundService.PlayUiSoundEffect(_closeSound);
        }

        if (DisableGameObjectWhenHidden)
        {
            gameObject.SetActive(false);
        }
    }

    public void SelectDefault()
    {
        if (_selectOnShow != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_selectOnShow);
        }
        else if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current is null in BaseWindow.SelectDefault");
        }
    }

    protected virtual void OnShow(object payload)
    {
    }

    protected virtual void OnHide()
    {
    }
}
