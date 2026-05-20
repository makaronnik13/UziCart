using Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Zenject;

public class ButtonEventSounds : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
   
    [SerializeField] protected UISound _hoverSound;
    [SerializeField] private UISound _clickSound;

    [Inject(Optional = true)] private SoundService _soundService;

    public void OnPointerClick(PointerEventData eventData)
    {
       if (_soundService == null) return;
       _soundService.PlayUiSoundEffect(_clickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_soundService == null) return;
        _soundService.PlayUiSoundEffect(_hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_soundService == null) return;
        _soundService.PlayUiSoundEffect(_hoverSound);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_soundService == null) return;
        _soundService.PlayUiSoundEffect(_clickSound);
    }
}
