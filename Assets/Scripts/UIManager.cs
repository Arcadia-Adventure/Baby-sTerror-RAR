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
        levelCompletePanel.SetActive(true);
    }


    /// <summary>
    /// UI BUTTONS
    /// </summary>

    
 


    public void DoorOpenCloseBtn()
    {
        Debug.Log("door click");
        PickDropController.instance.DoorOpenCloaeBtn();

    }

    public void PauseBtn()
    {
      
        Time.timeScale = 0;
        pausePanel.SetActive(true);

        SoundManager.instance.ClickSound();

        if(GameManager.instance.selectedLevel == 1)
        {
            GamePlayManager.instance.doorBell.Stop();
        }

        GamePlayManager.instance.RainBG.mute = true;
        BabyController.instance.babyCry.enabled = false;
    }



    public void ResumeBtn()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1;

        SoundManager.instance.ClickSound();

      

        GamePlayManager.instance.RainBG.mute = false;

        if(PickDropController.instance.heldObj != true)
        {
            if (GameManager.instance.selectedLevel == 1)
            {
                GamePlayManager.instance.doorBell.Play();
            }

            BabyController.instance.babyCry.enabled = true;
        }
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        SoundManager.instance.ClickSound();
    }

   
    public void NextBtn()
    {
        GameManager.instance.selectedLevel++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        SoundManager.instance.ClickSound();
    }

  

 
    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sl.value);

        fps.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
    }

}
