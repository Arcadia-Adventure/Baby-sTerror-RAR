using UnityEngine;
using System;
using System.Collections;

namespace Ommy.Audio
{
    public enum AudioCategory { BGM, Ambient, Voice, SFX }

    /// <summary>
    /// Drop-in AudioSource wrapper. Handles auto-registration with AudioManager,
    /// category-based mute/volume, per-source state tracking for pause/resume,
    /// and utility playback methods (delay, repeat, fade).
    /// 
    /// Scene setup — add this component to:
    ///   Rain              → Ambient
    ///   BabyCryingCradle  → Voice
    ///   Baby AudioSource  → Voice
    ///   DoorBell          → Ambient
    ///   DoorKnocking      → Ambient
    ///   Fire L8           → Ambient
    ///   DoorLock          → Ambient
    ///   Footsteps         → SFX
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MyAudioSource : MonoBehaviour
    {
        [Tooltip("BGM = menu music, Ambient = rain/doorbell/fire, Voice = baby, SFX = footsteps/effects")]
        public AudioCategory category;

        AudioSource _source;
        float _baseVolume;
        float _categoryVolumeMultiplier = 1f;
        bool _mutedBeforePause;
        bool _categoryMuted;
        Coroutine _activeRoutine;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _baseVolume = _source.volume;
        }

        void OnEnable()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Register(this);
        }

        void OnDisable()
        {
            KillRoutine();
            if (AudioManager.Instance != null)
                AudioManager.Instance.Unregister(this);
        }

        // ============================
        // Pass-through API
        // ============================

        public AudioClip clip
        {
            get => _source.clip;
            set => _source.clip = value;
        }

        public bool loop
        {
            get => _source.loop;
            set => _source.loop = value;
        }

        public bool isPlaying => _source.isPlaying;

        public float pitch
        {
            get => _source.pitch;
            set => _source.pitch = value;
        }

        public float volume
        {
            get => _baseVolume;
            set
            {
                _baseVolume = value;
                _source.volume = value * _categoryVolumeMultiplier;
            }
        }

        public bool mute
        {
            get => _source.mute;
            set => _source.mute = value;
        }

        public void Play() => _source.Play();
        public void Stop()
        {
            KillRoutine();
            _source.Stop();
        }

        public void PlayOneShot(AudioClip c, float vol = 1f) => _source.PlayOneShot(c, vol);

        // ============================
        // Utility Playback
        // ============================

        /// <summary>Play after a delay (seconds).</summary>
        public void PlayDelayed(float delay)
        {
            KillRoutine();
            _activeRoutine = StartCoroutine(DelayedRoutine(delay, () => _source.Play()));
        }

        /// <summary>Play repeatedly with an interval. Optional intervalGrowth adds to the interval each cycle.</summary>
        public void PlayRepeating(float interval, float intervalGrowth = 0f)
        {
            KillRoutine();
            _activeRoutine = StartCoroutine(RepeatingRoutine(interval, intervalGrowth));
        }

        /// <summary>Play repeatedly starting after an initial delay.</summary>
        public void PlayRepeating(float initialDelay, float interval, float intervalGrowth)
        {
            KillRoutine();
            _activeRoutine = StartCoroutine(DelayedRoutine(initialDelay, () =>
            {
                _source.Play();
                _activeRoutine = StartCoroutine(RepeatingRoutine(interval, intervalGrowth));
            }));
        }

        /// <summary>Fade volume from current to target over duration.</summary>
        public void FadeTo(float targetVolume, float duration, Action onComplete = null)
        {
            KillRoutine();
            _activeRoutine = StartCoroutine(FadeRoutine(targetVolume, duration, onComplete));
        }

        /// <summary>Fade out then stop.</summary>
        public void FadeOutAndStop(float duration)
        {
            FadeTo(0f, duration, () =>
            {
                _source.Stop();
                volume = _baseVolume;
            });
        }

        // ============================
        // Coroutine Internals
        // ============================

        IEnumerator DelayedRoutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        IEnumerator RepeatingRoutine(float interval, float intervalGrowth)
        {
            float currentInterval = interval;
            while (true)
            {
                yield return new WaitForSeconds(currentInterval);
                _source.Play();
                currentInterval += intervalGrowth;
            }
        }

        IEnumerator FadeRoutine(float targetVolume, float duration, Action onComplete)
        {
            float startVolume = _baseVolume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }
            volume = targetVolume;
            onComplete?.Invoke();
        }

        void KillRoutine()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        // ============================
        // AudioManager hooks
        // ============================

        public void OnGamePause()
        {
            _mutedBeforePause = _source.mute;
            _source.mute = true;
        }

        public void OnGameResume()
        {
            _source.mute = _mutedBeforePause;
        }

        public void SetCategoryMute(bool m)
        {
            _categoryMuted = m;
            _source.mute = m;
        }

        public void SetCategoryVolume(float vol)
        {
            _categoryVolumeMultiplier = vol;
            _source.volume = _baseVolume * vol;
        }
    }
}
