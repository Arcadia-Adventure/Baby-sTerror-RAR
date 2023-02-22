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


	public AudioSource pick;
	public AudioSource drop;
	public AudioSource levelComplete;



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

}
