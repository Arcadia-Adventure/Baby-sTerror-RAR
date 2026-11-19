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
        Application.targetFrameRate = ResolveTargetFrameRate();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SetDefaultPlayerPrefs();
    }

    int ResolveTargetFrameRate()
    {
        string vendor = SystemInfo.graphicsDeviceVendor ?? string.Empty;
        string device = SystemInfo.graphicsDeviceName ?? string.Empty;
        bool isPowerVr =
            vendor.IndexOf("Imagination", System.StringComparison.OrdinalIgnoreCase) >= 0
            || vendor.IndexOf("PowerVR", System.StringComparison.OrdinalIgnoreCase) >= 0
            || device.IndexOf("PowerVR", System.StringComparison.OrdinalIgnoreCase) >= 0;

        bool lowMemory = SystemInfo.systemMemorySize < 3000 || SystemInfo.graphicsMemorySize <= 512;
        if (isPowerVr || lowMemory)
            return 30;

        return targetFrameRate;
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
