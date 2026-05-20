using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public class OpenWindowButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private InputActionReference _openAction;
    [SerializeField] private WindowId _windowId;
    [SerializeField] private bool _openAsPopup = true;
    [SerializeField] private bool _keepPrevious;
    [SerializeField] private bool _instant;
    [SerializeField] private bool _toggle = true;

    [Inject(Optional = true)] private IWindowsService _windowsService;
    [Inject(Optional = true)] private GlobalSettings _settings;

    protected virtual void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        ApplyVisibility();
    }

    protected virtual void OnEnable()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (_button != null)
        {
            _button.onClick.AddListener(ToggleWindow);
        }

        if (_openAction != null && _openAction.action != null)
        {
            _openAction.action.performed += OnOpenActionPerformed;
            _openAction.action.Enable();
        }
    }

    protected virtual void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(ToggleWindow);
        }

        if (_openAction != null && _openAction.action != null)
        {
            _openAction.action.performed -= OnOpenActionPerformed;
            _openAction.action.Disable();
        }
    }

    void OnOpenActionPerformed(InputAction.CallbackContext _)
    {
        if (!gameObject.activeInHierarchy ||
            (_button != null && !_button.gameObject.activeInHierarchy))
        {
            return;
        }

        ToggleWindow();
    }

    protected virtual void ApplyVisibility()
    {
        if (_settings == null)
        {
            Debug.LogError("_settings is null in OpenWindowButton.ApplyVisibility");
            return;
        }

        bool visible = _settings.DebugTools;
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }

    public virtual void ToggleWindow()
    {
        if (_windowsService == null)
        {
            Debug.LogError("_windowsService is null in OpenWindowButton.ToggleWindow");
            return;
        }

        if (_windowId == null)
        {
            Debug.LogError("_windowId is null in OpenWindowButton.ToggleWindow");
            return;
        }

        bool isOpened = _windowsService.IsWindowVisible(_windowId);
        if (_toggle && isOpened)
        {
            _windowsService.Close(_windowId);
            return;
        }

        if (_openAsPopup)
        {
            _windowsService.OpenPopup(_windowId, null, _instant);
            return;
        }

        _windowsService.Open(_windowId, _keepPrevious, null, _instant);
    }
}
