using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

public class TabBehaviour : MonoBehaviour
{
    [System.Serializable]
    class TabData
    {
        public Button button;
        public WindowId windowId;
    }

    [SerializeField] private List<TabData> _tabs = new List<TabData>();
    readonly List<UnityAction> _buttonHandlers = new List<UnityAction>();
    readonly HashSet<WindowId> _internalHidden = new HashSet<WindowId>();
    readonly CompositeDisposable _disposables = new CompositeDisposable();

    [SerializeField] private int _defaultTabIndex = 0;
    [SerializeField] private bool _closeParentOnTabBack = true;
    [SerializeField] private bool _instantTabSwitch;

    [Inject] private IWindowsService _windowsService;

    BaseWindow _ownerWindow;
    WindowId _activeWindowId;
    int _activeTabIndex = -1;

    void OnEnable()
    {
        _ownerWindow = GetComponent<BaseWindow>();
        _internalHidden.Clear();
        RebindButtons();
        SubscribeWindowEvents();
    }

    void OnDisable()
    {
        UnbindButtons();
        _disposables.Clear();
    }

    public void ActivateDefaultTab()
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        SelectTab(_defaultTabIndex);
    }

    public void ActivateTab(WindowId windowId)
    {
        if (windowId == null)
        {
            ActivateDefaultTab();
            return;
        }

        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i] != null && _tabs[i].windowId == windowId)
            {
                SelectTab(i);
                return;
            }
        }

        ActivateDefaultTab();
    }

    public void CloseAllTabs()
    {
        if (_activeWindowId != null)
        {
            _internalHidden.Add(_activeWindowId);
            _windowsService?.Close(_activeWindowId, _instantTabSwitch);
        }

        _activeWindowId = null;
        _activeTabIndex = -1;
        UpdateButtonsState();
    }

    void RebindButtons()
    {
        UnbindButtons();
        _buttonHandlers.Clear();

        for (int i = 0; i < _tabs.Count; i++)
        {
            int tabIndex = i;
            UnityAction handler = () => SelectTab(tabIndex);
            _buttonHandlers.Add(handler);
            if (_tabs[i] != null && _tabs[i].button != null)
            {
                _tabs[i].button.onClick.AddListener(handler);
            }
        }

        UpdateButtonsState();
    }

    void UnbindButtons()
    {
        int count = Mathf.Min(_tabs.Count, _buttonHandlers.Count);
        for (int i = 0; i < count; i++)
        {
            if (_tabs[i] != null && _tabs[i].button != null)
            {
                _tabs[i].button.onClick.RemoveListener(_buttonHandlers[i]);
            }
        }

        _buttonHandlers.Clear();
    }

    void SubscribeWindowEvents()
    {
        _disposables.Clear();
        if (_windowsService == null)
        {
            return;
        }

        _windowsService.OnPopupHidden
            .Subscribe(HandlePopupHidden)
            .AddTo(_disposables);
    }

    void SelectTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= _tabs.Count || _windowsService == null)
        {
            return;
        }

        TabData selected = _tabs[tabIndex];
        if (selected == null || selected.windowId == null)
        {
            return;
        }

        if (_activeWindowId == selected.windowId)
        {
            _activeTabIndex = tabIndex;
            UpdateButtonsState();
            return;
        }

        WindowId previousWindowId = _activeWindowId;
        if (previousWindowId != null)
        {
            _internalHidden.Add(previousWindowId);
        }

        _activeTabIndex = tabIndex;
        _activeWindowId = selected.windowId;
        UpdateButtonsState();

        if (previousWindowId != null)
        {
            _windowsService.SwitchPopup(previousWindowId, selected.windowId, null, _instantTabSwitch);
            return;
        }

        _windowsService.OpenPopup(selected.windowId, null, _instantTabSwitch);
    }

    void HandlePopupHidden(BaseWindow hiddenPopup)
    {
        if (hiddenPopup == null || hiddenPopup.WindowId == null)
        {
            return;
        }

        WindowId hiddenId = hiddenPopup.WindowId;
        if (_internalHidden.Contains(hiddenId))
        {
            _internalHidden.Remove(hiddenId);
            if (_activeWindowId == hiddenId)
            {
                _activeWindowId = null;
                _activeTabIndex = -1;
                UpdateButtonsState();
            }
            return;
        }

        if (!IsTabWindow(hiddenId))
        {
            return;
        }

        _activeWindowId = null;
        _activeTabIndex = -1;
        UpdateButtonsState();

        if (_closeParentOnTabBack && _windowsService != null)
        {
            WindowId parentWindowId = _ownerWindow != null ? _ownerWindow.WindowId : null;
            if (parentWindowId != null)
            {
                _windowsService.Close(parentWindowId, _instantTabSwitch);
            }
            else
            {
                _windowsService.Back(_instantTabSwitch);
            }
        }
    }

    bool IsTabWindow(WindowId id)
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i] != null && _tabs[i].windowId == id)
            {
                return true;
            }
        }

        return false;
    }

    void UpdateButtonsState()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i] == null || _tabs[i].button == null)
            {
                continue;
            }

            _tabs[i].button.interactable = i != _activeTabIndex;
        }
    }
}
