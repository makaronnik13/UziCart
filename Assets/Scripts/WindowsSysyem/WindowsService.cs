using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

public interface IWindowsService
{
    ReactiveCommand<BaseWindow> OnWindowShown { get; }
    ReactiveCommand<BaseWindow> OnWindowHidden { get; }
    ReactiveCommand<BaseWindow> OnPopupShown { get; }
    ReactiveCommand<BaseWindow> OnPopupHidden { get; }
    bool HasBlockingWindowOpen { get; }
    bool IsWindowVisible(WindowId id);

    void Open(WindowId id, bool keepPrevious = false, object payload = null, bool instant = false);
    void OpenPopup(WindowId id, object payload = null, bool instant = false);
    void SwitchPopup(WindowId fromId, WindowId toId, object payload = null, bool instant = false);
    void Close(WindowId id, bool instant = false);
    void Back(bool instant = false, bool fromInput = false);
}

public class WindowsService : IWindowsService, IInitializable, IDisposable, IRuntimeResettable
{
    readonly Dictionary<WindowId, BaseWindow> cache = new Dictionary<WindowId, BaseWindow>();
    readonly Stack<BaseWindow> mainStack = new Stack<BaseWindow>();
    readonly Stack<BaseWindow> popupStack = new Stack<BaseWindow>();
    readonly HashSet<BaseWindow> shownWindows = new HashSet<BaseWindow>();
    readonly Subject<Func<IObservable<Unit>>> queue = new Subject<Func<IObservable<Unit>>>();
    readonly CompositeDisposable disposables = new CompositeDisposable();

    readonly GlobalSettings _settings;
    readonly DiContainer _container;
    InputAction _activeBackAction;
    int _requestGeneration;
    
    public ReactiveCommand<BaseWindow> OnWindowShown { get; } = new ReactiveCommand<BaseWindow>();
    public ReactiveCommand<BaseWindow> OnWindowHidden { get; } = new ReactiveCommand<BaseWindow>();
    public ReactiveCommand<BaseWindow> OnPopupShown { get; } = new ReactiveCommand<BaseWindow>();
    public ReactiveCommand<BaseWindow> OnPopupHidden { get; } = new ReactiveCommand<BaseWindow>();
    public bool HasBlockingWindowOpen => HasBlockingManagedWindowOpen();
    public bool IsWindowVisible(WindowId id) => IsWindowVisibleInternal(id);

    public WindowsService(GlobalSettings settings, DiContainer container)
    {
        this._settings = settings;
        _container = container;
    }

    public void Initialize()
    {
        queue.Select(request => request())
            .Concat()
            .Subscribe()
            .AddTo(disposables);
        RebuildCache();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        _activeBackAction = ResolveBackAction();
        if (_activeBackAction != null)
        {
            _activeBackAction.performed += OnBackPerformed;
            _activeBackAction.Enable();
        }

        OpenInitialMenuWindow();
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        if (_activeBackAction != null)
        {
            _activeBackAction.performed -= OnBackPerformed;
            _activeBackAction.Disable();
            _activeBackAction = null;
        }
        disposables.Dispose();
    }

    public void Open(WindowId id, bool keepPrevious = false, object payload = null, bool instant = false)
    {
        EnqueueRequest(generation => OpenRoutine(generation, id, keepPrevious, payload, instant));
    }

    public void OpenPopup(WindowId id, object payload = null, bool instant = false)
    {
        EnqueueRequest(generation => OpenPopupRoutine(generation, id, payload, instant));
    }

    public void SwitchPopup(WindowId fromId, WindowId toId, object payload = null, bool instant = false)
    {
        EnqueueRequest(generation => SwitchPopupRoutine(generation, fromId, toId, payload, instant));
    }

    public void Close(WindowId id, bool instant = false)
    {
        EnqueueRequest(generation => CloseRoutine(generation, id, instant));
    }

    public void Back(bool instant = false, bool fromInput = false)
    {
        EnqueueRequest(generation => BackRoutine(generation, instant, fromInput));
    }

