using System;
using System.Collections.Generic;
using UnityEngine;
public class BabyAudioController : MonoBehaviour
{
    [Serializable]
    public class AudioEntry
    {
        public BabyAnimationType animationType;
        public AudioClip clip;
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioEntry> audioEntries = new();

    private Dictionary<BabyAnimationType, AudioClip> _clipLookup;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _clipLookup = new Dictionary<BabyAnimationType, AudioClip>();
        foreach (var entry in audioEntries)
            _clipLookup[entry.animationType] = entry.clip;
    }

    public void Play(BabyAnimationType animationType, bool loop = false)
    {
        if (!_clipLookup.TryGetValue(animationType, out var clip) || clip == null)
            return;

        audioSource.Stop();
        audioSource.loop = loop;

        if (loop)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }

    public void Stop()
    {
        audioSource.loop = false;
        audioSource.Stop();
    }

    public void Mute(bool mute) => audioSource.mute = mute;

    public bool IsPlaying => audioSource.isPlaying;
}
