using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

#if gameanalytics_enabled
using GameAnalyticsSDK;
#endif
public class AppOpenAdController : MonoBehaviour
{
    private static AppOpenAdController _agent;
    public static AppOpenAdController agent
    {
        get
        {
            if (_agent == null)
            {
                _agent = new GameObject(nameof(AppOpenAdController)).AddComponent<AppOpenAdController>();
                DontDestroyOnLoad(_agent.gameObject);
            }
            return _agent;
        }
    }

    void Awake()
    {
        if (_agent == null)
        {
            _agent = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromMinutes(1);
    private DateTime appOpenExpireTime;
    private AppOpenAd appOpenAd;
    
    public bool IsAppOpenAdAvailable => appOpenAd != null && appOpenAd.CanShowAd() && DateTime.Now < appOpenExpireTime;

    void RequestAndLoadAppOpenAd(bool useTestIDs = false)
    {
        string adUnitId = useTestIDs ? "ca-app-pub-3940256099942544/3419835294" : ArcadiaSdkManager.myGameIds.appOpenAdId;

        DestroyAppOpenAd();

        AppOpenAd.Load(adUnitId, new AdRequest(),
            (AppOpenAd ad, LoadAdError loadError) =>
            {
                if (loadError != null || ad == null)
                {
                    ArcadiaSdkManager.PrintStatus("App open ad failed to load: " + loadError?.GetMessage());
                    return;
                }

                this.appOpenAd = ad;
                this.appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                ad.OnAdFullScreenContentClosed += () => RequestAndLoadAppOpenAd();
            });
    }

    public void DestroyAppOpenAd()
    {
        appOpenAd?.Destroy();
        appOpenAd = null;
    }

    public void ShowAppOpenAd()
    {
        if (!IsAppOpenAdAvailable)
        {
            RequestAndLoadAppOpenAd();
            return;
        }

        appOpenAd.Show();
        DestroyAppOpenAd();
    }
}