    public void ResetRuntimeState()
    {
        AdvanceRequestGeneration();
        HideAllManagedWindowsInstant();
        shownWindows.Clear();
        mainStack.Clear();
        popupStack.Clear();
        cache.Clear();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AdvanceRequestGeneration();
        shownWindows.Clear();
        RebuildCache();
        ResetStacksIfGameplayScene(scene.name);
    }

    void OnSceneUnloaded(Scene scene)
    {
        AdvanceRequestGeneration();
        shownWindows.Clear();
        RebuildCache();
    }

    void EnqueueRequest(Func<int, IObservable<Unit>> routineFactory)
    {
        int generation = _requestGeneration;
        queue.OnNext(() => RunRequest(generation, routineFactory));
    }

    IObservable<Unit> RunRequest(int generation, Func<int, IObservable<Unit>> routineFactory)
    {
        if (!IsRequestCurrent(generation) || routineFactory == null)
        {
            return Observable.ReturnUnit();
        }

        return routineFactory(generation) ?? Observable.ReturnUnit();
    }

    void AdvanceRequestGeneration()
    {
        _requestGeneration++;
    }

    bool IsRequestCurrent(int generation)
    {
        return generation == _requestGeneration;
    }

    void RebuildCache()
    {
        cache.Clear();
        BaseWindow[] windows = UnityEngine.Object.FindObjectsByType<BaseWindow>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < windows.Length; i++)
        {
            BaseWindow window = windows[i];
            if (window == null || window.WindowId == null)
            {
                Debug.LogError("Found null window or window.WindowId in WindowsService.RebuildCache");
                continue;
            }

            if (!cache.ContainsKey(window.WindowId))
            {
                cache.Add(window.WindowId, window);
            }
            else
            {
                Debug.LogWarning($"Duplicate window id found: {window.WindowId.name}", window);
            }
        }

