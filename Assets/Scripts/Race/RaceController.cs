using System.Collections.Generic;
using System;
using UniRx;
using UnityEngine;
using Zenject;

public class RaceController : MonoBehaviour
{
    [SerializeField] Collider _finishTrigger;
    [SerializeField] Collider[] _checkpointTriggers;
    [SerializeField] Transform _playerSpawnPoint;
    [SerializeField] Transform[] _aiSpawnPoints;
    [SerializeField, Min(1)] int _lapsCount = 3;
    [SerializeField] RaceCamera _raceCamera;
    [SerializeField] CountdownTimerWindow _countdownTimerWindow;
    [SerializeField] RaceControlUI _raceControlUI;
    [SerializeField] RaceUi _raceUi;
    [SerializeField] PrometeoPlayerInput _playerInput;
    [SerializeField] bool _autoStartIntro = true;
    [SerializeField] bool _debugRaceFlow = true;

    [Inject(Optional = true)] GlobalSettings _settings;
    [Inject(Optional = true)] MetaGameService _metaGameService;
    [Inject(Optional = true)] DiContainer _container;

    readonly List<RaceParticipant> _participants = new List<RaceParticipant>();
    readonly Dictionary<PrometeoCarController, RaceParticipant> _participantsByController = new Dictionary<PrometeoCarController, RaceParticipant>();
    readonly Subject<Unit> _raceStarted = new Subject<Unit>();
    readonly Subject<RaceParticipant> _playerSpawned = new Subject<RaceParticipant>();
    readonly Subject<RaceParticipant> _participantFinished = new Subject<RaceParticipant>();
    readonly Subject<RaceParticipant> _playerFinished = new Subject<RaceParticipant>();
    readonly Subject<RaceParticipant> _aiFinished = new Subject<RaceParticipant>();
    readonly ReactiveProperty<int> _playerCurrentLap = new ReactiveProperty<int>(1);
    readonly CompositeDisposable _disposables = new CompositeDisposable();

    float _raceStartTime;
    bool _raceStartedFlag;
    bool _startSequenceStarted;
    int _finishCount;

    public IReadOnlyList<RaceParticipant> Participants => _participants;
    public RaceParticipant PlayerParticipant { get; private set; }
    public int LapsCount => _lapsCount;
    public IReadOnlyReactiveProperty<int> PlayerCurrentLap => _playerCurrentLap;
    public IObservable<Unit> RaceStarted => _raceStarted;
    public IObservable<RaceParticipant> PlayerSpawned => _playerSpawned;
    public IObservable<RaceParticipant> ParticipantFinished => _participantFinished;
    public IObservable<RaceParticipant> PlayerFinished => _playerFinished;
    public IObservable<RaceParticipant> AiFinished => _aiFinished;

    void Start()
    {
        _metaGameService ??= FindFirstObjectByType<MetaGameService>();
        FindSceneReferences();
        SetRaceHudVisible(false);
        RegisterTriggers();
        SpawnParticipants();

        if (_autoStartIntro)
        {
            StartRaceIntro();
        }
    }

    void OnDestroy()
    {
        _disposables.Dispose();
        _raceStarted.Dispose();
        _playerSpawned.Dispose();
        _participantFinished.Dispose();
        _playerFinished.Dispose();
        _aiFinished.Dispose();
        _playerCurrentLap.Dispose();
    }

    [ContextMenu("Start Race")]
    public void StartRace()
    {
        if (_participants.Count == 0 || _raceStartedFlag)
        {
            return;
        }

        _raceStartedFlag = true;
        _raceStartTime = Time.time;
        SetRaceHudVisible(true);
        for (int i = 0; i < _participants.Count; i++)
        {
            _participants[i].Controller.SetControlEnabled(true);
            LogRace($"Control enabled for {_participants[i].DisplayName}. isPlayer={_participants[i].IsPlayer}");
        }

        LogRace($"Race started. participants={_participants.Count}, laps={_lapsCount}, checkpoints={(_checkpointTriggers != null ? _checkpointTriggers.Length : 0)}");
        _raceStarted.OnNext(Unit.Default);
    }

