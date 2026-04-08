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

    // Tween state tracking (optimized - only creates tweens when state changes)
    private CrosshairState currentCrosshairState = CrosshairState.None;
    private bool doorButtonVisible = false;
    private bool pickButtonVisible = false;
    private bool useDeviceButtonVisible = false;
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

    #region Optimized Button Methods

    /// <summary>
    /// Shows/hides door button with fade animation (only if state changed)
    /// </summary>
    public void SetDoorButtonVisible(bool visible)
    {
        if (doorButtonVisible == visible) return;
        
        doorButtonVisible = visible;
        door.image.DOKill();
        
        if (visible)
        {
            door.image.DOFade(1, 1);
            door.transform.parent.GetComponent<TouchButton>().enabled = true;
        }
        else
        {
            door.image.DOFade(0, 1);
            door.transform.parent.GetComponent<TouchButton>().enabled = false;
        }
    }

    /// <summary>
    /// Shows/hides pick button with fade animation (only if state changed)
    /// </summary>
    public void SetPickButtonVisible(bool visible)
    {
        if (pickButtonVisible == visible) return;
        
        pickButtonVisible = visible;
        pick.image.DOKill();
        
        if (visible)
        {
            pick.image.DOFade(1, 1);
            pick.transform.parent.GetComponent<TouchButton>().enabled = true;
        }
        else
        {
            pick.image.DOFade(0, 1);
            pick.transform.parent.GetComponent<TouchButton>().enabled = false;
        }
    }
    /// <summary>
    /// Shows/hides use device button with fade animation (only if state changed)
    /// </summary>
    public void SetUseDeviceButtonVisible(bool visible)
    {
        if (useDeviceButtonVisible == visible) return;
        useDeviceButtonVisible = visible;
        useDevice.image.DOKill();
        if (visible)
        {
            useDevice.image.DOFade(1, 1);
            useDevice.transform.parent.GetComponent<TouchButton>().enabled = true;
        }
        else
        {
            useDevice.image.DOFade(0, 1);
            useDevice.transform.parent.GetComponent<TouchButton>().enabled = false;
        }
    }

    #endregion
    public void OnDisable() 
    {
        int killedTweens = DOTween.KillAll();
        Debug.Log("killed " + killedTweens + " tweens");
    }
    public void RateUsClick()
    {
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
		AA_AnalyticsManager.Agent.CustomEvent("Exit Game Level",GamePreference.selectedLevel.ToString());
    }
    public void DoorOpenCloseBtn()
    {
        Debug.Log("door click");
        PickDropController.instance.DoorOpenCloseBtn();

    }

    public void PauseBtn()
    {
        ArcadiaSdkManager.Agent.ShowInterstitialAd();
        Time.timeScale = 0;
        pausePanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
        BabyController.Instance.MuteAudio(true);
        GamePlayManager.Instance.RainBG.mute = true;
        AudioManager.Instance.SetBGSetting(false);
    }


    public void ResumeBtn()
    {
        Time.timeScale = 1;
        BabyController.Instance.MuteAudio(false);
        GamePlayManager.Instance.RainBG.mute = false;
        ArcadiaSdkManager.Agent.ShowBanner();
        pausePanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.SetBGSetting(false);
    }


    public void HomeBtn() 
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
        AudioManager.Instance.PlaySFX(SFX.Click);
    }


    public void ReplayBtn()
    {
        Time.timeScale = 1;
        if(!ArcadiaSdkManager.Agent.removeAds) ArcadiaSdkManager.Agent.ShowRewardedAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        AudioManager.Instance.PlaySFX(SFX.Click);
    }

   
    public void NextBtn()
    {
        Time.timeScale = 1;
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
