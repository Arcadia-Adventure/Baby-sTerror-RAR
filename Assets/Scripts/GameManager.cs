using System.Collections;
using System.Collections.Generic;
using Ommy.Notifications;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : Singleton<GameManager>
{
	[Header("Tasks Details override daily notification")]
	public TasksDetail tasksDetail;
	protected override void Awake()
	{
		base.Awake();
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		SetDefaultPlayerPrefs();
	}
	private void Start() 
	{
		OverrideDailyNotification();
	}
	private void OverrideDailyNotification()
	{
		var currentObjective = tasksDetail.Objectives[GamePreference.openLevels];
		var notificationData = new NotificationData
		{
			title = currentObjective.levelNO,
			message = currentObjective.missionName
		};
		NotificationManager.Instance.dailyMessages = new List<NotificationData>{ notificationData };
	}
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
		if (!PlayerPrefs.HasKey("UnlockAllLevels"))
        {
			PlayerPrefs.SetInt("UnlockAllLevels", 0);
        }
    }
}
