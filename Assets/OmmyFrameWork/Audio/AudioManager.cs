//Ommy AudioManager
using UnityEngine;
using System.Collections.Generic;
using Ommy.SaveData;
namespace Ommy.Audio
{

    public enum SFX
    {
        Reward,
        Click,
        Upgrade, MachineUpgrade,
        Unlock,
        BeltUnlock,
        Spawn,
        EggDrop,
        WheatDrop,
        CornDrop,
        MilkDrop
    }//enum end

    [System.Serializable]
    public sealed class SFXClip
    {
        //===================================================
        // FIELDS
        //===================================================
        [SerializeField] SFX _sfx;
        [SerializeField] AudioClip _clip = null;

        // Constructor
        public SFXClip(SFX sfx) => _sfx = sfx;

        //===================================================
        // PROPERTIES
        //===================================================
        public SFX SFX => _sfx;
        public AudioClip Clip => _clip;

    }//struct end

    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;
        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }
        //===================================================
        // FIELDS
        //===================================================
        [SerializeField] AudioSource _bgSource = null;
        [SerializeField] AudioSource _sfxSource = null;
        [Space]
        [SerializeField] AudioClip _bgMusic = null;
        [Space]
        [SerializeField] List<SFXClip> _sfxClips = new List<SFXClip>();

        //===================================================
        // METHODS
        //===================================================
        internal void Init()
        {
            
            SetBGSetting(SaveData.SaveData.Instance.Music);
            SetSFXSetting(SaveData.SaveData.Instance.SFX);
        }//Start() end

        /// <summary>
        /// Method for creating Array of SFX Clips.
        /// </summary>
        private void Create()
        {
            int length = System.Enum.GetValues(typeof(SFX)).Length;

            if (_sfxClips == null || _sfxClips.Count == 0)
            {
                _sfxClips = new List<SFXClip>();
                for (int i = 0; i < length; i++)
                {
                    _sfxClips.Add(new SFXClip((SFX)i));
                }//loop end
            }//if end
            else
            {
                for (int i = 0; i < length - _sfxClips.Count; i++)
                    _sfxClips.Add(new SFXClip((SFX)0));

                for (int i = 0; i < length; i++)
                {
                    if (_sfxClips[i].SFX != (SFX)i)
                        _sfxClips[i] = new SFXClip((SFX)i);
                }//loop end
            }//else end

        }//Create() end

        /// <summary>
        /// Toggle Background Music Audio Source.
        /// </summary>
        public void SetBGSetting(bool Toggle) => _bgSource.mute = !Toggle;

        /// <summary>
        /// Toggle SFX Audio Source.
        /// </summary>
        public void SetSFXSetting(bool Toggle) => _sfxSource.mute = !Toggle;

        /// <summary>
        /// Call when Game Starts to play Background Music.
        /// </summary>
        public void StartGame()
        {
            if (_bgSource.isPlaying)
                return;

            _bgSource.clip = _bgMusic;
            _bgSource.loop = true;
            _bgSource.Play();
        }//StartGame() end

        /// <summary>
        /// Call when Game Starts to stop playing Background Music.
        /// </summary>
        public void GameEnd() => _bgSource.Stop();

        /// <summary>
        /// Call to play specific SFX clip against enum.
        /// </summary>
        public void PlaySFX(SFX sfx, float volume = 1f) =>
            _sfxSource.PlayOneShot(_sfxClips[(int)sfx].Clip, volume);

        /// <summary>
        /// Call to play custom Audio Clip.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f) =>
            _sfxSource.PlayOneShot(clip, volume);

    }//class end
}