using System;
using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;

public class NannyAudioController : MonoBehaviour
{
    static readonly HashSet<NannySound> LoopingSounds = new()
    {
        NannySound.Breathing,
        NannySound.Chasing
    };

    [Serializable]
    public class AudioEntry
    {
        public NannySound sound;
        public AudioClip clip;
    }

    [Tooltip("Breathing, growls, screams and attacks. Keep this one spatial so the player can locate her.")]
    [SerializeField] private MyAudioSource voiceSource;
    [Tooltip("Looping footstep clip, played only while she is actually moving.")]
    [SerializeField] private MyAudioSource footStepSource;
    [SerializeField] private float[] footStepVolumeRange = { 0.8f, 1f };
    [SerializeField] private List<AudioEntry> audioEntries = new();
    [Tooltip("Played through the door she is pounding on. Empty falls back to the door-break SFX.")]
    [SerializeField] private AudioClip doorBangClip;

    public AudioClip DoorBangClip => doorBangClip;

    Dictionary<NannySound, AudioClip> _clipLookup;
    NannySound _currentLoop = NannySound.None;

    private void Awake()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<MyAudioSource>();

        _clipLookup = new Dictionary<NannySound, AudioClip>();
        foreach (var entry in audioEntries)
            _clipLookup[entry.sound] = entry.clip;
    }

    // In Start, because the sources are on child objects whose Awake may not have run yet.
    private void Start()
    {
        foreach (NannySound sound in Enum.GetValues(typeof(NannySound)))
        {
            if (sound != NannySound.None && (!_clipLookup.TryGetValue(sound, out var clip) || clip == null))
                Debug.LogError($"[NannyAudioController] No clip for {sound} in Audio Entries, so it never plays.", this);
        }

        if (footStepSource.clip == null)
            Debug.LogError("[NannyAudioController] The footstep source's AudioSource has no clip, so her footsteps are silent.", footStepSource);
    }

    public void Play(NannySound sound)
    {
        if (!_clipLookup.TryGetValue(sound, out var clip) || clip == null)
            return;

        // Re-triggering a loop that's already running would restart it every frame.
        bool isLooping = LoopingSounds.Contains(sound);
        if (isLooping && _currentLoop == sound && voiceSource.isPlaying)
            return;

        voiceSource.Stop();
        voiceSource.loop = isLooping;
        voiceSource.clip = clip;
        voiceSource.Play();
        _currentLoop = isLooping ? sound : NannySound.None;
    }

    public void Stop()
    {
        voiceSource.loop = false;
        voiceSource.Stop();
        _currentLoop = NannySound.None;
    }

    public void SetFootstepsActive(bool active)
    {
        if (!active)
        {
            footStepSource.Stop();
            return;
        }

        if (footStepSource.isPlaying)
            return;

        footStepSource.volume = UnityEngine.Random.Range(footStepVolumeRange[0], footStepVolumeRange[1]);
        footStepSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
        footStepSource.Play();
    }

    public bool IsPlaying => voiceSource.isPlaying;
}
