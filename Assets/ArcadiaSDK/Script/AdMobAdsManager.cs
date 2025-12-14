#if UNITY_ADMOB
using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobAdsManager : MonoBehaviour, IAdsManager
{
    private static AdMobAdsManager _instance;
    private bool _isInitialized = false;
    
    // Simple single ad references (no dictionaries)
    private BannerView _bannerAd;
    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;
    private AppOpenAd _appOpenAd;
    private BannerView _mrecAd;
    
    // Store ad unit IDs
    private string _bannerAdUnitId;
    private string _interstitialAdUnitId;
    private string _rewardedAdUnitId;
    private string _appOpenAdUnitId;
    private string _mrecAdUnitId;
    
    // Callbacks
    private Action<int> _currentRewardedCallback;
    private Action _currentRewardedFailCallback;
    private Action _currentInterstitialSuccessCallback;
    private Action _currentInterstitialFailCallback;
    private Action _currentAppOpenSuccessCallback;
    private Action _currentAppOpenFailCallback;

    public static AdMobAdsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AdMobAdsManager");
                _instance = go.AddComponent<AdMobAdsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    public bool IsInitialized => _isInitialized;
    
    // Events
    public event Action OnAdsInitialized;
    public event Action<string> OnAdLoaded;
    public event Action<string, string> OnAdFailedToLoad;
    public event Action<string> OnAdShown;
    public event Action<string> OnAdClosed;
    public event Action<string> OnAdClicked;
    public event Action<string, int> OnRewardedAdRewarded;
    public event Action<string, double> OnAdRevenuePaid;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    #region Initialization
    
    public void Initialize(string appId, bool enableLogs = false)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[AdMob] SDK is already initialized.");
            return;
        }
        
        Debug.Log("[AdMob] SDK initialization started...");
        
        // Initialize AdMob SDK
        MobileAds.Initialize((InitializationStatus initializationStatus) =>
        {
            Dictionary<string, AdapterStatus> map = initializationStatus.getAdapterStatusMap();
            foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
            {
                string className = keyValuePair.Key;
                AdapterStatus status = keyValuePair.Value;
                switch (status.InitializationState)
                {
                    case AdapterState.NotReady:
                        Debug.Log($"[AdMob] Adapter: {className} is not ready.");
                        break;
                    case AdapterState.Ready:
                        Debug.Log($"[AdMob] Adapter: {className} is initialized.");
                        break;
                }
            }
            
            _isInitialized = true;
            OnAdsInitialized?.Invoke();
            Debug.Log("[AdMob] SDK initialized successfully.");
        });
    }
    
    #endregion
    
    #region Banner Ads
    
    public void LoadBanner(string adUnitId, BannerPosition position = BannerPosition.Bottom)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AdMob] SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing banner
        DestroyBanner(adUnitId);
        
        _bannerAdUnitId = adUnitId;
        
        Debug.Log($"[AdMob] Loading Banner: {adUnitId}");
        
        _bannerAd = new BannerView(adUnitId, AdSize.Banner, ConvertBannerPosition(position));
        
        _bannerAd.OnBannerAdLoaded += () => 
        {
            Debug.Log("[AdMob] Banner loaded successfully.");
            OnAdLoaded?.Invoke(adUnitId);
        };
        _bannerAd.OnBannerAdLoadFailed += (LoadAdError error) => 
        {
            Debug.LogError($"[AdMob] Banner failed to load: {error.GetMessage()}");
            OnAdFailedToLoad?.Invoke(adUnitId, error.GetMessage());
        };
        _bannerAd.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
        _bannerAd.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        
        _bannerAd.LoadAd(new AdRequest());
    }
    
    public void ShowBanner(string adUnitId)
    {
        if (_bannerAd != null)
        {
            _bannerAd.Show();
            Debug.Log("[AdMob] Banner shown.");
        }
        else
        {
            Debug.LogWarning("[AdMob] Banner is not loaded.");
        }
    }
    
    public void HideBanner(string adUnitId)
    {
        if (_bannerAd != null)
        {
            _bannerAd.Hide();
            Debug.Log("[AdMob] Banner hidden.");
        }
    }
    
    public void DestroyBanner(string adUnitId)
    {
        if (_bannerAd != null)
        {
            _bannerAd.Destroy();
            _bannerAd = null;
            Debug.Log("[AdMob] Banner destroyed.");
        }
    }
    
    public bool IsBannerLoaded(string adUnitId)
    {
        return _bannerAd != null;
    }
    
    #endregion
    
    #region Interstitial Ads
    
    public void LoadInterstitial(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AdMob] SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing interstitial
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }
        
        _interstitialAdUnitId = adUnitId;
        
        Debug.Log($"[AdMob] Loading Interstitial: {adUnitId}");
        
        InterstitialAd.Load(adUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMob] Interstitial failed to load: {error?.GetMessage()}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            Debug.Log("[AdMob] Interstitial loaded successfully.");
            _interstitialAd = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register events
            _interstitialAd.OnAdFullScreenContentOpened += () => 
            {
                Debug.Log("[AdMob] Interstitial opened.");
                OnAdShown?.Invoke(adUnitId);
            };
            _interstitialAd.OnAdFullScreenContentClosed += () => 
            {
                Debug.Log("[AdMob] Interstitial closed.");
                OnAdClosed?.Invoke(adUnitId);
                _currentInterstitialSuccessCallback?.Invoke();
                _currentInterstitialSuccessCallback = null;
                _currentInterstitialFailCallback = null;
                
                // Destroy after showing (with null check to prevent race conditions)
                if (_interstitialAd != null)
                {
                    _interstitialAd.Destroy();
                    _interstitialAd = null;
                }
            };
            _interstitialAd.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                Debug.LogError($"[AdMob] Interstitial failed to show: {adError.GetMessage()}");
                _currentInterstitialFailCallback?.Invoke();
                _currentInterstitialSuccessCallback = null;
                _currentInterstitialFailCallback = null;
                _interstitialAd = null;
            };
            _interstitialAd.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            _interstitialAd.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowInterstitial(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsInterstitialLoaded(adUnitId))
        {
            _currentInterstitialSuccessCallback = onSuccess;
            _currentInterstitialFailCallback = onFail;
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMob] Interstitial is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsInterstitialLoaded(string adUnitId)
    {
        return _interstitialAd != null && _interstitialAd.CanShowAd();
    }
    
    #endregion
    
    #region Rewarded Ads
    
    public void LoadRewarded(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AdMob] SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing rewarded ad
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
        
        _rewardedAdUnitId = adUnitId;
        
        Debug.Log($"[AdMob] Loading Rewarded: {adUnitId}");
        
        RewardedAd.Load(adUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMob] Rewarded ad failed to load: {error?.GetMessage()}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            Debug.Log("[AdMob] Rewarded ad loaded successfully.");
            _rewardedAd = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register events
            _rewardedAd.OnAdFullScreenContentOpened += () => 
            {
                Debug.Log("[AdMob] Rewarded ad opened.");
                OnAdShown?.Invoke(adUnitId);
            };
            _rewardedAd.OnAdFullScreenContentClosed += () => 
            {
                    Debug.Log("[AdMob] Rewarded ad closed.");
                    OnAdClosed?.Invoke(adUnitId);
                    
                    // Destroy after showing
                    if (_rewardedAd != null)
                    {
                        _rewardedAd.Destroy();
                        _rewardedAd = null;
                    }
            };
            _rewardedAd.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                Debug.LogError($"[AdMob] Rewarded ad failed to show: {adError.GetMessage()}");
                _currentRewardedFailCallback?.Invoke();
                _currentRewardedCallback = null;
                _currentRewardedFailCallback = null;
                _rewardedAd = null;
            };
            _rewardedAd.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            _rewardedAd.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowRewarded(string adUnitId, Action<int> onSuccess = null, Action onFail = null)
    {
        if (IsRewardedLoaded(adUnitId))
        {
            _currentRewardedCallback = onSuccess;
            _currentRewardedFailCallback = onFail;
            
            _rewardedAd.Show((Reward reward) =>
            {
                    Debug.Log($"[AdMob] Rewarded ad completed. Reward: {reward.Amount}");
                    OnRewardedAdRewarded?.Invoke(adUnitId, (int)reward.Amount);
                    _currentRewardedCallback?.Invoke((int)reward.Amount);
                    _currentRewardedCallback = null;
                    _currentRewardedFailCallback = null;
            });
        }
        else
        {
            Debug.LogWarning("[AdMob] Rewarded ad is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsRewardedLoaded(string adUnitId)
    {
        return _rewardedAd != null && _rewardedAd.CanShowAd();
    }
    
    #endregion
    
    #region MRec Ads
    
    public void LoadMRec(string adUnitId, BannerPosition position = BannerPosition.Center)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AdMob] SDK not initialized. Call Initialize() first.");
            return;
        }
        
        DestroyMRec(adUnitId);
        
        _mrecAdUnitId = adUnitId;
        
        Debug.Log($"[AdMob] Loading MRec: {adUnitId}");
        
        _mrecAd = new BannerView(adUnitId, AdSize.MediumRectangle, ConvertBannerPosition(position));
        
        _mrecAd.OnBannerAdLoaded += () => 
        {
            Debug.Log("[AdMob] MRec loaded successfully.");
            OnAdLoaded?.Invoke(adUnitId);
        };
        _mrecAd.OnBannerAdLoadFailed += (LoadAdError error) => 
        {
            Debug.LogError($"[AdMob] MRec failed to load: {error.GetMessage()}");
            OnAdFailedToLoad?.Invoke(adUnitId, error.GetMessage());
        };
        _mrecAd.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
        _mrecAd.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        
        _mrecAd.LoadAd(new AdRequest());
    }
    
    public void ShowMRec(string adUnitId)
    {
        if (_mrecAd != null)
        {
            _mrecAd.Show();
            Debug.Log("[AdMob] MRec shown.");
        }
        else
        {
            Debug.LogWarning("[AdMob] MRec is not loaded.");
        }
    }
    
    public void HideMRec(string adUnitId)
    {
        if (_mrecAd != null)
        {
            _mrecAd.Hide();
            Debug.Log("[AdMob] MRec hidden.");
        }
    }
    
    public void DestroyMRec(string adUnitId)
    {
        if (_mrecAd != null)
        {
            _mrecAd.Destroy();
            _mrecAd = null;
            Debug.Log("[AdMob] MRec destroyed.");
        }
    }
    
    public bool IsMRecLoaded(string adUnitId)
    {
        return _mrecAd != null;
    }
    
    #endregion
    
    #region App Open Ads
    
    public void LoadAppOpen(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AdMob] SDK not initialized. Call Initialize() first.");
            return;
        }
        
        if (_appOpenAd != null)
        {
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }
        
        _appOpenAdUnitId = adUnitId;
        
        Debug.Log($"[AdMob] Loading App Open: {adUnitId}");
        
        AppOpenAd.Load(adUnitId, new AdRequest(), (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMob] App Open ad failed to load: {error?.GetMessage()}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            Debug.Log("[AdMob] App Open ad loaded successfully.");
            _appOpenAd = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register events
            _appOpenAd.OnAdFullScreenContentOpened += () => 
            {
                Debug.Log("[AdMob] App Open ad opened.");
                OnAdShown?.Invoke(adUnitId);
            };
            _appOpenAd.OnAdFullScreenContentClosed += () => 
            {
                Debug.Log("[AdMob] App Open ad closed.");
                OnAdClosed?.Invoke(adUnitId);
                _currentAppOpenSuccessCallback?.Invoke();
                _currentAppOpenSuccessCallback = null;
                _currentAppOpenFailCallback = null;
                
                // Destroy after showing (with null check to prevent race conditions)
                if (_appOpenAd != null)
                {
                    _appOpenAd.Destroy();
                    _appOpenAd = null;
                }
            };
            _appOpenAd.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                Debug.LogError($"[AdMob] App Open ad failed to show: {adError.GetMessage()}");
                _currentAppOpenFailCallback?.Invoke();
                _currentAppOpenSuccessCallback = null;
                _currentAppOpenFailCallback = null;
                _appOpenAd = null;
            };
            _appOpenAd.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            _appOpenAd.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowAppOpen(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsAppOpenLoaded(adUnitId))
        {
            _currentAppOpenSuccessCallback = onSuccess;
            _currentAppOpenFailCallback = onFail;
            _appOpenAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMob] App Open ad is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsAppOpenLoaded(string adUnitId)
    {
        return _appOpenAd != null && _appOpenAd.CanShowAd();
    }
    
    #endregion
    
    #region Helper Methods
    
    private GoogleMobileAds.Api.AdPosition ConvertBannerPosition(BannerPosition position)
    {
        switch (position)
        {
            case BannerPosition.Top:
                return GoogleMobileAds.Api.AdPosition.Top;
            case BannerPosition.Bottom:
                return GoogleMobileAds.Api.AdPosition.Bottom;
            case BannerPosition.TopLeft:
                return GoogleMobileAds.Api.AdPosition.TopLeft;
            case BannerPosition.TopRight:
                return GoogleMobileAds.Api.AdPosition.TopRight;
            case BannerPosition.BottomLeft:
                return GoogleMobileAds.Api.AdPosition.BottomLeft;
            case BannerPosition.BottomRight:
                return GoogleMobileAds.Api.AdPosition.BottomRight;
            case BannerPosition.Center:
                return GoogleMobileAds.Api.AdPosition.Center;
            default:
                return GoogleMobileAds.Api.AdPosition.Bottom;
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        _bannerAd?.Destroy();
        _interstitialAd?.Destroy();
        _rewardedAd?.Destroy();
        _appOpenAd?.Destroy();
        _mrecAd?.Destroy();
    }
}
#endif
