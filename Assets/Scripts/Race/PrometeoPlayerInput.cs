using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PrometeoPlayerInput : MonoBehaviour
{
    [SerializeField] RaceControlUI _controlUI;
    [SerializeField] InputActionReference _leftAction;
    [SerializeField] InputActionReference _rightAction;
    [SerializeField] InputActionReference _moveForwardAction;
    [SerializeField] InputActionReference _stopAction;
    [SerializeField] bool _debugInput;

    [Inject(Optional = true)] RaceController _raceController;

    readonly CompositeDisposable _disposables = new CompositeDisposable();
    readonly SerialDisposable _controlUiSubscription = new SerialDisposable();
    PrometeoCarController _controller;
    RaceControlCommand? _lastSteeringCommand;
    RaceControlCommand? _lastMoveCommand;
    bool _previousLeft;
    bool _previousRight;
    bool _previousForward;
    bool _previousStop;
    float _previousSteering;
    bool _loggedMissingController;

    void Awake()
    {
        if (_controlUI == null)
        {
            _controlUI = FindFirstObjectByType<RaceControlUI>(FindObjectsInactive.Include);
        }
    }

    public void Initialize(PrometeoCarController controller, RaceControlUI controlUI)
    {
        if (controller == null)
        {
            Debug.LogError($"{nameof(PrometeoPlayerInput)} received empty controller.", this);
            return;
        }

        _controller = controller;
        LogInput($"Bound controller '{controller.name}'. controlEnabled={controller.ControlEnabled}");
        if (controlUI != null)
        {
            _controlUI = controlUI;
            SubscribeControlUI();
        }
    }

    void Start()
    {
        if (_raceController == null)
        {
            _raceController = FindFirstObjectByType<RaceController>();
        }

        if (_raceController != null)
        {
            _raceController.PlayerSpawned.Subscribe(BindPlayer).AddTo(_disposables);
            if (_raceController.PlayerParticipant != null)
            {
                BindPlayer(_raceController.PlayerParticipant);
            }
        }

        if (_controlUI != null)
        {
            SubscribeControlUI();
        }
    }

    void OnEnable()
    {
        Enable(_leftAction);
        Enable(_rightAction);
        Enable(_moveForwardAction);
        Enable(_stopAction);
    }

    void OnDisable()
    {
        Disable(_leftAction);
        Disable(_rightAction);
        Disable(_moveForwardAction);
        Disable(_stopAction);
        if (_controller != null)
        {
            _controller.ResetControlInput();
        }
    }

    void OnDestroy()
    {
        _controlUiSubscription.Dispose();
        _disposables.Dispose();
    }

    void Update()
    {
        if (_controller == null)
        {
            if (!_loggedMissingController)
            {
                LogInput("Update skipped: controller is null.");
                _loggedMissingController = true;
            }
            return;
        }

        _loggedMissingController = false;
        UpdateLastPressedCommands();
        float steering = GetSteering();
        bool forward = IsMoveForwardPressed();
        bool stop = IsStopPressed();

        LogInputState(steering, forward, stop);

        _controller.SetSteering(steering);
        _controller.SetMoveForward(forward);
        _controller.SetStop(stop);
    }

    void HandleUiPressed(RaceControlCommand command)
    {
        if (command == RaceControlCommand.Left || command == RaceControlCommand.Right)
        {
            _lastSteeringCommand = command;
        }
        else
        {
            _lastMoveCommand = command;
        }

        LogInput($"UI pressed {command}. lastSteering={_lastSteeringCommand}, lastMove={_lastMoveCommand}");
    }

    void BindPlayer(RaceParticipant participant)
    {
        if (participant == null || participant.Controller == null)
        {
            Debug.LogError($"{nameof(PrometeoPlayerInput)} cannot bind empty player participant.", this);
            return;
        }

        _controller = participant.Controller;
        LogInput($"Bound player from event. controller='{_controller.name}', participant='{participant.DisplayName}', controlEnabled={_controller.ControlEnabled}");
    }

    void SubscribeControlUI()
    {
        _controlUiSubscription.Disposable = _controlUI.Pressed
            .Subscribe(HandleUiPressed)
            .AddTo(this);
    }

    void UpdateLastPressedCommands()
    {
        if (WasPressedThisFrame(_leftAction))
        {
            _lastSteeringCommand = RaceControlCommand.Left;
            LogInput("Keyboard/InputAction pressed Left.");
        }
        if (WasPressedThisFrame(_rightAction))
        {
            _lastSteeringCommand = RaceControlCommand.Right;
            LogInput("Keyboard/InputAction pressed Right.");
        }
        if (WasPressedThisFrame(_moveForwardAction))
        {
            _lastMoveCommand = RaceControlCommand.MoveForward;
            LogInput("Keyboard/InputAction pressed MoveForward.");
        }
        if (WasPressedThisFrame(_stopAction))
        {
            _lastMoveCommand = RaceControlCommand.Stop;
            LogInput("Keyboard/InputAction pressed Stop.");
        }
    }

    float GetSteering()
    {
        bool left = IsLeftPressed();
        bool right = IsRightPressed();

        if (_lastSteeringCommand == RaceControlCommand.Left && left)
        {
            return -1f;
        }
        if (_lastSteeringCommand == RaceControlCommand.Right && right)
        {
            return 1f;
        }
        if (left)
        {
            return -1f;
        }
        if (right)
        {
            return 1f;
        }

        return 0f;
    }

    bool IsMoveForwardPressed()
    {
        bool forward = IsPressed(_moveForwardAction) || (_controlUI != null && _controlUI.MoveForwardPressed.Value);
        bool stop = IsStopPressedRaw();

        if (_lastMoveCommand == RaceControlCommand.Stop && stop)
        {
            return false;
        }

        return forward;
    }

    bool IsStopPressed()
    {
        bool stop = IsStopPressedRaw();
        bool forward = IsPressed(_moveForwardAction) || (_controlUI != null && _controlUI.MoveForwardPressed.Value);

        if (_lastMoveCommand == RaceControlCommand.MoveForward && forward)
        {
            return false;
        }

        return stop;
    }

    bool IsLeftPressed()
    {
        return IsPressed(_leftAction) || (_controlUI != null && _controlUI.LeftPressed.Value);
    }

    bool IsRightPressed()
    {
        return IsPressed(_rightAction) || (_controlUI != null && _controlUI.RightPressed.Value);
    }

    bool IsStopPressedRaw()
    {
        return IsPressed(_stopAction) || (_controlUI != null && _controlUI.StopPressed.Value);
    }

    static bool IsPressed(InputActionReference inputActionReference)
    {
        InputAction action = inputActionReference != null ? inputActionReference.action : null;
        return action != null && action.IsPressed();
    }

    static bool WasPressedThisFrame(InputActionReference inputActionReference)
    {
        InputAction action = inputActionReference != null ? inputActionReference.action : null;
        return action != null && action.WasPressedThisFrame();
    }

    static void Enable(InputActionReference inputActionReference)
    {
        InputAction action = inputActionReference != null ? inputActionReference.action : null;
        if (action != null && !action.enabled)
        {
            action.Enable();
        }
    }

    static void Disable(InputActionReference inputActionReference)
    {
        InputAction action = inputActionReference != null ? inputActionReference.action : null;
        if (action != null && action.enabled)
        {
            action.Disable();
        }
    }

    void LogInputState(float steering, bool forward, bool stop)
    {
        bool left = IsLeftPressed();
        bool right = IsRightPressed();
        bool stateChanged = left != _previousLeft ||
            right != _previousRight ||
            forward != _previousForward ||
            stop != _previousStop ||
            !Mathf.Approximately(steering, _previousSteering);

        if (stateChanged || Mathf.Abs(steering) > 0.01f)
        {
            LogInput(
                $"state left={left}, right={right}, forward={forward}, stop={stop}, steering={steering:0.00}, " +
                $"lastSteering={_lastSteeringCommand}, lastMove={_lastMoveCommand}, controlEnabled={_controller.ControlEnabled}, " +
                $"actions(L={Describe(_leftAction)}, R={Describe(_rightAction)}, F={Describe(_moveForwardAction)}, S={Describe(_stopAction)}), " +
                $"ui(L={(_controlUI != null && _controlUI.LeftPressed.Value)}, R={(_controlUI != null && _controlUI.RightPressed.Value)}, F={(_controlUI != null && _controlUI.MoveForwardPressed.Value)}, S={(_controlUI != null && _controlUI.StopPressed.Value)})");
        }

        _previousLeft = left;
        _previousRight = right;
        _previousForward = forward;
        _previousStop = stop;
        _previousSteering = steering;
    }

    void LogInput(string message)
    {
        if (_debugInput)
        {
            Debug.Log($"[{nameof(PrometeoPlayerInput)}] {message}", this);
        }
    }

    static string Describe(InputActionReference inputActionReference)
    {
        InputAction action = inputActionReference != null ? inputActionReference.action : null;
        if (action == null)
        {
            return "null";
        }

        return $"{action.actionMap?.name}/{action.name}:pressed={action.IsPressed()}";
    }
}
