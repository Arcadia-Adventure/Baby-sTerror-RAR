using System.Collections;
using System.Collections.Generic;
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
#if UNITY_ANDROID
        _reviewManager = new ReviewManager();
        StartCoroutine(Review());
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
