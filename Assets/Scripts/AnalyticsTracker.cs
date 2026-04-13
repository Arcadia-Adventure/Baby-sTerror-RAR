using System;
using Ommy.Prefs;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnalyticsTracker
{
    static bool _initialized;

    public static string SessionId { get; private set; }
    public static float SessionStartTime { get; private set; }
    public static float LevelStartTime { get; private set; }
    public static int LevelFailCount { get; private set; }
    public static int CurrentLevel { get; private set; }
    public static int CurrentObjectiveIndex { get; private set; }
    public static bool IsFirstSession { get; private set; }

    public static string LastAdType { get; set; }
    public static string LastAdPlacement { get; set; }
    public static float LastAdTime { get; set; }

    public static float TimeSinceSessionStart => Time.realtimeSinceStartup - SessionStartTime;
    public static float TimeSinceLevel => Time.realtimeSinceStartup - LevelStartTime;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
        SessionStartTime = Time.realtimeSinceStartup;
        IsFirstSession = PlayerPrefs.GetInt("AA_SessionCount", 0) == 0;
        PlayerPrefs.SetInt("AA_SessionCount", PlayerPrefs.GetInt("AA_SessionCount", 0) + 1);
        LevelFailCount = 0;
        CurrentObjectiveIndex = 0;
        LastAdType = "";
        LastAdPlacement = "";
    }

    public static void OnLevelStart(int level)
    {
        CurrentLevel = level;
        LevelStartTime = Time.realtimeSinceStartup;
        LevelFailCount = 0;
        CurrentObjectiveIndex = 0;
    }

    public static void OnLevelRetry()
    {
        LevelFailCount++;
    }

    public static void OnObjectiveCompleted(int objectiveIndex)
    {
        CurrentObjectiveIndex = objectiveIndex + 1;
    }

    public static void OnAdShown(string adType, string placement)
    {
        LastAdType = adType;
        LastAdPlacement = placement;
        LastAdTime = Time.realtimeSinceStartup;
    }

    public static string GetCurrentScene()
    {
        return SceneManager.GetActiveScene().name;
    }

    public static int GetOpenLevels()
    {
        return GamePreference.openLevels;
    }

    public static bool HasRemovedAds()
    {
        return PlayerPrefs.GetInt("removeAds", 0) == 1;
    }
}
