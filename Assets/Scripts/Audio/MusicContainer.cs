using System;
using UnityEngine;
using Zenject;

namespace Sound
{
    public class MusicContainer : MonoBehaviour
    {
        [SerializeField]
        private bool _overrideMusic = true;

        [SerializeField]
        private MusicAsset _music;

        [SerializeField]
        private bool _activateOnStart = false;

        private SoundService _soundService;
        
        [Inject]
        public void Construct(SoundService soundService)
        {
            _soundService = soundService;
        }
        

        [ContextMenu("Trigger")]
        public void Trigger()
        {
            if (_overrideMusic)
            {
                _soundService.PlayMusic(_music);
            }

        }

        public void StopMusic()
        {
            _soundService.StopMusic();
        }
        
        // Ambience support removed; music only.
    }
}
