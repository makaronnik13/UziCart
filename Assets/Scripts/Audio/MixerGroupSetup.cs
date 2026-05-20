
using System;
using UnityEngine.Audio;

namespace  Sound
{
    [Serializable]
    public class MixerGroupSetup
    {
        public AudioMixerGroup Mixer;
        public string ExposedParameterName;
        public float DefaultValue;
    }
    
}

