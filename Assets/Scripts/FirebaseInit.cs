using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit instance;
    private bool firebaseinit;
    private void Awake()
    {
        instance = this;
        firebaseinit = false;
        DontDestroyOnLoad(this);
    }
    void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            firebaseinit = true;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {

                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    public void FireBase_Events(string parameter1, string parameter2, string parameter3)
    {
        if (firebaseinit)
        {
            print("send event");
            Firebase.Analytics.FirebaseAnalytics.LogEvent(parameter1, parameter2, parameter3);
        }
    }
}
