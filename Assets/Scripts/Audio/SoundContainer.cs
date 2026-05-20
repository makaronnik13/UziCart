using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sound
{
    public enum SoundSelectionMode
    {
        Random,
        Custom
    }
    
    [Serializable]
    public class SoundContainer
    {
        [SerializeField]
        private string soundName;

        [SerializeField]
        private List<AudioClip> audioClips = new List<AudioClip>();

        [SerializeField]
        private SoundSelectionMode _mode = SoundSelectionMode.Random;

        [SerializeField]
        private AudioSource _overridenSource;
        
        
        private Queue<AudioClip> _clipQueue = new Queue<AudioClip>();
        
        public string SoundName => soundName;
        public AudioSource OverridenAudioSource => _overridenSource; 
            
        
        private Func<AudioClip> _clipFunction;
        
        public AudioClip GetSound()
        {
            try
            {
                if (_mode == SoundSelectionMode.Random && _clipQueue.Count == 0)
                {
                    _clipQueue = new Queue<AudioClip>(audioClips.OrderBy(c=> Guid.NewGuid()));
                }
                return _mode == SoundSelectionMode.Random ? _clipQueue.Dequeue() : _clipFunction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"No clip found! {e.Message}");
                return null;
            }
        }

        public void SetFunction(Func<AudioClip> getClipFunc)
        {
            _clipFunction = getClipFunc ?? throw new ArgumentNullException(nameof(getClipFunc));
            _mode = SoundSelectionMode.Custom;
        }
        
       public void SetRandom()
       {
           _clipFunction = null;
           _mode = SoundSelectionMode.Random;
       }
       
    }
}
