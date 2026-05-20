using System;
using UniRx;
using UnityEngine;

public interface IPauseService
{
    ReactiveProperty<bool> IsPaused { get; }
    ReactiveCommand<bool> OnPauseChanged { get; }
    void Pause();
    void Resume();
}

[Serializable]
public class PauseService : IPauseService, IRuntimeResettable
{
    public ReactiveProperty<bool> IsPaused { get; } = new ReactiveProperty<bool>(false);
    public ReactiveCommand<bool> OnPauseChanged { get; } = new ReactiveCommand<bool>();
    int _pauseRequests;

    public void Pause()
    {
        _pauseRequests++;
        SetPaused(_pauseRequests > 0);
    }

    public void Resume()
    {
        if (_pauseRequests > 0)
        {
            _pauseRequests--;
        }
        else
        {
            Debug.LogWarning("PauseService.Resume called with no active pause requests");
        }

        SetPaused(_pauseRequests > 0);
    }

    public void ResetRuntimeState()
    {
        _pauseRequests = 0;

        if (!IsPaused.Value)
        {
            return;
        }

        IsPaused.Value = false;
        OnPauseChanged.Execute(false);
    }

    void SetPaused(bool paused)
    {
        if (IsPaused.Value == paused)
        {
            return;
        }

        // Apply the fallback timeScale first so services reacting to IsPaused
        // can override it with their own restored speed in the same frame.
        Time.timeScale = paused ? 0f : 1f;
        IsPaused.Value = paused;
        OnPauseChanged.Execute(paused);
    }
}
