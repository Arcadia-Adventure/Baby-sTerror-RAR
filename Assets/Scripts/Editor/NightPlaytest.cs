using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Play any night straight from the editor, through the real Loading scene.</summary>
public class NightPlaytest : EditorWindow
{
    const string LoadingScene = "Assets/Scene/Loading.unity";
    const string ActiveKey = "BabysTerror.PlaytestActive";
    int _night = 1;

    [MenuItem("Baby's Terror/20 Nights/Playtest Night...")]
    static void Open() => GetWindow<NightPlaytest>("Playtest Night").minSize = new Vector2(260f, 70f);

    void OnGUI()
    {
        _night = EditorGUILayout.IntSlider("Night", _night, 1, Mathf.Max(1, LevelConfigLoader.LevelCount));
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button($"Play Night {_night}"))
                Play(_night);
        }
    }

    /// <summary>Starts play mode from the Loading scene and opens the night once the main menu is up.</summary>
    public static void Play(int night)
    {
        if (EditorApplication.isPlaying)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorPrefs.SetInt(EditorNightLauncher.PrefKey, night);
        SessionState.SetBool(ActiveKey, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoadingScene);
        EditorApplication.isPlaying = true;
    }

    // Static subscriptions don't survive the domain reload on entering play mode, so the
    // start-scene override is undone from here, re-registered after every reload.
    [InitializeOnLoadMethod]
    static void RegisterRestore()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false))
                return;

            SessionState.EraseBool(ActiveKey);
            EditorSceneManager.playModeStartScene = null;
        };
    }
}
