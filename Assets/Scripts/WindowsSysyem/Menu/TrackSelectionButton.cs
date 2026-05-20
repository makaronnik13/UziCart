using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackSelectionButton : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] Image _icon;
    [SerializeField] TMP_Text _nameText;

    TrackConfigSO _track;
    int _index;
    Action<int> _onClick;

    public TrackConfigSO Track => _track;

    void Awake()
    {
        AutoBind();
    }

    void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Initialize(TrackConfigSO track, int index, Action<int> onClick)
    {
        _track = track;
        _index = index;
        _onClick = onClick;
        AutoBind();

        if (_icon != null)
        {
            _icon.sprite = track != null ? track.Preview : null;
            _icon.enabled = _icon.sprite != null;
            _icon.preserveAspect = true;
        }

        string trackName = track != null ? track.TrackName : "Track";
        if (_nameText != null)
        {
            _nameText.text = trackName;
        }
    }

    public void SetSelected(bool selected)
    {
        if (_button != null)
        {
            _button.interactable = !selected;
        }
    }

    void HandleClick()
    {
        _onClick?.Invoke(_index);
    }

    void AutoBind()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_icon == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject != gameObject)
                {
                    _icon = images[i];
                    break;
                }
            }
        }

        if (_nameText == null)
        {
            _nameText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
