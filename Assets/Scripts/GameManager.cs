using System.Collections.Generic;
using Ommy.Notifications;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public int targetFrameRate = 30;

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
        Application.targetFrameRate = targetFrameRate;
        OverrideDailyNotification();
        AnalyticsTracker.Initialize();
        AA_AnalyticsManager.Agent.TrackScreenView("session_start");
    }

    void OverrideDailyNotification()
    {
        int openLevels = GamePreference.openLevels;
        if (tasksDetail == null || openLevels < 0 || openLevels >= tasksDetail.Objectives.Count)
            return;

        var currentObjective = tasksDetail.Objectives[openLevels];
        var notificationData = new NotificationData
        {
            title = currentObjective.levelNO,
            message = currentObjective.missionName
        };
        NotificationManager.Instance.dailyMessages = new List<NotificationData> { notificationData };
    }

    public void SetDefaultPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey(PrefKeys.MouseSensitivity))
            PlayerPrefs.SetFloat(PrefKeys.MouseSensitivity, 0.3f);

        if (!PlayerPrefs.HasKey(PrefKeys.Music))
            PlayerPrefs.SetInt(PrefKeys.Music, 1);

        if (!PlayerPrefs.HasKey(PrefKeys.Sound))
            PlayerPrefs.SetInt(PrefKeys.Sound, 1);

        if (!PlayerPrefs.HasKey(PrefKeys.UnlockAllLevels))
            PlayerPrefs.SetInt(PrefKeys.UnlockAllLevels, 0);
    }
}
