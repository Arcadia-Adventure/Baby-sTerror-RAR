using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
		GoogleAdMobController.instance.ShowBanner();
		UnlockLevels();
		//print(PlayerPrefs.GetInt("totalUnlockLevel"));
    }



    public GameObject[] lockSprite;
	public GameObject[] barImg;



    public void UnlockLevels()
    {
		int totalUnlockLevel = PlayerPrefs.GetInt("totalUnlockLevel");

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
        GameManager.instance.selectedLevel = selectedLevel;

		SceneManager.LoadScene("GamePlay");

		SoundManager.instance.ClickSound();

	
	}

	public void UnlockAllLevelBtn()
    {
		SoundManager.instance.ClickSound();
	}
}
