using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	#region Singleton

	public static GameManager instance;
	void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}
		SetDefaultPlayerPrefs();
	}

	#endregion


	public int selectedLevel;

	
	public void SetDefaultPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey("MouseSensitivity"))
        {
			PlayerPrefs.SetFloat("MouseSensitivity", 0.3f);
        }

        if (!PlayerPrefs.HasKey("Music"))
        {
			PlayerPrefs.SetInt("Music", 1);
        }

        if (!PlayerPrefs.HasKey("Sound"))
        {
			PlayerPrefs.SetInt("Sound", 1);
        }

        if (!PlayerPrefs.HasKey("totalUnlockLevel"))
        {
			PlayerPrefs.SetInt("totalUnlockLevel", 0);
        }
    }

   
}
