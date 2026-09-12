using System.Collections.Generic;
using Ommy.Notifications;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public int targetFrameRate = 60;

    protected override void Awake()
    {
        base.Awake();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SetDefaultPlayerPrefs();
    }

    protected void Start()
    {
        OverrideDailyNotification();
        AnalyticsTracker.Initialize();
        AA_AnalyticsManager.Agent.TrackScreenView("session_start");
    }

    void OverrideDailyNotification()
    {
        int nextLevel = GamePreference.openLevels + 1;
        var levelData = LevelConfigLoader.GetLevelData(nextLevel);
        if (levelData == null) return;

        var notificationData = new NotificationData
        {
            title = "Level " + levelData.level,
            message = levelData.missionName
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
