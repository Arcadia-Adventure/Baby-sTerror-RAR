//#if UNITY_EDITOR || DEVELOPMENT_BUILD

using IngameDebugConsole;
using UnityEngine;

public static class LogsSetting
{
	static GameObject fpsLabelGO;
	static GameObject ingamedebuger;
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void OnAfterSceneLoadRuntimeMethod()
	{
		Debug.Log($"Logs are now {(ArcadiaSdkManager.Agent.GetLog() ? "enabled" : "disabled")}");
		ShowLogs(ArcadiaSdkManager.Agent.GetLog());
	}
	public static void ToggleLogs()
	{
		ShowLogs(!ArcadiaSdkManager.Agent.GetLog());
	}
	public static void ShowLogs(bool value)
	{
		ArcadiaSdkManager.Agent.SetLog(value);
		if (value)
		{
			fpsLabelGO = GameObject.FindFirstObjectByType<FPSLabel>()?.gameObject;
			ingamedebuger = GameObject.FindFirstObjectByType<DebugLogManager>(FindObjectsInactive.Include)?.gameObject;
			if (fpsLabelGO == null)
			{
				fpsLabelGO = new GameObject("FPS Label");
				fpsLabelGO.AddComponent<FPSLabel>();
			}
			if (ingamedebuger) ingamedebuger.SetActive(true);
			if (fpsLabelGO) fpsLabelGO.SetActive(true);
		}
		else
		{
			fpsLabelGO?.SetActive(false);
			ingamedebuger?.SetActive(false);
		}
	}
}
//#endif