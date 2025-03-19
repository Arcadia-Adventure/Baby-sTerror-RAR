using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using ControlFreak2.UI;
using ControlFreak2;
public class UIManager : MonoBehaviour
{

    public GameObject levelCompletePanel;
    public GameObject pausePanel;
 

   // course sprite
    public Sprite knobImage;
    public Sprite doorOpenImage;
    public Sprite doorCloseImage;
    public Sprite pickImage;
    public Sprite dropImage;

    
    public TouchButtonSpriteAnimator door;

    public TouchButtonSpriteAnimator pick;

    public Image crossHairDetection;
    public TextMeshProUGUI detectionTxt;
    public RectTransform rt;

    public GameObject nextBtn;

    public FirstPersonController fps;

    public static UIManager instance;
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
       sl.value = PlayerPrefs.GetFloat("MouseSensitivity");

        fps.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");

        crossHairDetection.sprite = knobImage;
        detectionTxt.text = null;

       
    }


    public void LvlCompleteON()
    {
        AA_AnalyticsManager.Agent.GameCompleteAnalytics(GameManager.instance.selectedLevel);
        levelCompletePanel.SetActive(true);
    }


    /// <summary>
    /// UI BUTTONS
    /// </summary>

    
 

    private void OnApplicationQuit() 
    {
		AA_AnalyticsManager.Agent.CustomEvent("Exit Game Level",GameManager.instance.selectedLevel.ToString());
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

        //Camera.main.GetComponent<AudioListener>().enabled = false;

        SoundManager.instance.ClickSound();
        GamePlayManager.instance.doorBell.mute = true;
        BabyController.instance.babyCry.mute = true;
        BabyController.instance.babyAngryVoice.mute = true;
        GamePlayManager.instance.RainBG.mute = true;
        Items.instance.fireLvl7.GetComponentInChildren<AudioSource>().mute=true;
        Items.instance.fireLvl10.GetComponentInChildren<AudioSource>().mute=true;
        Items.instance.fireLvl8.GetComponentInChildren<AudioSource>().mute=true;
        SoundManager.instance.BG.Play();
    }


    public void ResumeBtn()
    {
        Time.timeScale = 1;
        GamePlayManager.instance.doorBell.mute = false;
        BabyController.instance.babyAngryVoice.mute = false;
        GamePlayManager.instance.RainBG.mute = false;
        BabyController.instance.babyCry.mute = false;
        Items.instance.fireLvl7.GetComponentInChildren<AudioSource>().mute=false;
        Items.instance.fireLvl10.GetComponentInChildren<AudioSource>().mute = false;
        Items.instance.fireLvl8.GetComponentInChildren<AudioSource>().mute = false;
        ArcadiaSdkManager.Agent.ShowBanner();
        pausePanel.SetActive(false);

       // Camera.main.GetComponent<AudioListener>().enabled = true;

        SoundManager.instance.ClickSound();
        SoundManager.instance.BG.Stop();
    }


    public void HomeBtn() 
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");

        SoundManager.instance.ClickSound();
    }


    public void ReplayBtn()
    {
        Time.timeScale = 1;
        ArcadiaSdkManager.Agent.ShowRewardedAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        SoundManager.instance.ClickSound();
    }

   
    public void NextBtn()
    {
        Time.timeScale = 1;
        ArcadiaSdkManager.Agent.ShowRewardedAd();
        GameManager.instance.selectedLevel++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        SoundManager.instance.ClickSound();
    }
    public void HintClick()
    {
        SoundManager.instance.ClickSound();
        ArcadiaSdkManager.Agent.ShowRewardedAd();
        HintManager.Instance.ActivateIndicator(GameManager.instance.selectedLevel-1, ObjectiveController.instance.currentTask);
    }
  

 
    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sl.value);

        fps.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
    }

}
