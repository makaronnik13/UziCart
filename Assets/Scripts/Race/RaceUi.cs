using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public class RaceUi : MonoBehaviour
{
    [SerializeField] TMP_Text _lapText;
    [SerializeField] TMP_Text _speedText;
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Button _pauseButton;
    [SerializeField] InputActionReference _pauseAction;

    [Inject(Optional = true)] RaceController _raceController;
    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] GlobalSettings _settings;

    readonly CompositeDisposable _disposables = new CompositeDisposable();
    readonly SerialDisposable _speedSubscription = new SerialDisposable();

    void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_pauseButton == null)
        {
            _pauseButton = FindPauseButton();
        }

        _pauseButton?.onClick.AddListener(OpenPauseScreen);

        if (_pauseAction != null && _pauseAction.action != null)
        {
            _pauseAction.action.performed += OnPauseActionPerformed;
            _pauseAction.action.Enable();
        }
    }

    void Start()
    {
        if (_raceController == null)
        {
            _raceController = FindFirstObjectByType<RaceController>();
        }

        if (_raceController == null)
        {
            Debug.LogError($"{nameof(RaceUi)} has no {nameof(RaceController)}.", this);
            return;
        }

        _raceController.PlayerCurrentLap
            .Subscribe(UpdateLap)
            .AddTo(_disposables);

        _raceController.PlayerSpawned
            .Subscribe(SubscribeToPlayerSpeed)
            .AddTo(_disposables);

        if (_raceController.PlayerParticipant != null)
        {
            SubscribeToPlayerSpeed(_raceController.PlayerParticipant);
        }

        UpdateLap(_raceController.PlayerCurrentLap.Value);
    }

    public void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    void OnDestroy()
    {
        _pauseButton?.onClick.RemoveListener(OpenPauseScreen);
        if (_pauseAction != null && _pauseAction.action != null)
        {
            _pauseAction.action.performed -= OnPauseActionPerformed;
            _pauseAction.action.Disable();
        }

        _speedSubscription.Dispose();
        _disposables.Dispose();
    }

    void UpdateLap(int lap)
    {
        if (_lapText != null && _raceController != null)
        {
            _lapText.text = $"Lap {lap}/{_raceController.LapsCount}";
        }
    }

    void SubscribeToPlayerSpeed(RaceParticipant participant)
    {
        _speedSubscription.Disposable = participant.Controller.SpeedKmh
            .Subscribe(speed =>
            {
                if (_speedText != null)
                {
                    _speedText.text = $"{Mathf.RoundToInt(speed)} km/h";
                }
            });
    }

    void OpenPauseScreen()
    {
        if (_canvasGroup != null && (!_canvasGroup.interactable || _canvasGroup.alpha <= 0f))
        {
            return;
        }

        WindowId pauseWindowId = _settings?.windowsConfig?.pauseWindowId;
        if (_windowsService != null && pauseWindowId != null)
        {
            _windowsService.Open(pauseWindowId, keepPrevious: true);
            return;
        }

        Debug.LogError($"{nameof(RaceUi)} cannot open pause screen.", this);
    }

    void OnPauseActionPerformed(InputAction.CallbackContext _)
    {
        if (IsPauseWindowVisible())
        {
            return;
        }

        Observable.NextFrame()
            .Subscribe(__ =>
            {
                if (!IsPauseWindowVisible())
                {
                    OpenPauseScreen();
                }
            })
            .AddTo(_disposables);
    }

    bool IsPauseWindowVisible()
    {
        WindowId pauseWindowId = _settings?.windowsConfig?.pauseWindowId;
        return _windowsService != null &&
               pauseWindowId != null &&
               _windowsService.IsWindowVisible(pauseWindowId);
    }

    Button FindPauseButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name;
            if (buttonName.Contains("Pause") || buttonName.Contains("Пауза"))
            {
                return buttons[i];
            }
        }

        return null;
    }
}
