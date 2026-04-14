using Ommy.Audio;
using UnityEngine;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Music UI")]
    public GameObject musicOn;
    public GameObject musicOff;

    [Header("Sound UI")]
    public GameObject soundOn;
    public GameObject soundOff;

    bool music;
    bool sound;

    public void LoadSavedSettings()
    {
        ApplyMusic(PlayerPrefs.GetInt(PrefKeys.Music) == 1);
        ApplySound(PlayerPrefs.GetInt(PrefKeys.Sound) == 1);
    }

    public void ToggleMusic()
    {
        ApplyMusic(!music);
        PlayerPrefs.SetInt(PrefKeys.Music, music ? 1 : 0);
        AA_AnalyticsManager.Agent.TrackButtonClick("music_toggle");
    }

    public void ToggleSound()
    {
        ApplySound(!sound);
        PlayerPrefs.SetInt(PrefKeys.Sound, sound ? 1 : 0);
        if (sound) AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackButtonClick("sound_toggle");
    }

    void ApplyMusic(bool enabled)
    {
        music = enabled;
        AudioManager.Instance.SetBGSetting(enabled);
        musicOn.SetActive(enabled);
        musicOff.SetActive(!enabled);
    }

    void ApplySound(bool enabled)
    {
        sound = enabled;
        AudioManager.Instance.SetSFXSetting(enabled);
        AudioManager.Instance.MuteCategory(AudioCategory.Ambient, !enabled);
        AudioManager.Instance.MuteCategory(AudioCategory.Voice, !enabled);
        AudioManager.Instance.MuteCategory(AudioCategory.SFX, !enabled);
        soundOn.SetActive(enabled);
        soundOff.SetActive(!enabled);
    }
}
