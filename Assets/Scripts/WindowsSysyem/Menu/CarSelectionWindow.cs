using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CarSelectionWindow : BaseWindow
{
    [SerializeField] Transform _previewRoot;
    [SerializeField] Button _previousButton;
    [SerializeField] Button _nextButton;
    [SerializeField] Button _backButton;
    [SerializeField] Button _confirmButton;
    [SerializeField] Transform _gridRoot;
    [SerializeField] CharacterButton _characterButtonTemplate;
    [SerializeField] CharacterStatsPanel _statsPanel;
    [SerializeField] float _previewOffset = 8f;
    [SerializeField] float _switchDuration = 0.35f;

    [Inject(Optional = true)] GlobalSettings _settings;
    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] MetaGameService _metaGameService;
    [Inject(Optional = true)] DiContainer _container;

    readonly List<CharacterButton> _characterButtons = new List<CharacterButton>();
    GameObject _currentPreview;
    int _selectedIndex;
    bool _switching;
    bool _leaving;
    IDisposable _previewAnimation;

    IReadOnlyList<CarConfigSO> Characters => _settings != null ? _settings.cars : null;

    protected override void Awake()
    {
        base.Awake();
        _previousButton?.onClick.AddListener(Previous);
        _nextButton?.onClick.AddListener(Next);
        _backButton?.onClick.AddListener(Back);
        _confirmButton?.onClick.AddListener(Confirm);
    }

    void OnDestroy()
    {
        StopPreviewAnimation();
        _previousButton?.onClick.RemoveListener(Previous);
        _nextButton?.onClick.RemoveListener(Next);
        _backButton?.onClick.RemoveListener(Back);
        _confirmButton?.onClick.RemoveListener(Confirm);
    }

    protected override void OnDisable()
    {
        _switching = false;
        _leaving = false;
        base.OnDisable();
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        EnsureMetaService();
        _switching = false;
        _leaving = false;
        BuildButtons();

        _selectedIndex = 0;
        GameObject preview = SpawnPreview(_selectedIndex, new Vector3(_previewOffset, 0f, 0f), Vector3.zero, true);
        if (preview != null)
        {
            _switching = true;
            PlayPreviewAnimation(
                AnimatePreview(preview, preview.transform.localPosition, Vector3.zero, preview.transform.localScale, Vector3.one),
                () => _switching = false,
                preview.transform);
        }

        RefreshDetails();
    }

    void EnsureMetaService()
    {
        _metaGameService ??= new MetaGameService();
    }

    void BuildButtons()
    {
        EnsureTemplates();

        for (int i = 0; i < _characterButtons.Count; i++)
        {
            if (_characterButtons[i] != null && _characterButtons[i] != _characterButtonTemplate)
            {
                Destroy(_characterButtons[i].gameObject);
            }
        }

        _characterButtons.Clear();
        if (_characterButtonTemplate == null || _gridRoot == null || Characters == null)
        {
            return;
        }

        _characterButtonTemplate.gameObject.SetActive(false);
        for (int i = 0; i < Characters.Count; i++)
        {
            int index = i;
            CharacterButton button = CreateCharacterButton();
            button.gameObject.SetActive(true);
            button.Initialize(Characters[i], index, Select);
            _characterButtons.Add(button);
        }
    }

    CharacterButton CreateCharacterButton()
    {
        if (_container != null)
        {
            return _container.InstantiatePrefabForComponent<CharacterButton>(_characterButtonTemplate, _gridRoot);
        }

        return Instantiate(_characterButtonTemplate, _gridRoot);
    }

    void EnsureTemplates()
    {
        if (_characterButtonTemplate == null && _gridRoot != null)
        {
            _characterButtonTemplate = _gridRoot.GetComponentInChildren<CharacterButton>(true);
            if (_characterButtonTemplate == null)
            {
                Transform templateTransform = _gridRoot.Find("CarButtonTemplate");
                if (templateTransform != null)
                {
                    _characterButtonTemplate = templateTransform.GetComponent<CharacterButton>();
                    if (_characterButtonTemplate == null)
                    {
                        _characterButtonTemplate = templateTransform.gameObject.AddComponent<CharacterButton>();
                    }
                }
            }
        }

        if (_statsPanel == null)
        {
            _statsPanel = GetComponentInChildren<CharacterStatsPanel>(true);
            if (_statsPanel == null)
            {
                _statsPanel = gameObject.AddComponent<CharacterStatsPanel>();
            }
        }
    }

    void Previous()
    {
        if (_leaving || Characters == null || Characters.Count == 0)
        {
            return;
        }

        Select((_selectedIndex - 1 + Characters.Count) % Characters.Count);
    }

    void Next()
    {
        if (_leaving || Characters == null || Characters.Count == 0)
        {
            return;
        }

        Select((_selectedIndex + 1) % Characters.Count);
    }

    void Select(int index)
    {
        if (_switching || _leaving || Characters == null || Characters.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, Characters.Count - 1);
        if (index == _selectedIndex)
        {
            RefreshDetails();
            return;
        }

        int direction = index > _selectedIndex ? 1 : -1;
        _selectedIndex = index;
        SwitchPreview(index, direction);
        RefreshDetails();
    }

    void SwitchPreview(int index, int direction)
    {
        _switching = true;
        GameObject previous = _currentPreview;
        GameObject next = SpawnPreview(index, new Vector3(_previewOffset * direction, 0f, 0f), Vector3.zero, false);

        Vector3 previousStart = previous != null ? previous.transform.localPosition : Vector3.zero;
        Vector3 previousScaleStart = previous != null ? previous.transform.localScale : Vector3.one;
        Vector3 previousEnd = new Vector3(-_previewOffset * direction, 0f, 0f);
        Vector3 nextStart = next != null ? next.transform.localPosition : Vector3.zero;
        Vector3 nextScaleStart = next != null ? next.transform.localScale : Vector3.zero;

        PlayPreviewAnimation(
            AnimatePreviewPair(
                previous, previousStart, previousEnd, previousScaleStart, Vector3.zero,
                next, nextStart, Vector3.zero, nextScaleStart, Vector3.one),
            () =>
            {
                if (previous != null)
                {
                    Destroy(previous);
                }

                if (next != null)
                {
                    next.transform.localPosition = Vector3.zero;
                    next.transform.localScale = Vector3.one;
                }

                _switching = false;
            },
            next != null ? next.transform : previous != null ? previous.transform : null);
    }

    float Ease(float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }

    IObservable<Unit> AnimatePreview(GameObject preview, Vector3 startPosition, Vector3 endPosition, Vector3 startScale, Vector3 endScale)
    {
        return AnimatePreviewPair(preview, startPosition, endPosition, startScale, endScale, null, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
    }

    IObservable<Unit> AnimatePreviewPair(
        GameObject first, Vector3 firstStartPosition, Vector3 firstEndPosition, Vector3 firstStartScale, Vector3 firstEndScale,
        GameObject second, Vector3 secondStartPosition, Vector3 secondEndPosition, Vector3 secondStartScale, Vector3 secondEndScale)
    {
        return Observable.Create<Unit>(observer =>
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _switchDuration);

            IDisposable subscription = null;
            subscription = Observable.EveryUpdate().Subscribe(_ =>
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Ease(elapsed / duration);

                ApplyPreviewFrame(first, firstStartPosition, firstEndPosition, firstStartScale, firstEndScale, t);
                ApplyPreviewFrame(second, secondStartPosition, secondEndPosition, secondStartScale, secondEndScale, t);

                if (elapsed < duration)
                {
                    return;
                }

                ApplyPreviewFrame(first, firstStartPosition, firstEndPosition, firstStartScale, firstEndScale, 1f);
                ApplyPreviewFrame(second, secondStartPosition, secondEndPosition, secondStartScale, secondEndScale, 1f);
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                subscription?.Dispose();
            });

            return subscription;
        });
    }

    void ApplyPreviewFrame(GameObject preview, Vector3 startPosition, Vector3 endPosition, Vector3 startScale, Vector3 endScale, float t)
    {
        if (preview == null)
        {
            return;
        }

        preview.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
        preview.transform.localScale = Vector3.Lerp(startScale, endScale, t);
    }

    void PlayPreviewAnimation(IObservable<Unit> animation, Action onComplete, Transform owner)
    {
        StopPreviewAnimation();
        IDisposable subscription = animation.Subscribe(
            _ => { },
            () =>
            {
                _previewAnimation = null;
                onComplete?.Invoke();
            });
        _previewAnimation = subscription;

        if (owner != null)
        {
            subscription.AddTo(owner);
        }
    }

    void StopPreviewAnimation()
    {
        _previewAnimation?.Dispose();
        _previewAnimation = null;
    }

    void LeavePreview(int direction)
    {
        _leaving = true;

        GameObject preview = _currentPreview;
        if (preview == null)
        {
            return;
        }

        Vector3 endPosition = new Vector3(_previewOffset * direction, 0f, 0f);
        PlayPreviewAnimation(
            AnimatePreview(preview, preview.transform.localPosition, endPosition, preview.transform.localScale, Vector3.zero),
            null,
            preview.transform);
    }

    GameObject SpawnPreview(int index, Vector3 localPosition, Vector3 localScale, bool clearPrevious)
    {
        if (_previewRoot == null || Characters == null || index < 0 || index >= Characters.Count)
        {
            return null;
        }

        if (clearPrevious && _currentPreview != null)
        {
            Destroy(_currentPreview);
            _currentPreview = null;
        }

        CarConfigSO character = Characters[index];
        if (character == null || character.Prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(character.Prefab, _previewRoot);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = localScale;
        _currentPreview = instance;
        return instance;
    }

    void RefreshDetails()
    {
        CarConfigSO character = Characters != null && _selectedIndex >= 0 && _selectedIndex < Characters.Count
            ? Characters[_selectedIndex]
            : null;
        _statsPanel?.SetCharacter(character);
        RefreshButtonSelection();
    }

    void RefreshButtonSelection()
    {
        for (int i = 0; i < _characterButtons.Count; i++)
        {
            if (_characterButtons[i] != null)
            {
                _characterButtons[i].SetSelected(i == _selectedIndex);
            }
        }
    }

    void Back()
    {
        if (_leaving)
        {
            return;
        }

        LeavePreview(-1);
        _windowsService?.Back();
    }

    void Confirm()
    {
        if (_leaving)
        {
            return;
        }

        LeavePreview(1);
        EnsureMetaService();
        _metaGameService.SelectCar(_selectedIndex);
        if (_settings?.windowsConfig?.trackSelectionWindowId == null)
        {
            return;
        }

        WindowId trackSelectionWindowId = _settings.windowsConfig.trackSelectionWindowId;
        if (_settings.windowsConfig.IsPopup(trackSelectionWindowId))
        {
            _windowsService?.OpenPopup(trackSelectionWindowId);
        }
        else
        {
            _windowsService?.Open(trackSelectionWindowId, keepPrevious: true);
        }
    }
}
