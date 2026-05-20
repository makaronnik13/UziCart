using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BackButton : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] bool _instant;

    [Inject(Optional = true)] IWindowsService _windowsService;

    void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }
    }

    void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
        else
        {
            Debug.LogError("_button is null in BackButton.OnEnable", this);
        }
    }

    void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    void HandleClick()
    {
        if (_windowsService == null)
        {
            Debug.LogError("_windowsService is null in BackButton.HandleClick", this);
            return;
        }

        _windowsService.Back(_instant);
    }
}
