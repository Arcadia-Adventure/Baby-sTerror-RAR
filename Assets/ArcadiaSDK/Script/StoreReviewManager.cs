using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using Google.Play.Review;
#endif

public class StoreReviewManager : MonoBehaviour
{
#if UNITY_ANDROID
    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;
#endif
    public void RateUs()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            _reviewManager = new ReviewManager();
            StartCoroutine(Review());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StoreReviewManager] In-app review unavailable: {e.Message}");
        }
#endif
#if UNITY_IOS || UNITY_IPHONE
        UnityEngine.iOS.Device.RequestStoreReview();
#endif
    }

    IEnumerator Review()
    {
        yield return new WaitForSeconds(.1f);
#if UNITY_ANDROID
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogError($"[StoreReviewManager] RequestReviewFlow failed: {requestFlowOperation.Error}");
            yield break;
        }
        _playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;
        _playReviewInfo = null;
        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogError($"[StoreReviewManager] LaunchReviewFlow failed: {launchFlowOperation.Error}");
            yield break;
        }
        Debug.Log("[StoreReviewManager] Review flow completed successfully");
#endif
    }
}
