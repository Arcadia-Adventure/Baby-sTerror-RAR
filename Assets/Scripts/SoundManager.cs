using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	#region Singleton

	public static SoundManager instance;
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
	}

	#endregion


	public AudioSource BG;
	public AudioSource click;

	public AudioSource happy;
	public AudioSource fridgeOpen;
	public AudioSource fridgeClose;
	

	public AudioSource doorOpen;
	public AudioSource doorClose;
	public AudioSource doorBreak;

	public AudioSource playerFall;
	public AudioSource playerStandup;


	public AudioSource pick;
	public AudioSource drop;
	public AudioSource levelComplete;


	public void PlayerFall()
	{
		playerFall.Play();
	}

	public void PlayerStandUP()
    {
		playerStandup.Play();
    }

	
    public void BGUISound()
    {
		BG.Play();
    }

	public void ClickSound()
    {
		click.Play();
    }
	
	public void BabyHappy()
    {
		happy.Play();
    }
	
	public void FridgeOpenDoor()
    {
		fridgeOpen.Play();
    }

	public void FridgeCloseDoor()
	{
		fridgeClose.Play();
	}
	public void DoorOpen()
    {
		doorOpen.Play();
    }
	public void DoorClose()
	{
		doorClose.Play();
	}

	public void DoorBreak()
    {
		doorBreak.Play();
    }

	public void PickItem()
    {
		pick.Play();
    }
	public void DropItem()
    {
		drop.Play();
    }

	public void LevelCompleteSound()
    {
		levelComplete.Play();
	}


	public IEnumerator LevelComplete()
	{
		yield return new WaitForSeconds(1.8f);
		GamePlayManager.instance.LevelComplete();

		GamePlayManager.instance.RainBG.Stop();
		SoundManager.instance.LevelCompleteSound();
		StartCoroutine(UIBGSound());
	}

	public IEnumerator UIBGSound()
	{
		yield return new WaitForSeconds(2f);
		SoundManager.instance.BGUISound();
		//print("BGUISOUND");
	}

}
