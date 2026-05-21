using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] Image _icon;
    [SerializeField] TMP_Text _nameText;

    CarConfigSO _character;
    int _index;
    Action<int> _onClick;

    public CarConfigSO Character => _character;

    void Awake()
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

    public void Initialize(CarConfigSO character, int index, Action<int> onClick)
    {
        _character = character;
        _index = index;
        _onClick = onClick;

        if (_icon != null)
        {
            _icon.sprite = character != null ? character.Preview : null;
            _icon.enabled = _icon.sprite != null;
           // _icon.preserveAspect = true;
        }

        if (_nameText != null)
        {
            _nameText.text = character != null ? character.CarName : string.Empty;
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
}
