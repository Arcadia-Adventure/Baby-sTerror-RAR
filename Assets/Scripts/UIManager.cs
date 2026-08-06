using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ControlFreak2;
using ControlFreak2.UI;
using DG.Tweening;
using Ommy.Prefs;
using Ommy.Audio;
using Ommy.Singleton;

public class UIManager : Singleton<UIManager>
{
    [Header("Level Complete")]
    public GameObject nextButton;
    public GameObject rateusButton;
    public GameObject levelCompletePanel;

    [Header("Pause")]
    public GameObject pausePanel;

    [Header("Crosshair")]
    public Sprite knobImage;
    public Sprite doorOpenImage;
    public Sprite doorCloseImage;
    public Sprite pickImage;
    public Sprite dropImage;
    public Image crossHairDetection;
    public TextMeshProUGUI detectionTxt;
    public RectTransform rt;

    [Header("Touch Buttons")]
    public TouchButtonSpriteAnimator door;
    public TouchButtonSpriteAnimator pick;
    public TouchButtonSpriteAnimator useDevice;

    [Header("Settings")]
    public FirstPersonController fps;
    public Slider sl;

    CrosshairState currentCrosshairState = CrosshairState.None;

    private void Start()
    {
        Time.timeScale = 1f;
        sl.value = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
        fps.mouseSensitivity = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
        crossHairDetection.sprite = knobImage;
        detectionTxt.text = null;
    }

    #region Crosshair

    public void SetCrosshair(CrosshairState state, string detectionText)
    {
        detectionTxt.text = detectionText;
        if (currentCrosshairState == state) return;
        currentCrosshairState = state;
        crossHairDetection.DOKill();

        switch (state)
        {
            case CrosshairState.None:
                rt.sizeDelta = new Vector2(20, 20);
                crossHairDetection.sprite = knobImage;
                detectionTxt.text = null;
                break;
            case CrosshairState.Pick:
                SetCrosshairSprite(pickImage);
                break;
            case CrosshairState.Drop:
                SetCrosshairSprite(dropImage);
                break;
            case CrosshairState.DoorOpen:
                SetCrosshairSprite(doorOpenImage);
                break;
            case CrosshairState.DoorClose:
                SetCrosshairSprite(doorCloseImage);
                break;
        }
    }

    void SetCrosshairSprite(Sprite sprite)
    {
        rt.sizeDelta = new Vector2(50, 50);
        crossHairDetection.sprite = sprite;
        crossHairDetection.DOFade(1, 1);
    }

    #endregion

    #region Button Visibility

    public void SetDoorButtonVisible(bool visible) => SetButtonVisible(door, visible);
    public void SetPickButtonVisible(bool visible) => SetButtonVisible(pick, visible);
    public void SetUseDeviceButtonVisible(bool visible) => SetButtonVisible(useDevice, visible);

    void SetButtonVisible(TouchButtonSpriteAnimator button, bool visible)
    {
        var img = button.image;
        float targetAlpha = visible ? 1f : 0f;
        button.transform.parent.GetComponent<TouchButton>().enabled = visible;

        if (Mathf.Approximately(img.color.a, targetAlpha)) return;

        img.DOKill();
        img.DOFade(targetAlpha, 0.3f);
    }

    #endregion

    #region Game UI Actions

    public void LevelComplete()
    {
        levelCompletePanel.SetActive(true);
        bool isLastLevel = GamePreference.selectedLevel >= LevelConfigLoader.LevelCount;
        nextButton.SetActive(!isLastLevel);
        rateusButton.SetActive(isLastLevel);
    }

    public void RateUsClick()
    {
        Application.OpenURL("market://details?id=" + Application.identifier);
        AA_AnalyticsManager.Agent.TrackButtonClick("rate_us");
    }

    #endregion

    #region Pause & Navigation

    public void DoorOpenCloseBtn()
    {
        PickDropController.Instance.DoorOpenCloseBtn();
    }

    public void PauseBtn()
    {
        Time.timeScale = 0;
        pausePanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.PauseAll();
        AudioManager.Instance.SetBGSetting(true);
        AudioManager.Instance.StartGame();
        AA_AnalyticsManager.Agent.TrackButtonClick("pause");
        ArcadiaSdkManager.CurrentAdPlacement = "pause_interstitial";
        ArcadiaSdkManager.Agent.ShowInterstitialAd();
    }

    public void ResumeBtn()
    {
        Time.timeScale = 1;
        pausePanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.ResumeAll();
        AudioManager.Instance.GameEnd();
        AudioManager.Instance.SetBGSetting(false);
        AA_AnalyticsManager.Agent.TrackButtonClick("resume");
        ArcadiaSdkManager.Agent.ShowBanner();
    }

    public void HomeBtn()
    {
        Time.timeScale = 1;
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "home_button");
        SceneManager.LoadScene("MainMenu");
    }

    public void ReplayBtn()
    {
        Time.timeScale = 1;
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackLevelRetry(GamePreference.selectedLevel);
        ArcadiaSdkManager.CurrentAdPlacement = "replay_rewarded";
        if (!ArcadiaSdkManager.Agent.removeAds) ArcadiaSdkManager.Agent.ShowRewardedAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextBtn()
    {
        Time.timeScale = 1;
        GamePreference.selectedLevel++;
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackButtonClick("next_level");
        ArcadiaSdkManager.CurrentAdPlacement = "next_rewarded";
        if (!ArcadiaSdkManager.Agent.removeAds) ArcadiaSdkManager.Agent.ShowRewardedAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat(PrefKeys.MouseSensitivity, sl.value);
        fps.mouseSensitivity = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
    }

    #endregion

    #region Lifecycle

    private void OnDisable()
    {
        DOTween.Kill(crossHairDetection);
        if (door != null) DOTween.Kill(door.image);
        if (pick != null) DOTween.Kill(pick.image);
        if (useDevice != null) DOTween.Kill(useDevice.image);
    }

    private void OnApplicationQuit()
    {
        AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "app_quit");
        AA_AnalyticsManager.Agent.TrackSessionEnd("app_quit", AnalyticsTracker.GetCurrentScene());
    }

    #endregion
}
