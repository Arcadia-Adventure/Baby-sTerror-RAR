using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;

public class FirebaseManager : MonoBehaviour
{
    public const string AdsSettingsKey = "ads_settings";
    public const string GameSettingsKey = "game_settings";
    const ulong FetchTimeoutMs = 5000;

    public static UnityEvent<bool> onInitialize;
    public static UnityEvent onRemoteConfigReady = new UnityEvent();

    static FirebaseApp app;
    static bool _firebaseInitStarted;

    public static AdsRemoteSettings AdsSettings { get; private set; } = new AdsRemoteSettings();
    public static GameRemoteSettings GameSettings { get; private set; } = new GameRemoteSettings();
    public static bool IsRemoteConfigReady { get; private set; }

    public static void SetRemoteConfigDefaults(AdsRemoteSettings adsDefaults, GameRemoteSettings gameDefaults)
    {
        if (IsRemoteConfigReady) return;
        if (adsDefaults != null) AdsSettings = adsDefaults;
        if (gameDefaults != null) GameSettings = gameDefaults;
    }

    public static IEnumerator WaitForRemoteConfig(float timeoutSeconds = 5f)
    {
        float elapsed = 0f;
        while (!IsRemoteConfigReady && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsRemoteConfigReady)
            MarkRemoteConfigReady();
    }

    static void MarkRemoteConfigReady()
    {
        if (IsRemoteConfigReady) return;
        IsRemoteConfigReady = true;
        onRemoteConfigReady?.Invoke();
    }

    public static void InitializeFirebase()
    {
        if (_firebaseInitStarted) return;
        _firebaseInitStarted = true;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(
        previousTask =>
        {
          var dependencyStatus = previousTask.Result;
          if (dependencyStatus == Firebase.DependencyStatus.Available)
          {
            app = Firebase.FirebaseApp.DefaultInstance;
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            onInitialize?.Invoke(true);
            FetchRemoteConfig();
          }
          else
          {
            onInitialize?.Invoke(false);
            MarkRemoteConfigReady();
            UnityEngine.Debug.LogError(
              $"Could not resolve all Firebase dependencies: {dependencyStatus}\n" +
              "Firebase Unity SDK is not safe to use here");
          }
        });
    }

    static void FetchRemoteConfig()
    {
        try
        {
            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            var defaults = new Dictionary<string, object>
            {
                { AdsSettingsKey, JsonUtility.ToJson(AdsSettings) },
                { GameSettingsKey, JsonUtility.ToJson(GameSettings) }
            };

            var settings = remoteConfig.ConfigSettings;
            settings.FetchTimeoutInMilliseconds = FetchTimeoutMs;
            settings.MinimumFetchIntervalInMilliseconds = 0;

            remoteConfig.SetDefaultsAsync(defaults).ContinueWithOnMainThread(defaultsTask =>
            {
                if (defaultsTask.IsFaulted)
                    Debug.LogWarning($"[FirebaseManager] Remote Config SetDefaults failed: {defaultsTask.Exception}");

                remoteConfig.SetConfigSettingsAsync(settings).ContinueWithOnMainThread(_ =>
                {
                    remoteConfig.FetchAndActivateAsync().ContinueWithOnMainThread(fetchTask =>
                    {
                        if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                        {
                            Debug.LogWarning($"[FirebaseManager] Remote Config fetch failed: {fetchTask.Exception}");
                        }
                        else
                        {
                            ApplyRemoteConfigValues(remoteConfig);
                        }
                        MarkRemoteConfigReady();
                    });
                });
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseManager] Remote Config setup failed: {e.Message}");
            MarkRemoteConfigReady();
        }
    }

    static void ApplyRemoteConfigValues(FirebaseRemoteConfig remoteConfig)
    {
        TryParseAdsSettings(remoteConfig.GetValue(AdsSettingsKey).StringValue);
        TryParseGameSettings(remoteConfig.GetValue(GameSettingsKey).StringValue);
        Debug.Log($"[FirebaseManager] Remote Config applied. ads={JsonUtility.ToJson(AdsSettings)} game={JsonUtility.ToJson(GameSettings)}");
    }

    static void TryParseAdsSettings(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var parsed = JsonUtility.FromJson<AdsRemoteSettings>(json);
            if (parsed != null) AdsSettings = parsed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseManager] Failed to parse {AdsSettingsKey}: {e.Message}");
        }
    }

    static void TryParseGameSettings(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var parsed = JsonUtility.FromJson<GameRemoteSettings>(json);
            if (parsed != null) GameSettings = parsed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseManager] Failed to parse {GameSettingsKey}: {e.Message}");
        }
    }

    // Log a custom event to Firebase Analytics
    public static void LogEvent(string eventName, string parameterName, string parameterValue)
    {
        FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
    }
    public static void LogLevelStartEvent(int level)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart,
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter(FirebaseAnalytics.ParameterLevelName, "level_" + level));
    }

    public static void LogLevelFailEvent(int level, int score = -1)
    {
        FirebaseAnalytics.LogEvent("level_fail",
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter(FirebaseAnalytics.ParameterLevelName, "level_" + level),
            new Parameter(FirebaseAnalytics.ParameterScore, score));

        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter(FirebaseAnalytics.ParameterLevelName, "level_" + level),
            new Parameter(FirebaseAnalytics.ParameterSuccess, 0),
            new Parameter(FirebaseAnalytics.ParameterScore, score));
    }

    public static void LogLevelCompleteEvent(int level, int score = -1)
    {
        FirebaseAnalytics.LogEvent("level_complete",
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter(FirebaseAnalytics.ParameterLevelName, "level_" + level),
            new Parameter(FirebaseAnalytics.ParameterScore, score));

        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter(FirebaseAnalytics.ParameterLevelName, "level_" + level),
            new Parameter(FirebaseAnalytics.ParameterSuccess, 1),
            new Parameter(FirebaseAnalytics.ParameterScore, score));
    }

    public static void LogScreenView(string screenName)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventScreenView,
            new Parameter(FirebaseAnalytics.ParameterScreenName, screenName));
    }

    public static void LogDesignEvent(string eventName, params Parameter[] parameters)
    {
        FirebaseAnalytics.LogEvent(eventName, parameters);
    }

    public static void LogAdEvent(string eventType, string adType, string placement)
    {
        FirebaseAnalytics.LogEvent("ad_event",
            new Parameter("event_type", eventType),
            new Parameter("ad_type", adType),
            new Parameter("placement", placement));
    }

    public static void LogSessionEnd(string reason, string scene, float duration)
    {
        FirebaseAnalytics.LogEvent("session_end",
            new Parameter("reason", reason),
            new Parameter("scene", scene),
            new Parameter("duration_seconds", (int)duration));
    }
}
