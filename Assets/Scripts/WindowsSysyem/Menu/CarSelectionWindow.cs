using System.Collections;
using System.Collections.Generic;
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

    readonly List<CharacterButton> _characterButtons = new List<CharacterButton>();
    GameObject _currentPreview;
    int _selectedIndex;
    bool _switching;

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
        _previousButton?.onClick.RemoveListener(Previous);
        _nextButton?.onClick.RemoveListener(Next);
        _backButton?.onClick.RemoveListener(Back);
        _confirmButton?.onClick.RemoveListener(Confirm);
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        EnsureMetaService();
        BuildButtons();

        int count = Characters != null ? Characters.Count : 0;
        _selectedIndex = count > 0 ? Mathf.Clamp(_metaGameService.SelectedCarIndex, 0, count - 1) : 0;
        SpawnPreview(_selectedIndex, Vector3.zero, true);
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
            CharacterButton button = Instantiate(_characterButtonTemplate, _gridRoot);
            button.gameObject.SetActive(true);
            button.Initialize(Characters[i], index, Select);
            _characterButtons.Add(button);
        }
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
        if (Characters == null || Characters.Count == 0)
        {
            return;
        }

        Select((_selectedIndex - 1 + Characters.Count) % Characters.Count);
    }

    void Next()
    {
        if (Characters == null || Characters.Count == 0)
        {
            return;
        }

        Select((_selectedIndex + 1) % Characters.Count);
    }

    void Select(int index)
    {
        if (_switching || Characters == null || Characters.Count == 0)
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
        StartCoroutine(SwitchPreview(index, direction));
        RefreshDetails();
    }

    IEnumerator SwitchPreview(int index, int direction)
    {
        _switching = true;
        GameObject previous = _currentPreview;
        GameObject next = SpawnPreview(index, new Vector3(_previewOffset * direction, 0f, 0f), false);

        float elapsed = 0f;
        Vector3 previousStart = previous != null ? previous.transform.localPosition : Vector3.zero;
        Vector3 previousEnd = new Vector3(-_previewOffset * direction, 0f, 0f);
        Vector3 nextStart = next != null ? next.transform.localPosition : Vector3.zero;

        while (elapsed < _switchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _switchDuration)));
            if (previous != null)
            {
                previous.transform.localPosition = Vector3.Lerp(previousStart, previousEnd, t);
            }

            if (next != null)
            {
                next.transform.localPosition = Vector3.Lerp(nextStart, Vector3.zero, t);
            }

            yield return null;
        }

        if (previous != null)
        {
            Destroy(previous);
        }

        if (next != null)
        {
            next.transform.localPosition = Vector3.zero;
        }

        _switching = false;
    }

    GameObject SpawnPreview(int index, Vector3 localPosition, bool clearPrevious)
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
        instance.transform.localScale = Vector3.one;
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
        _windowsService?.Back();
    }

    void Confirm()
    {
        EnsureMetaService();
        _metaGameService.SelectCar(_selectedIndex);
        if (_settings?.windowsConfig?.trackSelectionWindowId != null)
        {
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
}
