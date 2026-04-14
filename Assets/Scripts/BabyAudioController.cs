using System;
using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;

public class BabyAudioController : MonoBehaviour
{
    static readonly HashSet<BabyAnimationType> LoopingAnimations = new()
    {
        BabyAnimationType.CrySit,
        BabyAnimationType.CryLay,
        BabyAnimationType.CryStand,
        BabyAnimationType.AngrySit
    };

    [Serializable]
    public class AudioEntry
    {
        public BabyAnimationType animationType;
        public AudioClip clip;
    }

    [SerializeField] private MyAudioSource audioSource;
    [SerializeField] private List<AudioEntry> audioEntries = new();

    Dictionary<BabyAnimationType, AudioClip> _clipLookup;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<MyAudioSource>();

        _clipLookup = new Dictionary<BabyAnimationType, AudioClip>();
        foreach (var entry in audioEntries)
            _clipLookup[entry.animationType] = entry.clip;
    }

    public void Play(BabyAnimationType audioType)
    {
        if (!_clipLookup.TryGetValue(audioType, out var clip) || clip == null)
            return;

        audioSource.Stop();
        audioSource.loop = LoopingAnimations.Contains(audioType);
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.loop = false;
        audioSource.Stop();
    }

    public bool IsPlaying => audioSource.isPlaying;
}
