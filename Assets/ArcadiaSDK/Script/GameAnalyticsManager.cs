using GameAnalyticsSDK;
using UnityEngine;
using UnityEngine.Events;
public class GameAnalyticsManager
{    
    public static UnityEvent<bool> OnInitialize;
    public static void Initialize()
    {
        GameAnalytics.Initialize();
        GameAnalyticsILRD.SubscribeMaxImpressions();
    }

    public static void GameStartAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start,"Level_Start",levelNo.ToString(),levelNo);
    }
    public static void GameFailAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail,"Level_Fail",levelNo.ToString(),levelNo);
    }
    public static void GameCompleteAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete,"Level_Complete",levelNo.ToString(),levelNo);
    }
    public static void CustomEvent(string param, string value)
    {
        GameAnalytics.NewDesignEvent(param + ":" + value);
    }
    public static void AdTrackingAnalytics(string adType, string location)
    {
        GameAnalytics.NewDesignEvent("ad:" + adType + ":" + location);
    }
    public static void DesignEvent(string eventId)
    {
        GameAnalytics.NewDesignEvent(eventId);
    }
    public static void DesignEvent(string eventId, float value)
    {
        GameAnalytics.NewDesignEvent(eventId, value);
    }
    #region GAIDs

    public static void SetGAIds()
    {

        for (; 0< GameAnalytics.SettingsGA.Platforms.Count; )
        {
            GameAnalytics.SettingsGA.RemovePlatformAtIndex(0);
        }

        if (ArcadiaSdkManager.myGameIds.platform=="Android")
        {
            GameAnalytics.SettingsGA.AddPlatform(RuntimePlatform.Android);
        }
        else if (ArcadiaSdkManager.myGameIds.platform=="IOS")
        {
            GameAnalytics.SettingsGA.AddPlatform(RuntimePlatform.IPhonePlayer);
        }
        
        GameAnalytics.SettingsGA.UpdateGameKey(0,ArcadiaSdkManager.myGameIds.gameKey_GameAnaytics);
        GameAnalytics.SettingsGA.UpdateSecretKey(0,ArcadiaSdkManager.myGameIds.secretKey_GameAnaytics);
        
        GameAnalytics.SettingsGA.SubmitFpsAverage = true;
        GameAnalytics.SettingsGA.SubmitFpsCritical = true;
        GameAnalytics.SettingsGA.NativeErrorReporting = true;
        GameAnalytics.SettingsGA.SubmitErrors = true;
        GameAnalytics.SettingsGA.InfoLogBuild = true;
        GameAnalytics.SettingsGA.InfoLogEditor = true;
        GameAnalytics.SettingsGA.UsePlayerSettingsBuildNumber = true;
        GameAnalytics.SettingsGA.FpsCriticalThreshold = 30;
            
    }

    #endregion
}
