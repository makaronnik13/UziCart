using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RaceControlUI : MonoBehaviour
{
    [SerializeField] Button _leftButton;
    [SerializeField] Button _rightButton;
    [SerializeField] Button _moveForwardButton;
    [SerializeField] Button _stopButton;
    [SerializeField] CanvasGroup _canvasGroup;

    readonly ReactiveProperty<bool> _leftPressed = new ReactiveProperty<bool>();
    readonly ReactiveProperty<bool> _rightPressed = new ReactiveProperty<bool>();
    readonly ReactiveProperty<bool> _moveForwardPressed = new ReactiveProperty<bool>();
    readonly ReactiveProperty<bool> _stopPressed = new ReactiveProperty<bool>();
    readonly Subject<RaceControlCommand> _pressed = new Subject<RaceControlCommand>();

    public IReadOnlyReactiveProperty<bool> LeftPressed => _leftPressed;
    public IReadOnlyReactiveProperty<bool> RightPressed => _rightPressed;
    public IReadOnlyReactiveProperty<bool> MoveForwardPressed => _moveForwardPressed;
    public IReadOnlyReactiveProperty<bool> StopPressed => _stopPressed;
    public IObservable<RaceControlCommand> Pressed => _pressed;

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

        BindButton(_leftButton, RaceControlCommand.Left, _leftPressed);
        BindButton(_rightButton, RaceControlCommand.Right, _rightPressed);
        BindButton(_moveForwardButton, RaceControlCommand.MoveForward, _moveForwardPressed);
        BindButton(_stopButton, RaceControlCommand.Stop, _stopPressed);
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

        if (!visible)
        {
            _leftPressed.Value = false;
            _rightPressed.Value = false;
            _moveForwardPressed.Value = false;
            _stopPressed.Value = false;
        }
    }

    void OnDisable()
    {
        _leftPressed.Value = false;
        _rightPressed.Value = false;
        _moveForwardPressed.Value = false;
        _stopPressed.Value = false;
    }

    void OnDestroy()
    {
        _leftPressed.Dispose();
        _rightPressed.Dispose();
        _moveForwardPressed.Dispose();
        _stopPressed.Dispose();
        _pressed.Dispose();
    }

    void BindButton(Button button, RaceControlCommand command, ReactiveProperty<bool> state)
    {
        if (button == null)
        {
            return;
        }

        RaceControlButtonBinding binding = button.GetComponent<RaceControlButtonBinding>();
        if (binding == null)
        {
            binding = button.gameObject.AddComponent<RaceControlButtonBinding>();
        }

        binding.Initialize(
            () =>
            {
                state.Value = true;
                _pressed.OnNext(command);
            },
            () => state.Value = false);
    }

    class RaceControlButtonBinding : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        System.Action _pressed;
        System.Action _released;

        public void Initialize(System.Action pressed, System.Action released)
        {
            _pressed = pressed;
            _released = released;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _released?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _released?.Invoke();
        }
    }
}
