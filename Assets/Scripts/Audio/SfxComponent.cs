using UnityEngine;
using System;
using Zenject;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Sound
{
    [DefaultExecutionOrder(102)]
    public class SfxComponent : MonoBehaviour
    {
        [SerializeField] protected List<SoundContainer> sounds;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] protected bool _isUiSFX;

        private SoundContainer _container;
        private PauseService _pauseService;


        [Inject]
        public void Construct(PauseService pauseService)
        {
            _pauseService = pauseService;

            if (audioSource == null)
            {
                Debug.LogError($"No audio source on sfx {gameObject.name} {gameObject.transform.parent?.name}");
                return;
            }

            _pauseService.IsPaused.Subscribe(isPaused =>
            {
                if (_isUiSFX) return;
                if (isPaused) audioSource.Pause();
                else audioSource.UnPause();
            }).AddTo(this);
            
            /*
            _sceneLoadingService.OnPreload.Subscribe(_ =>
            {
                if (_isUiSFX) return;
                audioSource.Stop();
            }).AddTo(this);
            */
        }

        public void PlaySoundEffectWithNewInstance(string soundName)
        {
            PlaySoundEffect(soundName, true);
        }
        
        public void PlaySoundEffect(string soundName)
        {
            PlaySoundEffect(soundName, false);
        }

        public void PlaySoundEffect(string soundName, bool createNewInstance = false)
        {
            if (sounds == null || sounds.Count == 0)
            {
                Debug.LogWarning($"No sound clips list on {gameObject.name}");
                return;
            }

            _container = GetContainer(soundName);
            if (_container == null) return;

            var sourceToClone = _container.OverridenAudioSource ? _container.OverridenAudioSource : audioSource;
            var clip = _container.GetSound();
            if (clip == null || sourceToClone == null) return;

            if (!createNewInstance)
            {
                sourceToClone.PlayOneShot(clip);
                return;
            }

            var clone = Instantiate(sourceToClone.gameObject);
            clone.name = $"TempSfxSound_{soundName}";
            clone.transform.parent = null;
            clone.transform.position = sourceToClone.transform.position;
            
            foreach (var c in clone.GetComponents<Component>())
            {
                if (!(c is AudioSource) && !(c is Transform))
                    Destroy(c);
            }

            var tempSrc = clone.GetComponent<AudioSource>();
            var runner = clone.AddComponent<TempSfxRunner>();
            runner.Init(tempSrc, clip, _pauseService);
        }

        protected SoundContainer GetContainer(string soundName)
        {
            _container = sounds.FirstOrDefault(c => c.SoundName == soundName);
            if (_container == null)
            {
                Debug.LogWarning($"No cound container found for {soundName} on {gameObject.name}");
                return null;
            }
            return _container;
        }

        protected void SetSfxFunction(string sfxName, Func<AudioClip> getClipFunc)
        {
            SoundContainer container = sounds.FirstOrDefault(s => s.SoundName == sfxName);
            try
            {
                container.SetFunction(getClipFunc);
            }
            catch
            {
                Debug.LogError($"Wrong sfxName {sfxName}! No sound container found in {gameObject.name}");
            }
        }

        private static void CopyAudioSource(AudioSource from, AudioSource to)
        {
            to.outputAudioMixerGroup = from.outputAudioMixerGroup;
            to.volume = from.volume;
            to.pitch = from.pitch;
            to.panStereo = from.panStereo;
            to.spatialBlend = from.spatialBlend;
            to.dopplerLevel = from.dopplerLevel;
            to.spread = from.spread;
            to.priority = from.priority;
            to.reverbZoneMix = from.reverbZoneMix;
            to.rolloffMode = from.rolloffMode;
            to.minDistance = from.minDistance;
            to.maxDistance = from.maxDistance;
            to.bypassEffects = from.bypassEffects;
            to.bypassListenerEffects = from.bypassListenerEffects;
            to.bypassReverbZones = from.bypassReverbZones;
            to.playOnAwake = false;
            to.loop = false;
        }
    }
}