    [ContextMenu("Start Race Intro")]
    public void StartRaceIntro()
    {
        if (_participants.Count == 0 || _raceStartedFlag || _startSequenceStarted)
        {
            return;
        }

        if (!ValidateStartSequenceReferences())
        {
            return;
        }

        _startSequenceStarted = true;
        SetRaceHudVisible(false);
        for (int i = 0; i < _participants.Count; i++)
        {
            _participants[i].Controller.SetControlEnabled(false);
        }

        if (_raceCamera != null && PlayerParticipant != null)
        {
            _raceCamera.InitializeTarget(PlayerParticipant.Controller.transform);
            _raceCamera.ShowIntroCamera();
        }

        float introCameraDuration = _raceCamera.IntroCameraDuration;
        float cameraBlendDuration = _raceCamera.CameraBlendDuration;

        Observable.Timer(TimeSpan.FromSeconds(introCameraDuration))
            .Do(_ => _raceCamera.BlendToMainCamera(cameraBlendDuration))
            .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(cameraBlendDuration)))
            .SelectMany(_ => _countdownTimerWindow != null
                ? _countdownTimerWindow.PlayCountdown()
                : Observable.ReturnUnit())
            .Subscribe(
                _ => { },
                error =>
                {
                    _startSequenceStarted = false;
                    Debug.LogError(error, this);
                },
                StartRace)
            .AddTo(_disposables);
    }

    public void HandleCheckpointTrigger(Collider other, int checkpointIndex)
    {
        if (!_raceStartedFlag || checkpointIndex < 0)
        {
            LogRace($"Checkpoint ignored before race start or invalid index. checkpoint={checkpointIndex}, other={other.name}");
            return;
        }

        RaceParticipant participant = GetParticipant(other);
        if (participant == null || participant.Finished)
        {
            LogRace($"Checkpoint ignored. participant={(participant != null ? participant.DisplayName : "null")}, finished={participant?.Finished}, other={other.name}, checkpoint={checkpointIndex}");
            return;
        }

        if (participant.NextCheckpointIndex == checkpointIndex)
        {
            participant.NextCheckpointIndex++;
            LogRace($"{participant.DisplayName} passed checkpoint {checkpointIndex}. next={participant.NextCheckpointIndex}/{(_checkpointTriggers != null ? _checkpointTriggers.Length : 0)}");
        }
        else
        {
            LogRace($"{participant.DisplayName} checkpoint ignored. expected={participant.NextCheckpointIndex}, actual={checkpointIndex}");
        }
    }

    public void HandleFinishTrigger(Collider other)
    {
        if (!_raceStartedFlag)
        {
            LogRace($"Finish ignored before race start. other={other.name}");
            return;
        }

        RaceParticipant participant = GetParticipant(other);
        if (participant == null || participant.Finished)
        {
            LogRace($"Finish ignored. participant={(participant != null ? participant.DisplayName : "null")}, finished={participant?.Finished}, other={other.name}");
            return;
        }

        if (_checkpointTriggers != null && participant.NextCheckpointIndex < _checkpointTriggers.Length)
        {
            LogRace($"{participant.DisplayName} finish ignored: checkpoints not completed. next={participant.NextCheckpointIndex}/{_checkpointTriggers.Length}, completedLaps={participant.CompletedLaps}/{_lapsCount}, other={other.name}");
            return;
        }

        participant.NextCheckpointIndex = 0;
        participant.CompletedLaps++;
        LogRace($"{participant.DisplayName} crossed finish. completedLaps={participant.CompletedLaps}/{_lapsCount}, isPlayer={participant.IsPlayer}");

        if (participant.IsPlayer)
        {
            _playerCurrentLap.Value = Mathf.Clamp(participant.CompletedLaps + 1, 1, _lapsCount);
            LogRace($"Player lap property set to {_playerCurrentLap.Value}/{_lapsCount}");
        }

        if (participant.CompletedLaps >= _lapsCount)
        {
            FinishParticipant(participant);
        }
    }

    void RegisterTriggers()
    {
        if (_finishTrigger == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no finish trigger.", this);
            return;
        }

        RegisterTrigger(_finishTrigger, -1, true);
        if (_checkpointTriggers == null)
        {
            return;
        }

        for (int i = 0; i < _checkpointTriggers.Length; i++)
        {
            RegisterTrigger(_checkpointTriggers[i], i, false);
        }
    }

    void RegisterTrigger(Collider trigger, int checkpointIndex, bool isFinish)
    {
        if (trigger == null)
        {
            Debug.LogError($"{nameof(RaceController)} has empty trigger reference.", this);
            return;
        }

        if (!trigger.isTrigger)
        {
            Debug.LogError($"{trigger.name} must be marked as Trigger.", trigger);
        }

        RaceTriggerForwarder forwarder = trigger.GetComponent<RaceTriggerForwarder>();
        if (forwarder == null)
        {
            forwarder = trigger.gameObject.AddComponent<RaceTriggerForwarder>();
        }

        forwarder.Initialize(this, checkpointIndex, isFinish);
    }

    void SpawnParticipants()
    {
        if (_settings == null || _settings.cars == null || _settings.cars.Count == 0)
        {
            Debug.LogError($"{nameof(RaceController)} has no cars in GlobalSettings.", this);
            return;
        }

        int selectedIndex = _metaGameService != null
            ? Mathf.Clamp(_metaGameService.SelectedCarIndex, 0, _settings.cars.Count - 1)
            : 0;
        int aiCount = _settings.cars.Count - 1;
        if (_playerSpawnPoint == null || _aiSpawnPoints == null || _aiSpawnPoints.Length < aiCount)
        {
            Debug.LogError($"{nameof(RaceController)} does not have enough spawn points.", this);
            return;
        }

        for (int i = 0; i < aiCount; i++)
        {
            if (_aiSpawnPoints[i] == null)
            {
                Debug.LogError($"{nameof(RaceController)} has empty AI spawn point at index {i}.", this);
                return;
            }
        }

        for (int i = 0; i < _settings.cars.Count; i++)
        {
            CarConfigSO car = _settings.cars[i];
            if (car == null || car.RacePrefab == null)
            {
                Debug.LogError($"Car config at index {i} has no RacePrefab.", this);
                return;
            }
        }

        ClearParticipants();
        RaceParticipant player = SpawnParticipant(_settings.cars[selectedIndex], _playerSpawnPoint, true);
        if (player == null)
        {
            ClearParticipants();
            return;
        }

        PlayerParticipant = player;
        _playerCurrentLap.Value = 1;
        if (_raceCamera != null)
        {
            _raceCamera.InitializeTarget(player.Controller.transform);
        }
        BindPlayerInput(player);
        _playerSpawned.OnNext(player);

        int spawnIndex = 0;
        for (int i = 0; i < _settings.cars.Count; i++)
        {
            if (i == selectedIndex)
            {
                continue;
            }

            RaceParticipant ai = SpawnParticipant(_settings.cars[i], _aiSpawnPoints[spawnIndex], false);
            if (ai == null)
            {
                ClearParticipants();
                return;
            }

            spawnIndex++;
        }
    }

    RaceParticipant SpawnParticipant(CarConfigSO car, Transform spawnPoint, bool isPlayer)
    {
        GameObject instance = _container != null
            ? _container.InstantiatePrefab(car.RacePrefab, spawnPoint.position, spawnPoint.rotation, null)
            : Instantiate(car.RacePrefab, spawnPoint.position, spawnPoint.rotation);

        PrometeoCarController controller = instance.GetComponentInChildren<PrometeoCarController>();
        if (controller == null)
        {
            Debug.LogError($"{car.RacePrefab.name} has no {nameof(PrometeoCarController)}.", instance);
            Destroy(instance);
            return null;
        }

        controller.SetControlEnabled(false);
        if (isPlayer && _playerInput == null)
        {
            _playerInput = instance.GetComponentInChildren<PrometeoPlayerInput>(true);
        }

        RaceParticipant participant = new RaceParticipant(car, controller, isPlayer);
        _participants.Add(participant);
        _participantsByController[controller] = participant;
        return participant;
    }

    RaceParticipant GetParticipant(Collider other)
    {
        PrometeoCarController controller = other.GetComponentInParent<PrometeoCarController>();
        if (controller == null)
        {
            return null;
        }

        _participantsByController.TryGetValue(controller, out RaceParticipant participant);
        return participant;
    }

    void FinishParticipant(RaceParticipant participant)
    {
        participant.Finished = true;
        participant.TotalTime = Time.time - _raceStartTime;
        participant.FinishPlace = ++_finishCount;
        participant.Controller.ResetControlInput();
        participant.Controller.SetControlEnabled(false);
        LogRace($"{participant.DisplayName} finished. place={participant.FinishPlace}, time={participant.TotalTime:0.000}, controlEnabled={participant.Controller.ControlEnabled}");

        _participantFinished.OnNext(participant);
        if (participant.IsPlayer)
        {
            CompleteUnfinishedParticipantsAfterPlayer(participant.TotalTime);
            LogRace("PlayerFinished event emitted.");
            _playerFinished.OnNext(participant);
        }
        else
        {
            LogRace("AiFinished event emitted.");
            _aiFinished.OnNext(participant);
        }
    }

    void CompleteUnfinishedParticipantsAfterPlayer(float playerFinishTime)
    {
        float minOffset = _settings != null ? Mathf.Max(0f, _settings.unfinishedRaceTimeMinOffset) : 2f;
        float maxOffset = _settings != null ? Mathf.Max(minOffset, _settings.unfinishedRaceTimeMaxOffset) : 15f;
        List<RaceParticipant> unfinishedParticipants = new List<RaceParticipant>();
        for (int i = 0; i < _participants.Count; i++)
        {
            RaceParticipant participant = _participants[i];
            if (participant != null && !participant.Finished)
            {
                participant.TotalTime = playerFinishTime + UnityEngine.Random.Range(minOffset, maxOffset);
                unfinishedParticipants.Add(participant);
            }
        }

        unfinishedParticipants.Sort((left, right) => left.TotalTime.CompareTo(right.TotalTime));
        for (int i = 0; i < unfinishedParticipants.Count; i++)
        {
            RaceParticipant participant = unfinishedParticipants[i];
            participant.Finished = true;
            participant.CompletedLaps = _lapsCount;
            participant.FinishPlace = ++_finishCount;
            participant.Controller.ResetControlInput();
            participant.Controller.SetControlEnabled(false);
            LogRace($"{participant.DisplayName} auto-finished after player. place={participant.FinishPlace}, time={participant.TotalTime:0.000}");
            _participantFinished.OnNext(participant);
            _aiFinished.OnNext(participant);
        }
    }

    void ClearParticipants()
    {
        for (int i = 0; i < _participants.Count; i++)
        {
            if (_participants[i].Controller != null)
            {
                Destroy(_participants[i].Controller.gameObject);
            }
        }

        _participants.Clear();
        _participantsByController.Clear();
        PlayerParticipant = null;
        _finishCount = 0;
        _raceStartedFlag = false;
        _startSequenceStarted = false;
    }

    void FindSceneReferences()
    {
        if (_raceCamera == null)
        {
            _raceCamera = FindFirstObjectByType<RaceCamera>();
        }
        if (_countdownTimerWindow == null)
        {
            _countdownTimerWindow = FindFirstObjectByType<CountdownTimerWindow>(FindObjectsInactive.Include);
        }
        if (_raceControlUI == null)
        {
            _raceControlUI = FindFirstObjectByType<RaceControlUI>(FindObjectsInactive.Include);
        }
        if (_raceUi == null)
        {
            _raceUi = FindFirstObjectByType<RaceUi>(FindObjectsInactive.Include);
        }
        if (_playerInput == null)
        {
            _playerInput = FindFirstObjectByType<PrometeoPlayerInput>(FindObjectsInactive.Include);
        }
    }

    void SetRaceHudVisible(bool visible)
    {
        _raceControlUI?.SetVisible(visible);
        _raceUi?.SetVisible(visible);
    }

    bool ValidateStartSequenceReferences()
    {
        bool valid = true;
        if (_raceCamera == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(RaceCamera)}.", this);
            valid = false;
        }
        if (_countdownTimerWindow == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(CountdownTimerWindow)}.", this);
            valid = false;
        }
        if (_raceControlUI == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(RaceControlUI)}.", this);
            valid = false;
        }
        if (_raceUi == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(RaceUi)}.", this);
            valid = false;
        }
        if (_playerInput == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(PrometeoPlayerInput)}.", this);
            valid = false;
        }

        return valid;
    }

    void BindPlayerInput(RaceParticipant player)
    {
        if (player == null || player.Controller == null)
        {
            Debug.LogError($"{nameof(RaceController)} cannot bind player input without player controller.", this);
            return;
        }

        if (_playerInput == null)
        {
            Debug.LogError($"{nameof(RaceController)} has no {nameof(PrometeoPlayerInput)} for player control.", this);
            return;
        }

        _playerInput.Initialize(player.Controller, _raceControlUI);
    }

    void LogRace(string message)
    {
        if (_debugRaceFlow)
        {
            Debug.Log($"[RaceController] {message}", this);
        }
    }
}
