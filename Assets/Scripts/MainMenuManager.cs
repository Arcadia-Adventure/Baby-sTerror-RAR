using System.Collections;
using System.Collections.Generic;
using Ommy.Audio;
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
	public GameObject restoreButton;

	private void Start()
	{
		if (Application.platform == RuntimePlatform.Android)
        {
			restoreButton.SetActive(false);
        }
        ArcadiaSdkManager.Agent.ShowBanner();
		// set ui slider value from player prefs
		slider.value = PlayerPrefs.GetFloat("MouseSensitivity");

		SetMusicSavedValue();

		SetSoundSavedValue();
	}

	public void MoreGames()
    {
#if UNITY_ANDROID
        //Application.OpenURL("market://details?id=" + Application.identifier);
		Application.OpenURL("https://play.google.com/store/apps/developer?id=Arcadia+Adventures");
#elif UNITY_IOS
		Application.OpenURL("https://apps.apple.com/us/developer/muhammad-umar-shafaqat/id1671095846");
#endif
	}
	public void RateUs()
    {
#if UNITY_ANDROID
		Application.OpenURL("market://details?id=" + Application.identifier);
		//try { ArcadiaSdkManager.Agent.ShowRateUs(); } 
		//catch { 
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
			AudioManager.Instance.SetBGSetting(true);
			musicOn.SetActive(true);

			music = true;
		}
		else
		{
			musicOn.SetActive(false);
			AudioManager.Instance.SetBGSetting(false);
			musicOff.SetActive(true);

			music = false;
		}
	}

	public void SetSoundSavedValue()
    {
		if(PlayerPrefs.GetInt("Sound") == 1)
        {
			soundOff.SetActive(false);
			AudioManager.Instance.SetSFXSetting(true);
			soundOn.SetActive(true);
			sound = true;
        }
        else
        {
			soundOn.SetActive(false);
			AudioManager.Instance.SetSFXSetting(false);
			soundOff.SetActive(true);

			sound = false;
		}
    }

    public void PlayBtn()
	{
		SceneManager.LoadScene("LevelSelection");
		AudioManager.Instance.PlaySFX(SFX.Click);
	}

	public void SettingBtn()
	{ 
		settingPanel.SetActive(true);
		AudioManager.Instance.PlaySFX(SFX.Click);
	}


	public void BackBtn()
	{
		settingPanel.SetActive(false);
		AudioManager.Instance.PlaySFX(SFX.Click);
	}


	public void QuitBtn()
    { 
		quitPanel.SetActive(true);
		AudioManager.Instance.PlaySFX(SFX.Click);
	}

	public void QuitBtnYes()
    {
		Application.Quit();
		AudioManager.Instance.PlaySFX(SFX.Click);
	}

	public void QuitBtnNo()
	{
		quitPanel.SetActive(false);
		AudioManager.Instance.PlaySFX(SFX.Click);
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
			AudioManager.Instance.SetBGSetting(false);
			musicOff.SetActive(true);

			music = false;

			PlayerPrefs.SetInt("Music", 0);
		}
        else
        {
			// Music On

			musicOff.SetActive(false);
			AudioManager.Instance.SetBGSetting(true);
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
			AudioManager.Instance.SetSFXSetting(false);
			soundOff.SetActive(true);

			sound = false;

			PlayerPrefs.SetInt("Sound", 0);
        }
        else
        {
			soundOff.SetActive(false);
			AudioManager.Instance.SetSFXSetting(true);
			soundOn.SetActive(true);

			AudioManager.Instance.PlaySFX(SFX.Click);

			sound = true;

			PlayerPrefs.SetInt("Sound", 1);
        }
    }
}
