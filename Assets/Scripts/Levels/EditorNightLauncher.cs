#if UNITY_EDITOR
using Ommy.Prefs;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor only. Lets "Baby's Terror > 20 Nights > Playtest Night" jump straight into a night:
/// play starts from the Loading scene (so the audio and SDK managers exist as in a real launch),
/// and once the main menu loads the chosen night is opened.
/// </summary>
public static class EditorNightLauncher
{
    public const string PrefKey = "BabysTerror.PlaytestNight";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        int night = UnityEditor.EditorPrefs.GetInt(PrefKey, 0);
        if (night <= 0)
            return;

        // One shot: a normal Play afterwards behaves normally.
        UnityEditor.EditorPrefs.DeleteKey(PrefKey);

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu")
                return;

            SceneManager.sceneLoaded -= OnLoaded;
            GamePreference.selectedLevel = night;
            Debug.Log($"[EditorNightLauncher] Playtesting night {night}.");
            SceneManager.LoadScene("GamePlay");
        }

        SceneManager.sceneLoaded += OnLoaded;
        if (SceneManager.GetActiveScene().name == "MainMenu")
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
}
#endif
