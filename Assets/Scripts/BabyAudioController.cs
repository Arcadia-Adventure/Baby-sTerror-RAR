using System;
using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;
public class BabyAudioController : MonoBehaviour
{
    [Serializable]
    public class AudioEntry
    {
        public BabyAnimationType animationType;
        public AudioClip clip;
    }

    [SerializeField] private MyAudioSource audioSource;
    [SerializeField] private List<AudioEntry> audioEntries = new();

    private Dictionary<BabyAnimationType, AudioClip> _clipLookup;

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
        audioSource.loop = audioType.ToString().Contains("Cry") || audioType.ToString().Contains("Angry");
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
