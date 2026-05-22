using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FinishScreen : BaseWindow
{
    [SerializeField] GameObject _root;
    [SerializeField] Transform _layoutRoot;
    [SerializeField] FinishRaceItem _itemPrefab;
    [SerializeField] Button _exitToMenuButton;

    [Inject(Optional = true)] RaceController _raceController;
    [Inject(Optional = true)] IWindowsService _windowsService;
    [Inject(Optional = true)] GlobalSettings _settings;

    readonly CompositeDisposable _disposables = new CompositeDisposable();
    readonly List<FinishRaceItem> _items = new List<FinishRaceItem>();
    bool _shown;

    public override bool AddToWindowStack => false;
    public override bool CloseByBack => false;
    protected override bool DisableGameObjectWhenHidden => false;

    protected override void Awake()
    {
        base.Awake();
        AutoBindExitButton();
        _exitToMenuButton?.onClick.AddListener(OpenExitToMenuConfirmation);
    }

    protected override void Start()
    {
        base.Start();

        if (_raceController == null)
        {
            _raceController = FindFirstObjectByType<RaceController>();
        }

        if (_raceController == null)
        {
            Debug.LogError($"{nameof(FinishScreen)} has no {nameof(RaceController)}.", this);
            return;
        }

        if (_root != null)
        {
            _root.SetActive(false);
        }

        _raceController.PlayerFinished
            .Subscribe(_ =>
            {
                Debug.Log($"{nameof(FinishScreen)} received PlayerFinished event.", this);
                Show();
            })
            .AddTo(_disposables);

        _raceController.ParticipantFinished
            .Subscribe(_ =>
            {
                if (_shown)
                {
                    Refresh();
                }
            })
            .AddTo(_disposables);
    }

    void OnDestroy()
    {
        _exitToMenuButton?.onClick.RemoveListener(OpenExitToMenuConfirmation);
        _disposables.Dispose();
    }

    void Show()
    {
        _shown = true;
        if (_root != null)
        {
            _root.SetActive(true);
        }

        Refresh();
        ShowRoutine(null, false).Subscribe().AddTo(this);
    }

    void Refresh()
    {
        if (_layoutRoot == null || _itemPrefab == null || _raceController == null)
        {
            return;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
            {
                Destroy(_items[i].gameObject);
            }
        }

        _items.Clear();
        foreach (RaceParticipant participant in _raceController.Participants.Where(p => p.Finished).OrderBy(p => p.FinishPlace))
        {
            FinishRaceItem item = Instantiate(_itemPrefab, _layoutRoot);
            item.gameObject.SetActive(true);
            item.Initialize(participant);
            _items.Add(item);
        }
    }

    void OpenExitToMenuConfirmation()
    {
        WindowId exitToMenuId = _settings?.windowsConfig?.exitToMenuConfirmationPopupId;
        if (_windowsService != null && exitToMenuId != null)
        {
            Debug.Log($"{nameof(FinishScreen)} opening exit to menu confirmation popup: {exitToMenuId.name}.", this);
            _windowsService.OpenPopup(exitToMenuId);
            return;
        }

        Debug.LogError($"{nameof(FinishScreen)} cannot open exit to menu confirmation popup.", this);
    }

    void AutoBindExitButton()
    {
        if (_exitToMenuButton != null)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name;
            if (buttonName.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                buttonName.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                buttonName.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _exitToMenuButton = buttons[i];
                return;
            }
        }

        if (buttons.Length == 1)
        {
            _exitToMenuButton = buttons[0];
        }
    }
}
