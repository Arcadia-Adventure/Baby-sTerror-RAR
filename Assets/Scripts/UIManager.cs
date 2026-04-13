using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using ControlFreak2.UI;
using ControlFreak2;
using DG.Tweening;
using Ommy.Prefs;
using Ommy.Audio;

public enum CrosshairState { None, Pick, Drop, DoorOpen, DoorClose }

public class UIManager : MonoBehaviour
{
    public GameObject nextButton, rateusButton;
    public GameObject levelCompletePanel;
    public GameObject pausePanel;

    // Crosshair sprites
    public Sprite knobImage;
    public Sprite doorOpenImage;
    public Sprite doorCloseImage;
    public Sprite pickImage;
    public Sprite dropImage;

    public TouchButtonSpriteAnimator door;
    public TouchButtonSpriteAnimator pick;
    public TouchButtonSpriteAnimator useDevice;

    public Image crossHairDetection;
    public TextMeshProUGUI detectionTxt;
    public RectTransform rt;
    public FirstPersonController fps;

    public static UIManager instance;

    private CrosshairState currentCrosshairState = CrosshairState.None;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public Slider sl;

    private void Start()
    {
        Time.timeScale = 1f;
        sl.value = PlayerPrefs.GetFloat("MouseSensitivity");
        fps.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        crossHairDetection.sprite = knobImage;
        detectionTxt.text = null;
    }

    #region Optimized Crosshair Methods
    
    /// <summary>
    /// Sets crosshair state with optimized tween (only creates tween if state changed)
    /// </summary>
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
                rt.sizeDelta = new Vector2(50, 50);
                crossHairDetection.sprite = pickImage;
                crossHairDetection.DOFade(1, 1);
                break;
            case CrosshairState.Drop:
                rt.sizeDelta = new Vector2(50, 50);
                crossHairDetection.sprite = dropImage;
                crossHairDetection.DOFade(1, 1);
                break;
            case CrosshairState.DoorOpen:
                rt.sizeDelta = new Vector2(50, 50);
                crossHairDetection.sprite = doorOpenImage;
                crossHairDetection.DOFade(1, 1);
                break;
            case CrosshairState.DoorClose:
                rt.sizeDelta = new Vector2(50, 50);
                crossHairDetection.sprite = doorCloseImage;
                crossHairDetection.DOFade(1, 1);
                break;
        }
    }

    #endregion

    #region Button Visibility Methods

    public void SetDoorButtonVisible(bool visible)
    {
        SetButtonVisible(door, visible);
    }

    public void SetPickButtonVisible(bool visible)
    {
        SetButtonVisible(pick, visible);
    }

    public void SetUseDeviceButtonVisible(bool visible)
    {
        SetButtonVisible(useDevice, visible);
    }

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
    public void OnDisable() 
    {
        int killedTweens = DOTween.KillAll();
        Debug.Log("killed " + killedTweens + " tweens");
    }
    public void RateUsClick()
    {
        AA_AnalyticsManager.Agent.TrackButtonClick("rate_us");
        Application.OpenURL("market://details?id=" + Application.identifier);
    }
    public void LvlCompleteON()
    {
        levelCompletePanel.SetActive(true);
    }
    /// <summary>
    /// UI BUTTONS
    /// </summary>
    private void OnApplicationQuit() 
    {
		AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "app_quit");
		AA_AnalyticsManager.Agent.TrackSessionEnd("app_quit", AnalyticsTracker.GetCurrentScene());
    }
    public void DoorOpenCloseBtn()
    {
        Debug.Log("door click");
        PickDropController.instance.DoorOpenCloseBtn();

    }

    public void PauseBtn()
    {
        AA_AnalyticsManager.Agent.TrackButtonClick("pause");
        ArcadiaSdkManager.CurrentAdPlacement = "pause_interstitial";
        ArcadiaSdkManager.Agent.ShowInterstitialAd();
        Time.timeScale = 0;
        pausePanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.PauseAll();
        AudioManager.Instance.SetBGSetting(true);
        AudioManager.Instance.StartGame();
    }


    public void ResumeBtn()
    {
        AA_AnalyticsManager.Agent.TrackButtonClick("resume");
        Time.timeScale = 1;
        ArcadiaSdkManager.Agent.ShowBanner();
        pausePanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.ResumeAll();
        AudioManager.Instance.GameEnd();
        AudioManager.Instance.SetBGSetting(false);
    }


    public void HomeBtn() 
    {
        AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "home_button");
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
        AudioManager.Instance.PlaySFX(SFX.Click);
    }


    public void ReplayBtn()
    {
        AA_AnalyticsManager.Agent.TrackLevelRetry(GamePreference.selectedLevel);
        Time.timeScale = 1;
        ArcadiaSdkManager.CurrentAdPlacement = "replay_rewarded";
        if(!ArcadiaSdkManager.Agent.removeAds) ArcadiaSdkManager.Agent.ShowRewardedAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        AudioManager.Instance.PlaySFX(SFX.Click);
    }

   
    public void NextBtn()
    {
        AA_AnalyticsManager.Agent.TrackButtonClick("next_level");
        Time.timeScale = 1;
        ArcadiaSdkManager.CurrentAdPlacement = "next_rewarded";
        if(!ArcadiaSdkManager.Agent.removeAds) ArcadiaSdkManager.Agent.ShowRewardedAd();
        GamePreference.selectedLevel++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        AudioManager.Instance.PlaySFX(SFX.Click);
    }
    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sl.value);

        fps.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
    }

}
