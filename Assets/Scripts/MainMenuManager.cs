using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
	#region Singleton

	public static MainMenuManager instance;
	void Awake()
	{
		if (instance == null)
		{
			instance = this;
			
		}
		else
		{
			Destroy(gameObject);
			return;
		}
	}

	#endregion


	public GameObject settingPanel;
	public GameObject quitPanel;


	public Slider slider;

	public GameObject musicOn;
	public GameObject musicOff;

	public GameObject soundOn;
	public GameObject soundOff;

	private void Start()
	{
		GoogleAdMobController.instance.ShowBanner();
		// set ui slider value from player prefs
		slider.value = PlayerPrefs.GetFloat("MouseSensitivity");

		SetMusicSavedValue();

		SetSoundSavedValue();
	}

	public void MoreGames()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
		Application.OpenURL("https://apps.apple.com/us/developer/muhammad-umar-shafaqat/id1671095846");
#endif
	}
	public void RateUs()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
			Application.OpenURL("itms-apps://itunes.apple.com/app/" + "1672844290");
#endif
	
	}
	public void PrivacyPolicy()
    {
#if UNITY_ANDROID
		Application.OpenURL("https://sites.google.com/view/arcadia-adventures/home");
#elif UNITY_IOS
		Application.OpenURL("https://sites.google.com/view/ommygames/home");
#endif
    }
	public void SetMusicSavedValue()
    {
		if (PlayerPrefs.GetInt("Music") == 1)
		{

			musicOff.SetActive(false);
			SoundManager.instance.BG.Play();
			musicOn.SetActive(true);

			music = true;
		}
		else
		{
			musicOn.SetActive(false);
			SoundManager.instance.BG.Stop();
			musicOff.SetActive(true);

			music = false;
		}
	}

	public void SetSoundSavedValue()
    {
		if(PlayerPrefs.GetInt("Sound") == 1)
        {
			soundOff.SetActive(false);
			SoundManager.instance.click.mute = false;
			soundOn.SetActive(true);
			sound = true;
        }
        else
        {
			soundOn.SetActive(false);
			SoundManager.instance.click.mute = true;
			soundOff.SetActive(true);

			sound = false;
		}
    }

    public void PlayBtn()
	{
		SceneManager.LoadScene("LevelSelection");
		SoundManager.instance.ClickSound();
	}

	public void SettingBtn()
	{ 
		settingPanel.SetActive(true);
		SoundManager.instance.ClickSound();
	}


	public void BackBtn()
	{
		settingPanel.SetActive(false);
		SoundManager.instance.ClickSound();
	}


	public void QuitBtn()
    { 
		quitPanel.SetActive(true);
		SoundManager.instance.ClickSound();
	}

	public void QuitBtnYes()
    {
		Application.Quit();
		SoundManager.instance.ClickSound();
	}

	public void QuitBtnNo()
	{
		quitPanel.SetActive(false);
		SoundManager.instance.ClickSound();
	}


	public void SensivitySlider()
    {
		//set slider ui value in player prefs
		PlayerPrefs.SetFloat("MouseSensitivity", slider.value);
	}

	public bool music;
	
	public void MusicBtn()
    {
        if (music)
		{
			// Music Off

			musicOn.SetActive(false);
			SoundManager.instance.BG.Stop();
			musicOff.SetActive(true);

			music = false;

			PlayerPrefs.SetInt("Music", 0);
		}
        else
        {
			// Music On

			musicOff.SetActive(false);
			SoundManager.instance.BG.Play();
			musicOn.SetActive(true);

			music = true;

			PlayerPrefs.SetInt("Music", 1);
		}
    }

	public bool sound;

	public void SoundBtn()
    {
        if (sound)
        {
			soundOn.SetActive(false);
			SoundManager.instance.click.mute = true;
			soundOff.SetActive(true);

			sound = false;

			PlayerPrefs.SetInt("Sound", 0);
        }
        else
        {
			soundOff.SetActive(false);
			SoundManager.instance.click.mute = false;
			soundOn.SetActive(true);

			SoundManager.instance.click.Play();

			sound = true;

			PlayerPrefs.SetInt("Sound", 1);
        }
    }
}
