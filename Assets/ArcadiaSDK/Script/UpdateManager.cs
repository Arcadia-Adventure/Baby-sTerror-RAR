#if UNITY_ANDROID
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif
using System.Collections;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
#if UNITY_ANDROID
    private AppUpdateManager appUpdateManager;
#endif

    private static bool _checkedThisSession;

    public void ShowAvailbleUpdate()
    {
        if (_checkedThisSession)
            return;

        _checkedThisSession = true;

#if UNITY_ANDROID
        appUpdateManager = new AppUpdateManager();
        StartCoroutine(CheckForUpdate());
#endif
    }
#if UNITY_ANDROID

    IEnumerator CheckForUpdate()
    {
        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation =
          appUpdateManager.GetAppUpdateInfo();

        yield return appUpdateInfoOperation;

        if (!appUpdateInfoOperation.IsSuccessful)
        {
            Debug.LogError($"[UpdateManager] GetAppUpdateInfo failed: {appUpdateInfoOperation.Error}");
            _checkedThisSession = false;
            yield break;
        }

        var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
        Debug.Log($"[UpdateManager] UpdateAvailability: {appUpdateInfoResult.UpdateAvailability}");

        if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.UpdateAvailable &&
            appUpdateInfoResult.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions()))
        {
            var appUpdateOptions = AppUpdateOptions.ImmediateAppUpdateOptions();
            StartCoroutine(StartImmediateUpdate(appUpdateInfoResult, appUpdateOptions));
        }
    }

    IEnumerator StartImmediateUpdate(AppUpdateInfo appUpdateInfo_i, AppUpdateOptions appUpdateOptions_i)
    {
        var startUpdateRequest = appUpdateManager.StartUpdate(appUpdateInfo_i, appUpdateOptions_i);
        yield return startUpdateRequest;
        Debug.Log("[UpdateManager] Update flow completed");
    }
#endif

}