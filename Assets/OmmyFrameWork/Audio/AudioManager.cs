using UnityEngine;
using System.Collections.Generic;

namespace Ommy.Audio
{
    public enum SFX
    {
        Click = 1,
        PickItem = 2,
        LevelComplete = 3,
        DoorBreak = 4,
        DropItem = 5,
        Scream = 6,
    }

    [System.Serializable]
    public sealed class SFXClip
    {
        [SerializeField] SFX _sfx;
        [SerializeField] AudioClip _clip = null;

        public SFXClip(SFX sfx) => _sfx = sfx;

        public SFX SFX => _sfx;
        public AudioClip Clip => _clip;
    }

    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Owned Sources (2D)")]
        [SerializeField] AudioSource _bgSource;
        [SerializeField] AudioSource _sfxSource;
        [Space]
        [SerializeField] AudioClip _bgMusic;
        [Space]
        [SerializeField] List<SFXClip> _sfxClips = new();

        Dictionary<SFX, AudioClip> _sfxLookup;

        // --- Registry ---
        readonly Dictionary<AudioCategory, HashSet<MyAudioSource>> _registry = new()
        {
            { AudioCategory.BGM, new HashSet<MyAudioSource>() },
            { AudioCategory.Ambient, new HashSet<MyAudioSource>() },
            { AudioCategory.Voice, new HashSet<MyAudioSource>() },
            { AudioCategory.SFX, new HashSet<MyAudioSource>() },
        };

        readonly Dictionary<AudioCategory, bool> _categoryMuted = new()
        {
            { AudioCategory.BGM, false },
            { AudioCategory.Ambient, false },
            { AudioCategory.Voice, false },
            { AudioCategory.SFX, false },
        };

        readonly Dictionary<AudioCategory, float> _categoryVolume = new()
        {
            { AudioCategory.BGM, 1f },
            { AudioCategory.Ambient, 1f },
            { AudioCategory.Voice, 1f },
            { AudioCategory.SFX, 1f },
        };

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildSFXLookup();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void BuildSFXLookup()
        {
            _sfxLookup = new Dictionary<SFX, AudioClip>(_sfxClips.Count);
            foreach (var entry in _sfxClips)
                _sfxLookup[entry.SFX] = entry.Clip;
        }

        // ============================
        // Registry
        // ============================

        public void Register(MyAudioSource src)
        {
            if (src == null) return;
            _registry[src.category].Add(src);
            src.SetCategoryMute(_categoryMuted[src.category]);
            src.SetCategoryVolume(_categoryVolume[src.category]);
            Debug.Log($"[AudioManager] Registered '{src.gameObject.name}' as {src.category}");
        }

        public void Unregister(MyAudioSource src)
        {
            if (src == null) return;
            _registry[src.category].Remove(src);
        }

        // ============================
        // Category Control
        // ============================

        public void MuteCategory(AudioCategory category, bool mute)
        {
            _categoryMuted[category] = mute;
            foreach (var src in _registry[category])
            {
                if (src != null) src.SetCategoryMute(mute);
            }
        }

        public void SetCategoryVolume(AudioCategory category, float vol)
        {
            _categoryVolume[category] = vol;
            foreach (var src in _registry[category])
            {
                if (src != null) src.SetCategoryVolume(vol);
            }
        }

        public float GetCategoryVolume(AudioCategory category) => _categoryVolume[category];

        /// <summary>
        /// Saves each source's mute state, then mutes all gameplay categories.
        /// </summary>
        public void PauseAll()
        {
            PauseCategory(AudioCategory.Ambient);
            PauseCategory(AudioCategory.Voice);
            PauseCategory(AudioCategory.SFX);
        }

        void PauseCategory(AudioCategory category)
        {
            foreach (var src in _registry[category])
            {
                if (src != null) src.OnGamePause();
            }
        }

        /// <summary>
        /// Restores each source's mute state from before the pause.
        /// </summary>
        public void ResumeAll()
        {
            ResumeCategory(AudioCategory.Ambient);
            ResumeCategory(AudioCategory.Voice);
            ResumeCategory(AudioCategory.SFX);
        }

        void ResumeCategory(AudioCategory category)
        {
            foreach (var src in _registry[category])
            {
                if (src != null) src.OnGameResume();
            }
        }

        // ============================
        // BGM (owned 2D source)
        // ============================

        public void SetBGSetting(bool enabled) => _bgSource.mute = !enabled;

        public void SetSFXSetting(bool enabled) => _sfxSource.mute = !enabled;

        public void StartGame()
        {
            if (_bgSource.isPlaying)
                return;

            _bgSource.clip = _bgMusic;
            _bgSource.loop = true;
            _bgSource.Play();
        }

        public void GameEnd() => _bgSource.Stop();

        // ============================
        // SFX (owned 2D source)
        // ============================

        public void PlaySFX(SFX sfx, float volume = 1f)
        {
            if (_sfxLookup != null && _sfxLookup.TryGetValue(sfx, out var clip))
                _sfxSource.PlayOneShot(clip, volume);
            else
                Debug.LogWarning($"[AudioManager] No clip assigned for SFX.{sfx}");
        }

        public void PlaySFX(AudioClip clip, float volume = 1f) =>
            _sfxSource.PlayOneShot(clip, volume);
    }
}