        CleanStacks();
        EnsureRootWindow();
    }

    void HideAllManagedWindowsInstant()
    {
        HashSet<BaseWindow> windowsToHide = new HashSet<BaseWindow>();

        foreach (BaseWindow window in cache.Values)
        {
            if (window != null)
            {
                windowsToHide.Add(window);
            }
        }

        foreach (BaseWindow window in shownWindows)
        {
            if (window != null)
            {
                windowsToHide.Add(window);
            }
        }

        BaseWindow[] sceneWindows = UnityEngine.Object.FindObjectsByType<BaseWindow>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneWindows.Length; i++)
        {
            if (sceneWindows[i] != null)
            {
                windowsToHide.Add(sceneWindows[i]);
            }
        }

        foreach (BaseWindow window in windowsToHide)
        {
            if (window == null)
            {
                continue;
            }

            try
            {
                window.HideInstant();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, window);
            }
        }
    }

    void CleanStacks()
    {
        RemoveMissing(mainStack);
        RemoveMissing(popupStack);
    }

    void EnsureRootWindow()
    {
        if (mainStack.Count > 0)
        {
            return;
        }

        foreach (BaseWindow window in cache.Values)
        {
            if (window != null && window.PreventHide && window.gameObject.activeInHierarchy)
            {
                mainStack.Push(window);
                return;
            }
        }
    }

    void OpenInitialMenuWindow()
    {
        if (_settings == null ||
            _settings.windowsConfig == null ||
            _settings.windowsConfig.menuWindowId == null)
        {
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(_settings.menuSceneName) && activeSceneName != _settings.menuSceneName)
        {
            return;
        }

        if (mainStack.Count > 0)
        {
            return;
        }

        Open(_settings.windowsConfig.menuWindowId, false, null, true);
    }

    void ResetStacksIfGameplayScene(string sceneName)
    {
        if (_settings == null || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("_settings is null or sceneName is empty in WindowsService.ResetStacksIfGameplayScene");
            return;
        }

        /*
        if (sceneName != _settings.gameplaySceneName)
        {
            return;
        }*/

        mainStack.Clear();
        popupStack.Clear();
    }

    void RemoveMissing(Stack<BaseWindow> stack)
    {
        if (stack.Count == 0)
        {
            return;
        }

        List<BaseWindow> list = new List<BaseWindow>(stack);
        list.Reverse();
        stack.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            BaseWindow window = list[i];
            if (window != null && cache.ContainsValue(window))
            {
                stack.Push(window);
            }
        }
    }

    BaseWindow GetWindow(WindowId id)
    {
        if (id == null)
        {
            Debug.LogError("WindowId is null in WindowsService.GetWindow.");
            return null;
        }

        if (!cache.TryGetValue(id, out BaseWindow window) || window == null)
        {
            window = InstantiateWindowFromConfig(id);
            if (window == null)
            {
                Debug.LogError($"Window not found in scene: {id?.name}");
                return null;
            }
        }

        return window;
    }

    BaseWindow InstantiateWindowFromConfig(WindowId id)
    {
        GameObject prefab = id != null ? id.Prefab : null;
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = _container != null
            ? _container.InstantiatePrefab(prefab)
            : UnityEngine.Object.Instantiate(prefab);
        instance.name = prefab.name;

        BaseWindow window = instance.GetComponent<BaseWindow>();
        if (window == null)
        {
            Debug.LogError($"Prefab '{prefab.name}' for window '{id.name}' does not contain BaseWindow.");
            UnityEngine.Object.Destroy(instance);
            return null;
        }

        cache[id] = window;
        return window;
    }

    IObservable<Unit> OpenRoutine(int generation, WindowId id, bool keepPrevious, object payload, bool instant)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        CleanStacks();
        EnsureRootWindow();
        BaseWindow next = GetWindow(id);
        if (next == null)
        {
            Debug.LogError($"Requested window id '{id?.name}' not found in WindowsService.OpenRoutine");
            return Observable.ReturnUnit();
        }

        BaseWindow current = mainStack.Count > 0 ? mainStack.Peek() : null;
        IObservable<Unit> sequence = Observable.ReturnUnit();
        if (current != null && current != next)
        {
            if (!current.PreventHide)
            {
                sequence = Then(sequence, () => IfCurrent(generation, () => current.HideRoutine(instant)));
                sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
                {
                    MarkWindowHidden(current);
                    OnWindowHidden.Execute(current);

                    if (!keepPrevious)
                    {
                        mainStack.Pop();
                    }
                })));
            }
        }

        if (next.AddToWindowStack)
        {
            RemoveFromStack(mainStack, next);
            mainStack.Push(next);
        }

        sequence = Then(sequence, () => IfCurrent(generation, () => next.ShowRoutine(payload, instant)));
        sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
        {
            MarkWindowShown(next);
            OnWindowShown.Execute(next);
        })));
        return sequence;
    }

    bool IsWindowVisibleInternal(WindowId id)
    {
        if (id == null)
        {
            return false;
        }

        if (cache.TryGetValue(id, out BaseWindow cachedWindow) && cachedWindow != null && cachedWindow.IsVisible)
        {
            return true;
        }

        foreach (BaseWindow window in shownWindows)
        {
            if (window != null && window.WindowId == id && window.IsVisible)
            {
                return true;
            }
        }

        return false;
    }

    IObservable<Unit> OpenPopupRoutine(int generation, WindowId id, object payload, bool instant)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        CleanStacks();
        EnsureRootWindow();
        BaseWindow popup = GetWindow(id);
        if (popup == null)
        {
            Debug.LogError($"Requested popup id '{id?.name}' not found in WindowsService.OpenPopupRoutine");
            return Observable.ReturnUnit();
        }

        if (popup.AddToWindowStack)
        {
            RemoveFromStack(popupStack, popup);
            popupStack.Push(popup);
        }

        IObservable<Unit> sequence = Observable.ReturnUnit();
        sequence = Then(sequence, () => IfCurrent(generation, () => popup.ShowRoutine(payload, instant)));
        sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
        {
            MarkWindowShown(popup);
            OnPopupShown.Execute(popup);
        })));
        return sequence;
    }

    IObservable<Unit> CloseRoutine(int generation, WindowId id, bool instant)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        CleanStacks();
        EnsureRootWindow();
        BaseWindow target = GetWindow(id);
        if (target == null)
        {
            return Observable.ReturnUnit();
        }

        bool wasPopup = RemoveFromStack(popupStack, target);
        bool wasMain = RemoveFromStack(mainStack, target);

        if (!target.IsVisible && !wasPopup && !wasMain)
        {
            return Observable.ReturnUnit();
        }

        IObservable<Unit> sequence = Observable.ReturnUnit();
        sequence = Then(sequence, () => IfCurrent(generation, () => target.HideRoutine(instant)));
        sequence = Then(sequence, () => IfCurrent(generation, () => Run(() => MarkWindowHidden(target))));

        if (wasPopup)
        {
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() => OnPopupHidden.Execute(target))));
        }
        else if (wasMain)
        {
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() => OnWindowHidden.Execute(target))));
            if (mainStack.Count > 0)
            {
                BaseWindow previous = mainStack.Peek();
                sequence = Then(sequence, () => IfCurrent(generation, () => previous.ShowRoutine(null, instant)));
                sequence = Then(sequence, () => IfCurrent(generation, () => Run(() => OnWindowShown.Execute(previous))));
            }
        }
        else
        {
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() => OnPopupHidden.Execute(target))));
        }

        return sequence;
    }

    IObservable<Unit> SwitchPopupRoutine(int generation, WindowId fromId, WindowId toId, object payload, bool instant)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        CleanStacks();
        EnsureRootWindow();

        BaseWindow next = GetWindow(toId);
        if (next == null)
        {
            Debug.LogError($"Requested popup id '{toId?.name}' not found in WindowsService.SwitchPopupRoutine");
            return Observable.ReturnUnit();
        }

        BaseWindow current = fromId != null ? GetWindow(fromId) : null;
        IObservable<Unit> sequence = Observable.ReturnUnit();
        if (current == next)
        {
            if (next.AddToWindowStack)
            {
                RemoveFromStack(popupStack, next);
                popupStack.Push(next);
            }

            sequence = Then(sequence, () => IfCurrent(generation, () => next.ShowRoutine(payload, instant)));
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
            {
                MarkWindowShown(next);
                OnPopupShown.Execute(next);
            })));
            return sequence;
        }

        if (current != null)
        {
            RemoveFromStack(popupStack, current);
        }

        if (next.AddToWindowStack)
        {
            RemoveFromStack(popupStack, next);
            popupStack.Push(next);
        }

        sequence = Then(sequence, () => IfCurrent(generation, () => next.ShowRoutine(payload, instant)));
        sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
        {
            MarkWindowShown(next);
            OnPopupShown.Execute(next);
        })));

        if (current != null && current.IsVisible)
        {
            sequence = Then(sequence, () => IfCurrent(generation, () => current.HideRoutine(instant)));
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
            {
                MarkWindowHidden(current);
                OnPopupHidden.Execute(current);
            })));
        }

        return sequence;
    }

    IObservable<Unit> BackRoutine(int generation, bool instant, bool fromInput)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        CleanStacks();
        EnsureRootWindow();
        if (popupStack.Count > 0)
        {
            BaseWindow popup = popupStack.Peek();
            if (fromInput && !popup.CloseByBack)
            {
                return Observable.ReturnUnit();
            }

            popupStack.Pop();
            IObservable<Unit> popupSequence = Observable.ReturnUnit();
            popupSequence = Then(popupSequence, () => IfCurrent(generation, () => popup.HideRoutine(instant)));
            popupSequence = Then(popupSequence, () => IfCurrent(generation, () => Run(() =>
            {
                MarkWindowHidden(popup);
                OnPopupHidden.Execute(popup);
            })));
            if (mainStack.Count > 0)
            {
                popupSequence = Then(popupSequence, () => IfCurrent(generation, () => Observable.NextFrame()));
                popupSequence = Then(popupSequence, () => IfCurrent(generation, () => Run(() => mainStack.Peek().SelectDefault())));
            }
            return popupSequence;
        }

        if (mainStack.Count == 0)
        {
            /*
            if (IsGameplayScene() && _settings != null)
            {
                return OpenRoutine(generation, _settings.inGameMenuWindowId, false, null, instant);
            }
            */
            return Observable.ReturnUnit();
        }

        BaseWindow current = mainStack.Peek();
        if (fromInput && !current.CloseByBack)
        {
            return Observable.ReturnUnit();
        }

        mainStack.Pop();
        if (!current.PreventHide)
        {
            IObservable<Unit> sequence = Observable.ReturnUnit();
            sequence = Then(sequence, () => IfCurrent(generation, () => current.HideRoutine(instant)));
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
            {
                MarkWindowHidden(current);
                OnWindowHidden.Execute(current);
            })));

            if (mainStack.Count == 0)
            {
                return sequence;
            }

            BaseWindow previous = mainStack.Peek();
            sequence = Then(sequence, () => IfCurrent(generation, () => previous.ShowRoutine(null, instant)));
            sequence = Then(sequence, () => IfCurrent(generation, () => Run(() =>
            {
                MarkWindowShown(previous);
                OnWindowShown.Execute(previous);
            })));
            return sequence;
        }
        else
        {
            mainStack.Push(current);
            return Observable.ReturnUnit();
        }
    }

    bool RemoveFromStack(Stack<BaseWindow> stack, BaseWindow window)
    {
        if (stack.Count == 0 || window == null)
        {
            return false;
        }

        List<BaseWindow> list = new List<BaseWindow>(stack);
        bool removed = list.Remove(window);
        list.Reverse();
        stack.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            stack.Push(list[i]);
        }

        return removed;
    }

    void MarkWindowShown(BaseWindow window)
    {
        if (window == null)
        {
            return;
        }

        Transform transform = window.transform;
        if (transform != null)
        {
            transform.SetAsLastSibling();
        }

        shownWindows.Add(window);
    }

    void MarkWindowHidden(BaseWindow window)
    {
        if (window == null)
        {
            return;
        }

        shownWindows.Remove(window);
    }

    bool HasBlockingManagedWindowOpen()
    {
        foreach (BaseWindow window in shownWindows)
        {
            if (window == null ||
                window.PreventHide ||
                !window.BlocksCameraInput ||
                !window.IsVisible)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    void OnBackPerformed(InputAction.CallbackContext context)
    {
        Back(fromInput: true);
    }

    InputAction ResolveBackAction()
    {
        InputActionsConfig inputConfig = _settings != null ? _settings.inputActionsConfig : null;
        if (inputConfig != null && inputConfig.BackAction != null &&
            inputConfig.BackAction != null)
        {
            return inputConfig.BackAction;
        }

        return null;
    }


    bool IsGameplayScene()
    {
        if (_settings == null)
        {
            Debug.LogError("_settings is null in WindowsService.IsGameplayScene");
            return false;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName != _settings.menuSceneName;
    }

    IObservable<Unit> IfCurrent(int generation, Func<IObservable<Unit>> routineFactory)
    {
        if (!IsRequestCurrent(generation))
        {
            return Observable.ReturnUnit();
        }

        return routineFactory != null ? routineFactory() ?? Observable.ReturnUnit() : Observable.ReturnUnit();
    }

    static IObservable<Unit> Run(Action action)
    {
        return Observable.Create<Unit>(observer =>
        {
            action?.Invoke();
            observer.OnNext(Unit.Default);
            observer.OnCompleted();
            return Disposable.Empty;
        });
    }

    static IObservable<Unit> Then(IObservable<Unit> first, Func<IObservable<Unit>> nextFactory)
    {
        return Observable.Create<Unit>(observer =>
        {
            CompositeDisposable subscriptions = new CompositeDisposable();
            IDisposable secondSubscription = null;
            IDisposable firstSubscription = first.Subscribe(
                _ => { },
                observer.OnError,
                () =>
                {
                    IObservable<Unit> next = nextFactory != null ? nextFactory() : null;
                    if (next == null)
                    {
                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();
                        return;
                    }

                    secondSubscription = next.Subscribe(
                        __ => { },
                        observer.OnError,
                        () =>
                        {
                            observer.OnNext(Unit.Default);
                            observer.OnCompleted();
                        });
                    subscriptions.Add(secondSubscription);
                });

            subscriptions.Add(firstSubscription);
            return subscriptions;
        });
    }
}
