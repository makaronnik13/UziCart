using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace Sound
{
    public class SoundService : MonoBehaviour
    {
        [SerializeField] private AudioSource musicAudioSource;
        [SerializeField] private MusicAsset _offMusic;
        [SerializeField] private SfxComponent _uiSfxComponent;
        [SerializeField] private List<MixerGroupSetup> volumesParameters;
        [SerializeField, Min(0f)] private float _debounceSeconds = 0.3f;
        
        
        private CompositeDisposable _musicDisp = new CompositeDisposable();

        private AudioSource _musicAux;
        private bool _musicUsingA = true;
        private PauseService _pauseService;

        private int _musicSeq;

        private IDisposable _musicDebounce;

        private AudioMixerGroup _defaultMusicMixer;
        
        private MusicAsset _pausedTrack;
        private List<MusicAsset> _activatedBackgroundMusicAssets = new List<MusicAsset>();
        private readonly Dictionary<MusicAsset, float> _resumePosMusic = new Dictionary<MusicAsset, float>();

        [Inject]
        public void Construct(PauseService pauseService)
        {
            
            _pauseService = pauseService;

            _defaultMusicMixer = musicAudioSource.outputAudioMixerGroup;
            
        }

        private void Awake()
        {
            if (_musicAux == null && musicAudioSource != null)
            {
                _musicAux = Instantiate(musicAudioSource, musicAudioSource.transform.parent);
                _musicAux.name = musicAudioSource.name + "_Aux";
                _musicAux.playOnAwake = false;
                _musicAux.volume = 0f;
                _musicAux.Stop();
            }
            
            
            _pauseService.IsPaused.Subscribe(isPaused => { PerformPause(isPaused); }).AddTo(this);
            
            /*
            _sceneLoadingService.InGame.Subscribe(inGame =>
            {
                if (!inGame)
                {
                    PlayMusic(_offMusic, true);
                    // Ambience support removed; keep music only.
                }
            }).AddTo(this);
            */
        }

        private void Start()
        {
            SetVolumeParameters();
        }

        private void SetVolumeParameters()
        {
            foreach (var groupSetup in volumesParameters)
            {
                if (groupSetup == null || groupSetup.Mixer == null)
                {
                    continue;
                }

                float v = PlayerPrefs.HasKey(groupSetup.Mixer.name)
                    ? PlayerPrefs.GetFloat(groupSetup.Mixer.name)
                    : groupSetup.DefaultValue;
                bool muted = PlayerPrefs.GetInt(groupSetup.Mixer.name + "Muted", 0) == 1;
                ApplyMixerVolume(groupSetup, v, muted);
            }
        }

        private void PerformPause(bool isPaused)
        {
            /*
            if (isPaused)
            {
                _pausedTrack = GetActiveAssetForSource(GetActiveMusicSource());
                PlayMusic(_offMusic, true);
            }
            else
            {
                PlayMusic(_offMusic, true);
                if (_pausedTrack != null) PlayMusic(_pausedTrack, true);
            }
            */
        }

        private MusicAsset GetActiveAssetForSource(AudioSource src)
        {
            if (src == null || src.clip == null) return null;
            return _activatedBackgroundMusicAssets.FirstOrDefault(a => a != null && a.Clip == src.clip);
        }

        private AudioSource GetActiveMusicSource()
        {
            var a = musicAudioSource;
            var b = _musicAux;
            if (a != null && b != null)
            {
                if (a.isPlaying && b.isPlaying) return a.volume >= b.volume ? a : b;
                if (a.isPlaying) return a;
                if (b.isPlaying) return b;
            }
            return _musicUsingA ? a : b;
        }
        

        public void PlayUiSoundEffect(UISound sound)
        {
            if (sound == UISound.None)
            {
                return;
            }

            _uiSfxComponent.PlaySoundEffect(sound.ToString());
        }

        public void StopMusic()
        {
            PlayMusic(_offMusic, true);
        }

        public float GetMusicVolume() => GetVolumeByName("Music");
        public float GetSfxVolume() => GetVolumeByName("Sfx", "SFX", "Sound");
        public bool IsMusicMuted() => IsMutedByName("Music");
        public bool IsSfxMuted() => IsMutedByName("Sfx", "SFX", "Sound");

        public void SetMusicVolume(float value) => SetVolumeByName(value, IsMusicMuted(), "Music");
        public void SetSfxVolume(float value) => SetVolumeByName(value, IsSfxMuted(), "Sfx", "SFX", "Sound");
        public void SetMusicMuted(bool muted) => SetVolumeByName(GetMusicVolume(), muted, "Music");
        public void SetSfxMuted(bool muted) => SetVolumeByName(GetSfxVolume(), muted, "Sfx", "SFX", "Sound");

        public void PlayMusic(MusicAsset music, bool instant = false)
        {
         
            if (music == null) music = _offMusic;
            if (!_activatedBackgroundMusicAssets.Contains(music)) _activatedBackgroundMusicAssets.Add(music);
            
            if (instant)
            {
                CancelMusicDebounce();
                CommitMusic(music, true);
                return;
            }

            CancelMusicDebounce();
            _musicDebounce = Observable
                .Timer(TimeSpan.FromSeconds(Mathf.Max(0f, _debounceSeconds)), Scheduler.MainThreadIgnoreTimeScale)
                .Subscribe(_ =>
                {
                    _musicDebounce = null;
                    CommitMusic(music, false);
                });
        }

        void CancelMusicDebounce()
        {
            _musicDebounce?.Dispose();
            _musicDebounce = null;
        }

        private void CommitMusic(MusicAsset asset, bool instant)
        {
            if (AlreadyPlayingSame(asset, musicAudioSource, _musicAux)) return;

            _musicDisp.Clear();
            _musicSeq++;
            var seq = _musicSeq;
            var a = musicAudioSource;
            var b = _musicAux;
            var from = _musicUsingA ? a : b;
            var to = _musicUsingA ? b : a;
            CrossfadeChannel(from, to, asset, instant, _musicDisp, seq, () => { _musicUsingA = !_musicUsingA; });
        }
        
        private bool AlreadyPlayingSame(MusicAsset asset, AudioSource a, AudioSource b)
        {
            if (asset == null || asset.Clip == null) return false;
            if (a != null && a.clip == asset.Clip && a.isPlaying && a.time > 0f) return true;
            if (b != null && b.clip == asset.Clip && b.isPlaying && b.time > 0f) return true;
            return false;
        }

        private void CrossfadeChannel(AudioSource from, AudioSource to, MusicAsset asset, bool instant, CompositeDisposable disp, int seq, Action onSwitched)
        {
            var prevAsset = FindAssetByClip(from != null ? from.clip : null);
            var outCurve = prevAsset != null && prevAsset.OutCurve != null && prevAsset.OutCurve.keys.Length > 0 ? prevAsset.OutCurve : asset.OutCurve;
            float inDur = instant ? 0f : (asset.InCurve != null && asset.InCurve.keys.Length > 0 ? asset.InCurve.keys[asset.InCurve.keys.Length - 1].time : 0f);
            float outDur = instant ? 0f : (outCurve != null && outCurve.keys.Length > 0 ? outCurve.keys[outCurve.keys.Length - 1].time : 0f);

            if (asset.Clip != null)
            {
                to.outputAudioMixerGroup = asset.OverridenMixer != null
                    ? asset.OverridenMixer
                    : _defaultMusicMixer;
                
                if (to.clip != asset.Clip) to.clip = asset.Clip;

                if (asset.ContinueFromTheBegining)
                {
                    float resume = GetSavedPos(asset);
                    if (resume > 0f) to.time = Mathf.Clamp(resume, 0f, asset.Clip.length - 0.01f);
                }
                else
                {
                    to.time = 0f;
                }

                if (!to.isPlaying) to.Play();
                to.UnPause();

                if (inDur == 0f)
                {
                    to.volume = asset.InCurve != null && asset.InCurve.keys.Length > 0 ? asset.InCurve.Evaluate(asset.InCurve.keys[asset.InCurve.keys.Length - 1].time) : 1f;
                }
                else
                {
                    float t = 0f;
                    Observable.EveryUpdate()
                        .TakeWhile(_ => t <= inDur)
                        .Subscribe(_ =>
                        {
                            t += Time.unscaledDeltaTime;
                            to.volume = asset.InCurve.Evaluate(t);
                        },
                        _ =>
                        {
                            if (_musicSeq != seq) return;
                            to.volume = asset.InCurve.Evaluate(inDur);
                        }).AddTo(disp);
                }
            }

            if (from != null && from.isPlaying && from.clip != null && from != to)
            {
                if (outDur == 0f)
                {
                    SavePosTick(from);
                    from.volume = outCurve != null && outCurve.keys.Length > 0 ? outCurve.Evaluate(outCurve.keys[outCurve.keys.Length - 1].time) : 0f;
                    from.Pause();
                    onSwitched?.Invoke();
                }
                else
                {
                    float t2 = 0f;
                    Observable.EveryUpdate()
                        .TakeWhile(_ => t2 <= outDur)
                        .Subscribe(_ =>
                        {
                            t2 += Time.unscaledDeltaTime;
                            SavePosTick(from);
                            from.volume = outCurve.Evaluate(t2);
                        },
                        _ =>
                        {
                            if (_musicSeq != seq) return;
                            SavePosTick(from);
                            from.volume = outCurve.Evaluate(outDur);
                            from.Pause();
                            onSwitched?.Invoke();
                        }).AddTo(disp);
                }
            }
            else
            {
                onSwitched?.Invoke();
            }

            if (asset.Clip == null)
            {
                if (from != null && from.isPlaying)
                {
                    if (outDur == 0f)
                    {
                        SavePosTick(from);
                        from.Pause();
                    }
                    else
                    {
                        float t3 = 0f;
                        Observable.EveryUpdate()
                            .TakeWhile(_ => t3 <= outDur)
                            .Subscribe(_ =>
                            {
                                t3 += Time.unscaledDeltaTime;
                                SavePosTick(from);
                                from.volume = outCurve != null ? outCurve.Evaluate(t3) : 0f;
                            },
                            _ =>
                            {
                                if (_musicSeq != seq) return;
                                SavePosTick(from);
                                from.volume = outCurve != null && outCurve.keys.Length > 0 ? outCurve.Evaluate(outCurve.keys[outCurve.keys.Length - 1].time) : 0f;
                                from.Pause();
                            }).AddTo(disp);
                    }
                }
            }
        }

        private MusicAsset FindAssetByClip(AudioClip clip)
        {
            if (clip == null) return null;
            return _activatedBackgroundMusicAssets.FirstOrDefault(a => a != null && a.Clip == clip);
        }

        private float GetSavedPos(MusicAsset asset)
        {
            if (asset == null) return 0f;
            if (_resumePosMusic.TryGetValue(asset, out var t)) return t;
            return 0f;
        }

        private void SavePosTick(AudioSource src)
        {
            var a = FindAssetByClip(src != null ? src.clip : null);
            if (a == null) return;
            if (!a.ContinueFromTheBegining) return;
            float t = src.time;
            _resumePosMusic[a] = t;
        }

        public void OnDestroy()
        {
            CancelMusicDebounce();
            _musicDisp?.Dispose();
        }

        public static float ValueToDb(float v)
        {
            if (v == 0) return -80f;
            return Mathf.Log10(v) * 20f;
        }

        float GetVolumeByName(params string[] names)
        {
            MixerGroupSetup setup = FindMixerSetup(names);
            if (setup == null || setup.Mixer == null)
            {
                return 1f;
            }

            return PlayerPrefs.HasKey(setup.Mixer.name)
                ? PlayerPrefs.GetFloat(setup.Mixer.name)
                : setup.DefaultValue;
        }

        bool IsMutedByName(params string[] names)
        {
            MixerGroupSetup setup = FindMixerSetup(names);
            return setup != null && setup.Mixer != null && PlayerPrefs.GetInt(setup.Mixer.name + "Muted", 0) == 1;
        }

        void SetVolumeByName(float value, bool muted, params string[] names)
        {
            MixerGroupSetup setup = FindMixerSetup(names);
            if (setup == null || setup.Mixer == null)
            {
                Debug.LogWarning($"No mixer group found for {string.Join("/", names)} in SoundService.");
                return;
            }

            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(setup.Mixer.name, value);
            PlayerPrefs.SetInt(setup.Mixer.name + "Muted", muted ? 1 : 0);
            ApplyMixerVolume(setup, value, muted);
        }

        MixerGroupSetup FindMixerSetup(params string[] names)
        {
            if (volumesParameters == null)
            {
                return null;
            }

            return volumesParameters.FirstOrDefault(setup =>
                setup != null &&
                setup.Mixer != null &&
                names.Any(name => setup.Mixer.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        static void ApplyMixerVolume(MixerGroupSetup setup, float value, bool muted)
        {
            AudioMixerGroup mixerGroup = setup != null ? setup.Mixer : null;
            if (mixerGroup == null || mixerGroup.audioMixer == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(setup.ExposedParameterName))
            {
                Debug.LogWarning($"No exposed mixer parameter configured for mixer group '{mixerGroup.name}'.", mixerGroup);
                return;
            }

            if (!mixerGroup.audioMixer.SetFloat(setup.ExposedParameterName, muted ? -80f : ValueToDb(Mathf.Clamp01(value))))
            {
                Debug.LogWarning($"Exposed mixer parameter '{setup.ExposedParameterName}' was not found for mixer group '{mixerGroup.name}'.", mixerGroup);
            }
        }
    }
}
