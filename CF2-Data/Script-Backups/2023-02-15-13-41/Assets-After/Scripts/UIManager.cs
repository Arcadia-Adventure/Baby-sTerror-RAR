using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public GameObject levelCompletePanel;
    public GameObject pausePanel;

    public GameObject pickBtn;
    public GameObject dropBtn;
    public GameObject doorOpenCloseBtn;

    public Sprite knobImage;
    public Sprite doorImage;
    public Sprite pickImage;
    public Sprite dropImage;


    public Image door;
    public Image pick;
    public Image crossHairDetection;
    public TextMeshProUGUI detectionTxt;
    public RectTransform rt;
   


    public static UIManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

    }


    private void Start()
    {
        pickBtn.SetActive(false);
        dropBtn.SetActive(false);
        doorOpenCloseBtn.SetActive(false);

       
        crossHairDetection.sprite = knobImage;
        detectionTxt.text = null;
    }


    private void Update()
    {
        if (ControlFreak2.CF2Input.GetKeyDown(KeyCode.P))
        {
            PickBtn();
        }
        else if (ControlFreak2.CF2Input.GetKeyDown(KeyCode.O))
        {
            DropBtn();
        }
    }


    public void LvlCompleteON()
    {
        levelCompletePanel.SetActive(true);
    }


    /// <summary>
    /// UI BUTTONS
    /// </summary>

    public void PickBtn()
    {
        PickDropController.instance.PickupObject();
    }
    public void DropBtn()
    {
       

        if (PickDropController.instance.heldObj != null)
        {
            PickDropController.instance.DropObject();

            SoundManager.instance.DropItem();

            GamePlayManager.instance.feederBlueGlow.Play();
            GamePlayManager.instance.shirtBlueGlow.Play();
            GamePlayManager.instance.axeBlueGlow.Play();
            GamePlayManager.instance.toyBlueGlow.Play();
            GamePlayManager.instance.cylinderBlueGlow.Play();
            GamePlayManager.instance.talismanBlueGlow.Play();
            GamePlayManager.instance.facewashGlow.Play();

            print("drop");
        }
    }


    public void DoorOpenCloseBtn()
    {
        Debug.Log("door click");
        PickDropController.instance.DoorOpenCloaeBtn();

    }

    public void PauseBtn()
    {
        Time.timeScale = 0;
        pausePanel.SetActive(true);
    }

    public void HomeBtn() 
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");

      
    }
    public void ReplayBtn()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
    }

    public void ResumeBtn()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void NextBtn()
    {
        GameManager.instance.selectedLevel++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }



}
