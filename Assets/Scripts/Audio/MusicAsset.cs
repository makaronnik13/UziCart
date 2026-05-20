using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Sound
{
    [CreateAssetMenu(menuName = "Sound/MusicAsset")]
    public class MusicAsset : ScriptableObject
    {
        public AudioClip Clip;
        public AnimationCurve InCurve;
        public AnimationCurve OutCurve;
        public bool ContinueFromTheBegining = false;
        public AudioMixerGroup OverridenMixer;
    }
}
