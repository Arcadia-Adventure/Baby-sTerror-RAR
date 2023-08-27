using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Purchasing;
public class LevelSelectionManager : MonoBehaviour
{
    #region Singleton

    public static LevelSelectionManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    #endregion
    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt("UnlockAllLevels", 1);
        unlockAllLevelsButton.enabled=false;
        UnlockLevels();
    }
    private void Start()
    {
		GoogleAdMobController.instance.DestroyBannerAd();
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            unlockAllLevelsButton.enabled= false;
        }
        //GoogleAdMobController.instance.ShowBanner();
        UnlockLevels();
        //print(PlayerPrefs.GetInt("totalUnlockLevel"));
    }


    public Image unlockAllLevelsButton;
    public GameObject[] lockSprite;
    public GameObject[] barImg;



    public void UnlockLevels()
    {
        int totalUnlockLevel = PlayerPrefs.GetInt("totalUnlockLevel");
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            totalUnlockLevel = lockSprite.Length;
        }
        for (int i = 0; i < totalUnlockLevel; i++)
        {
            lockSprite[i].SetActive(false);
            barImg[i].SetActive(true);
        }
    }


    public void BackBtn()
    {
        SceneManager.LoadScene("MainMenu");

        SoundManager.instance.ClickSound();
    }


    public void LevelSelectBtn(int selectedLevel)
    {
        FirebaseInit.instance.FireBase_Events("Selected Level", "level " + selectedLevel.ToString(), "total unlock levels  " + PlayerPrefs.GetInt("totalUnlockLevel").ToString());
        GameManager.instance.selectedLevel = selectedLevel;

        SceneManager.LoadScene("GamePlay");

        SoundManager.instance.ClickSound();


    }

    public void UnlockAllLevelBtn()
    {
        SoundManager.instance.ClickSound();
    }
}
