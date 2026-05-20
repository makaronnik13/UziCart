using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class TrackSelectionWindow : BaseWindow
{
    [SerializeField] Transform _gridRoot;
    [SerializeField] TrackSelectionButton _trackButtonTemplate;
    [SerializeField] Button _backButton;
    [SerializeField] Button _confirmButton;

    [Inject(Optional = true)] GlobalSettings _settings;
    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] MetaGameService _metaGameService;

    readonly List<TrackSelectionButton> _trackButtons = new List<TrackSelectionButton>();
    int _selectedIndex;

    IReadOnlyList<TrackConfigSO> Tracks => _settings != null ? _settings.tracks : null;

    protected override void Awake()
    {
        base.Awake();
        _backButton?.onClick.AddListener(Back);
        _confirmButton?.onClick.AddListener(Confirm);
    }

    void OnDestroy()
    {
        _backButton?.onClick.RemoveListener(Back);
        _confirmButton?.onClick.RemoveListener(Confirm);
    }

    protected override void OnShow(object payload)
    {
        base.OnShow(payload);
        _metaGameService ??= new MetaGameService();
        BuildButtons();
        int count = Tracks != null ? Tracks.Count : 0;
        _selectedIndex = count > 0 ? Mathf.Clamp(_metaGameService.SelectedTrackIndex, 0, count - 1) : 0;
        RefreshSelection();
    }

    void BuildButtons()
    {
        EnsureTemplate();

        for (int i = 0; i < _trackButtons.Count; i++)
        {
            if (_trackButtons[i] != null && _trackButtons[i] != _trackButtonTemplate)
            {
                Destroy(_trackButtons[i].gameObject);
            }
        }

        _trackButtons.Clear();
        if (_trackButtonTemplate == null || _gridRoot == null || Tracks == null)
        {
            return;
        }

        _trackButtonTemplate.gameObject.SetActive(false);
        int count = Mathf.Min(6, Tracks.Count);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            TrackSelectionButton button = Instantiate(_trackButtonTemplate, _gridRoot);
            button.gameObject.SetActive(true);
            button.Initialize(Tracks[i], index, Select);
            _trackButtons.Add(button);
        }
    }

    void EnsureTemplate()
    {
        if (_trackButtonTemplate == null && _gridRoot != null)
        {
            _trackButtonTemplate = _gridRoot.GetComponentInChildren<TrackSelectionButton>(true);
            if (_trackButtonTemplate == null)
            {
                Button buttonTemplate = _gridRoot.GetComponentInChildren<Button>(true);
                if (buttonTemplate != null)
                {
                    _trackButtonTemplate = buttonTemplate.GetComponent<TrackSelectionButton>();
                    if (_trackButtonTemplate == null)
                    {
                        _trackButtonTemplate = buttonTemplate.gameObject.AddComponent<TrackSelectionButton>();
                    }
                }
            }
        }
    }

    void Select(int index)
    {
        _selectedIndex = index;
        RefreshSelection();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < _trackButtons.Count; i++)
        {
            if (_trackButtons[i] != null)
            {
                _trackButtons[i].SetSelected(i == _selectedIndex);
            }
        }
    }

    void Back()
    {
        _windowsService?.Back();
    }

    void Confirm()
    {
        _metaGameService ??= new MetaGameService();
        _metaGameService.SelectTrack(_selectedIndex);

        TrackConfigSO track = Tracks != null && _selectedIndex >= 0 && _selectedIndex < Tracks.Count
            ? Tracks[_selectedIndex]
            : null;
        if (track == null || string.IsNullOrWhiteSpace(track.SceneName))
        {
            Debug.LogError("Selected track has no scene name.");
            return;
        }

        SceneManager.LoadScene(track.SceneName);
    }
}
