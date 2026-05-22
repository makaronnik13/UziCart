using UnityEngine;
using Zenject;

namespace Sound
{
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] MusicAsset _music;
        [SerializeField] bool _instant;

        [Inject(Optional = true)] SoundService _soundService;

        void Start()
        {
            if (_music == null)
            {
                Debug.LogError($"{nameof(SceneMusic)} has no music asset.", this);
                return;
            }

            if (_soundService == null)
            {
                _soundService = FindFirstObjectByType<SoundService>();
            }

            if (_soundService == null)
            {
                Debug.LogError($"{nameof(SceneMusic)} has no {nameof(SoundService)}.", this);
                return;
            }

            _soundService.PlayMusic(_music, _instant);
        }
    }
}
