using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Purchasing;
using UnityEngine.Events;
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
    public GameObject loadingScreen;
    public UnityEvent onPurchaseAllLevels;
    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt("UnlockAllLevels", 1);
        unlockAllLevelsButton.enabled = false;
        UnlockLevelsIfNeeded();
    }
    private void Start()
    {
		ArcadiaSdkManager.Agent.HideBanner();
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            unlockAllLevelsButton.enabled= false;
        }
        //GoogleAdMobController.instance.ShowBanner();
        UnlockLevelsIfNeeded();
        //print(PlayerPrefs.GetInt("totalUnlockLevel"));
    }


    public Image unlockAllLevelsButton;
    public GameObject[] lockSprite;
    public GameObject[] barImg;



    void UnlockLevelsIfNeeded()
    {
        int totalUnlockLevel = PlayerPrefs.GetInt("totalUnlockLevel");
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            onPurchaseAllLevels?.Invoke();
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
        loadingScreen.SetActive(true);
        GameManager.instance.selectedLevel = selectedLevel;
        SceneManager.LoadScene("GamePlay");
        SoundManager.instance.ClickSound();
        AA_AnalyticsManager.Agent.GameStartAnalytics(selectedLevel);
    }
}
