using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    public GameObject levelCompletePanel;
    public GameObject pausePanel;

    

    public static UIManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
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

    public void PickBtn() => PickDropController.instance.PickupObject();
     

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
