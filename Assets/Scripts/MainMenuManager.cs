using Ommy.Audio;
using Ommy.Singleton;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : Singleton<MainMenuManager>
{

    [Header("Panels")]
    public GameObject settingPanel;
    public GameObject quitPanel;

    [Header("Settings")]
    public Slider slider;
    public AudioSettingsController audioSettings;
    public GameObject restoreButton;
    public TMP_Text versionText;

    private void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
            restoreButton.SetActive(false);

        slider.value = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
        audioSettings.LoadSavedSettings();

        if (versionText != null)
            versionText.text = "v" + Application.version;

        AudioManager.Instance.StartGame();

        ArcadiaSdkManager.CurrentAdPlacement = "main_menu_banner";
        ArcadiaSdkManager.Agent.ShowBanner();
        AA_AnalyticsManager.Agent.TrackScreenView("main_menu");
    }

    #region Navigation

    public void PlayBtn()
    {
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackButtonClick("play");
        SceneManager.LoadScene("LevelSelection");
    }

    public void SettingBtn()
    {
        settingPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackButtonClick("settings");
    }

    public void BackBtn()
    {
        settingPanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
    }

    public void QuitBtn()
    {
        quitPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
    }

    public void QuitBtnYes()
    {
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackSessionEnd("quit_button", "MainMenu");
        Application.Quit();
    }

    public void QuitBtnNo()
    {
        quitPanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
    }

    #endregion

    #region Settings Controls

    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat(PrefKeys.MouseSensitivity, slider.value);
    }

    public void MusicBtn() => audioSettings.ToggleMusic();
    public void SoundBtn() => audioSettings.ToggleSound();

    #endregion

    #region Links

    public void MoreGames()
    {
#if UNITY_ANDROID
        Application.OpenURL("https://play.google.com/store/apps/developer?id=Arcadia+Adventures");
#elif UNITY_IOS
        Application.OpenURL("https://apps.apple.com/us/developer/muhammad-umar-shafaqat/id1671095846");
#endif
    }

    public void RateUs()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
        Application.OpenURL("itms-apps://itunes.apple.com/app/" + "1672844290");
#endif
    }

    public void PrivacyPolicy()
    {
#if UNITY_ANDROID
        Application.OpenURL("https://sites.google.com/view/arcadia-adventures/home");
#elif UNITY_IOS
        Application.OpenURL("https://sites.google.com/view/ommygames/home");
#endif
    }

    #endregion
}
