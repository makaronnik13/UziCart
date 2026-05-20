using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Sound
{
    public class TempSfxRunner : MonoBehaviour
    {
        private AudioSource _src;
        public void Init(AudioSource src, AudioClip clip, PauseService pauseService)
        {
            Debug.Log("Temp sfx runner play " + clip.name + " on " + gameObject.name);
            _src = src;
            pauseService.IsPaused.Subscribe(isPaused =>
            {
                if (_src == null) return;
                if (isPaused) _src.Pause();
                else _src.UnPause();
            }).AddTo(this);

            /*
            sceneLoadingService.OnPreload.Subscribe(_ =>
            {
                if (_src == null) return;
                _src.Stop();
                Destroy(gameObject);
            }).AddTo(this);
            */
            
            _src.PlayOneShot(clip);
            Destroy(gameObject, clip.length );
        }

    }
}
