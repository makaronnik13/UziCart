using System.Collections.Generic;
using UnityEngine;

namespace Sound
{
    public class HullSound : MonoBehaviour
    {
        [System.Serializable]
        public class SpeedClipGroup
        {
            [Min(0f)] public float speed;
            public List<AudioClip> clips = new List<AudioClip>();
        }

        [SerializeField] Collider _hullCollider;
        [SerializeField] Rigidbody _carRigidbody;
        [SerializeField] AudioSource _audioSource;
        [SerializeField] List<SpeedClipGroup> _speedClips = new List<SpeedClipGroup>();
        [SerializeField, Min(0f)] float _minSpeedToPlay = 0.1f;

        void Reset()
        {
            _hullCollider = GetComponent<Collider>();
            _carRigidbody = GetComponentInParent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
        }

        void Awake()
        {
            if (_carRigidbody == null)
            {
                _carRigidbody = GetComponentInParent<Rigidbody>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsHullCollision(collision))
            {
                return;
            }

            float speed = GetImpactSpeed(collision);
            if (speed < _minSpeedToPlay)
            {
                return;
            }

            AudioClip clip = GetRandomClip(speed);
            if (clip == null || _audioSource == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip);
        }

        bool IsHullCollision(Collision collision)
        {
            if (_hullCollider == null)
            {
                return true;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (contact.thisCollider == _hullCollider)
                {
                    return true;
                }
            }

            return collision.collider == _hullCollider;
        }

        float GetImpactSpeed(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 0f)
            {
                return collision.relativeVelocity.magnitude;
            }

            return _carRigidbody != null ? _carRigidbody.linearVelocity.magnitude : 0f;
        }

        AudioClip GetRandomClip(float speed)
        {
            SpeedClipGroup group = GetClosestGroup(speed);
            if (group == null || group.clips == null || group.clips.Count == 0)
            {
                return null;
            }

            List<AudioClip> clips = group.clips;
            for (int attempts = 0; attempts < clips.Count; attempts++)
            {
                AudioClip clip = clips[Random.Range(0, clips.Count)];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        SpeedClipGroup GetClosestGroup(float speed)
        {
            SpeedClipGroup bestGroup = null;
            float bestDifference = float.MaxValue;
            for (int i = 0; i < _speedClips.Count; i++)
            {
                SpeedClipGroup group = _speedClips[i];
                if (group == null || group.clips == null || group.clips.Count == 0)
                {
                    continue;
                }

                float difference = Mathf.Abs(speed - group.speed);
                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    bestGroup = group;
                }
            }

            return bestGroup;
        }
    }
}
