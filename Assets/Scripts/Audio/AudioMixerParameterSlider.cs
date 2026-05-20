using UnityEngine;
using UnityEngine.Audio;

namespace Sound
{
    public class AudioMixerParameterSlider : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _mixerGroup;
        void Start()
        {
            if (PlayerPrefs.HasKey(_mixerGroup.name))
            {
                //_slider.value = PlayerPrefs.GetFloat(_mixerGroup.name); 
                //_mixerGroup.audioMixer.SetFloat(_mixerGroup.name+"Volume", SoundService.ValueToDb(_slider.value));
            }

            /*
            _slider.OnValueChangedAsObservable().Subscribe(v =>
            {
                _mixerGroup.audioMixer.SetFloat(_mixerGroup.name+"Volume", SoundService.ValueToDb(v));
                PlayerPrefs.SetFloat(_mixerGroup.name, v);
            }).AddTo(this);
            */

        }
     
        [ContextMenu("Clear Sound Prefs")]
        private void ClearSoundPrefs()
        {
            PlayerPrefs.DeleteKey(_mixerGroup.name);
        }
    }
}
