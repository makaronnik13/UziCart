using System;
using UniRx;
using Unity.Cinemachine;
using UnityEngine;

public class RaceCamera : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] CinemachineCamera _introCamera;
    [SerializeField] CinemachineCamera _mainCamera;
    [SerializeField] CinemachineBrain _brain;

    [Header("Intro Timing")]
    [SerializeField, Min(0f)] float _introCameraDuration = 2f;
    [SerializeField, Min(0f)] float _cameraBlendDuration = 2f;

    [Header("Priorities")]
    [SerializeField] int _inactivePriority = 0;
    [SerializeField] int _activePriority = 20;

    [Header("Target")]
    [SerializeField] bool _assignTargetToIntroCamera = true;

    IDisposable _disableIntroSubscription;

    public float IntroCameraDuration => _introCameraDuration;
    public float CameraBlendDuration => _cameraBlendDuration;

    void Awake()
    {
        if (_brain == null)
        {
            _brain = FindFirstObjectByType<CinemachineBrain>();
        }
    }

    void OnDestroy()
    {
        _disableIntroSubscription?.Dispose();
    }

    public void InitializeTarget(Transform playerCar)
    {
        if (playerCar == null)
        {
            Debug.LogError($"{nameof(RaceCamera)} received empty player target.", this);
            return;
        }

        AssignTarget(_mainCamera, playerCar);
        if (_assignTargetToIntroCamera)
        {
            AssignTarget(_introCamera, playerCar);
        }
    }

    public void ShowIntroCamera()
    {
        if (!ValidateCameras())
        {
            return;
        }

        _disableIntroSubscription?.Dispose();
        _introCamera.gameObject.SetActive(true);
        _mainCamera.gameObject.SetActive(true);
        _introCamera.Priority.Value = _activePriority;
        _mainCamera.Priority.Value = _inactivePriority;
    }

    public void BlendToMainCamera(float blendDuration)
    {
        if (!ValidateCameras())
        {
            return;
        }

        if (_brain != null)
        {
            _brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut,
                Mathf.Max(0f, blendDuration));
        }

        _mainCamera.gameObject.SetActive(true);
        _mainCamera.Priority.Value = _activePriority;
        _introCamera.Priority.Value = _inactivePriority;

        _disableIntroSubscription?.Dispose();
        _disableIntroSubscription = Observable.Timer(TimeSpan.FromSeconds(Mathf.Max(0f, blendDuration)))
            .Subscribe(_ =>
            {
                if (_introCamera != null)
                {
                    _introCamera.gameObject.SetActive(false);
                }
            })
            .AddTo(this);
    }

    void AssignTarget(CinemachineCamera camera, Transform target)
    {
        if (camera == null)
        {
            return;
        }

        camera.Target.TrackingTarget = target;
        camera.Target.LookAtTarget = target;
        camera.Target.CustomLookAtTarget = true;
    }

    bool ValidateCameras()
    {
        if (_introCamera == null)
        {
            Debug.LogError($"{nameof(RaceCamera)} has no intro camera.", this);
            return false;
        }
        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(RaceCamera)} has no main camera.", this);
            return false;
        }

        return true;
    }
}
