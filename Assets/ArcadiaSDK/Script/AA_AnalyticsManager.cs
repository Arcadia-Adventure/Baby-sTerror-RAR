using System;
using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Setup;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class AA_AnalyticsManager : MonoBehaviour
{
    private static AA_AnalyticsManager _instance = null;

    static public AA_AnalyticsManager Agent
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType(typeof(AA_AnalyticsManager)) as AA_AnalyticsManager;
                if (_instance == null)
                {
                    GameObject obj = new GameObject("AA_AnalyticsManager");
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<AA_AnalyticsManager>();
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this.gameObject.GetComponent<AA_AnalyticsManager>();
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != _instance)
                Destroy(this.gameObject);
        }
    }

    public UnityEvent<bool> onGameAnalyticsInitialize;
    public UnityEvent<bool> onFirebaseInitialize;
    public void OnEnable()
    {
        InitializeAnalytics();
    }
    public void InitializeAnalytics()
    {
        GameAnalyticsManager.OnInitialize=onGameAnalyticsInitialize;
        FirebaseManager.onInitialize=onFirebaseInitialize;
        GameAnalyticsManager.Initialize();
        FirebaseManager.InitializeFirebase();
    }
    public void GameStartAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelStartEvent(levelNo);
        GameAnalyticsManager.GameStartAnalytics(levelNo);
    }
    public void GameFailAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelFailEvent(levelNo);
        GameAnalyticsManager.GameFailAnalytics(levelNo);
    }
    public void GameCompleteAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelCompleteEvent(levelNo);
        GameAnalyticsManager.GameCompleteAnalytics(levelNo);
    }
    public void CustomEvent(string param, string value)
    {
        FirebaseManager.LogEvent("custom", param, value);
        GameAnalyticsManager.CustomEvent(param, value);
    }

    public void TrackScreenView(string screenName)
    {
        FirebaseManager.LogScreenView(screenName);
        GameAnalyticsManager.DesignEvent("screen:" + screenName);
    }

    public void TrackButtonClick(string buttonName)
    {
        FirebaseManager.LogEvent("button_click", "button", buttonName);
        GameAnalyticsManager.DesignEvent("button:" + buttonName);
    }

    public void TrackLevelSelected(int level, int openLevels)
    {
        FirebaseManager.LogDesignEvent("level_selected",
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("open_levels", openLevels));
        GameAnalyticsManager.DesignEvent("level_selected:" + level, openLevels);
    }

    public void TrackObjectiveCompleted(int level, int objectiveIndex, string taskType)
    {
        FirebaseManager.LogDesignEvent("objective_completed",
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("objective_index", objectiveIndex),
            new Firebase.Analytics.Parameter("task_type", taskType),
            new Firebase.Analytics.Parameter("time_in_level", (int)AnalyticsTracker.TimeSinceLevel));
        GameAnalyticsManager.DesignEvent("objective:completed:" + level + ":" + objectiveIndex);
    }

    public void TrackObjectiveStalled(int level, int objectiveIndex, float elapsedSeconds)
    {
        FirebaseManager.LogDesignEvent("objective_stalled",
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("objective_index", objectiveIndex),
            new Firebase.Analytics.Parameter("stall_seconds", (int)elapsedSeconds));
        GameAnalyticsManager.DesignEvent("objective:stalled:" + level + ":" + objectiveIndex, elapsedSeconds);
    }

    public void TrackLevelRetry(int level)
    {
        AnalyticsTracker.OnLevelRetry();
        FirebaseManager.LogDesignEvent("level_retry",
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("fail_count", AnalyticsTracker.LevelFailCount));
        GameAnalyticsManager.GameFailAnalytics(level);
        GameAnalyticsManager.DesignEvent("level:retry:" + level, AnalyticsTracker.LevelFailCount);
    }

    public void TrackLevelAbandon(int level, string reason)
    {
        FirebaseManager.LogDesignEvent("level_abandon",
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("reason", reason),
            new Firebase.Analytics.Parameter("objective_index", AnalyticsTracker.CurrentObjectiveIndex),
            new Firebase.Analytics.Parameter("time_in_level", (int)AnalyticsTracker.TimeSinceLevel),
            new Firebase.Analytics.Parameter("fail_count", AnalyticsTracker.LevelFailCount));
        GameAnalyticsManager.DesignEvent("level:abandon:" + level + ":" + reason);
    }

    public void TrackAdEvent(string eventType, string adType, string placement)
    {
        FirebaseManager.LogAdEvent(eventType, adType, placement);
        GameAnalyticsManager.AdTrackingAnalytics(adType, eventType + ":" + placement);
    }

    public void TrackSessionEnd(string reason, string scene)
    {
        float duration = AnalyticsTracker.TimeSinceSessionStart;
        FirebaseManager.LogSessionEnd(reason, scene, duration);
        GameAnalyticsManager.DesignEvent("session:end:" + reason, duration);
    }
}
