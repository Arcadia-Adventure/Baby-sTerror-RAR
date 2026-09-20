using IngameDebugConsole;
using Ommy.Prefs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Seeds the developer settings menu and opens <c>DevSettingsDialog</c> when
/// the main-menu Settings button or gameplay Pause button is held, or when F12
/// is pressed in the editor.
/// </summary>
public class DevSettingsBootstrap : MonoBehaviour
{
    private const string GameplayHeading = "Gameplay";
    private const string ConsoleHeading = "Console";
    private const string ConsolePrefKey = "dev_mobile_console";
    private const int HintGrantAmount = 5;

    private static bool entriesRegistered;
    private static DevSettingsBootstrap instance;
    private static GameObject dialogInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        RegisterEntries();
        if (instance != null) return;

        var go = new GameObject(nameof(DevSettingsBootstrap));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DevSettingsBootstrap>();
    }

    private static void RegisterEntries()
    {
        if (entriesRegistered) return;
        entriesRegistered = true;

        DevSettingsMenu.AddEntry(GameplayHeading, new DevSettingsTitledEntry(
            "Hints", () => GamePreference.hintCount.ToString()));
        DevSettingsMenu.AddEntry(GameplayHeading, new DevSettingsButton(
            "Increase Hints (+5)", IncreaseHints));
        DevSettingsMenu.AddEntry(GameplayHeading, new DevSettingsButton(
            "Unlock All Levels", UnlockAllLevels));

        DevSettingsMenu.AddEntry(ConsoleHeading,
            new DevSettingsToggle("Mobile Console", SetConsole, IsConsoleEnabled));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        AttachHoldTriggers();
        if (IsConsoleEnabled()) ShowConsole();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CloseDevSettings();
        AttachHoldTriggers();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F12))
            OpenDevSettings();
#endif
    }

    private void AttachHoldTriggers()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name != "Setting Button" && button.name != "PauseButton")
                continue;

            var hold = button.GetComponent<DebugButtonHold>();
            if (hold == null) hold = button.gameObject.AddComponent<DebugButtonHold>();
            hold.OnHoldComplete.RemoveListener(OpenDevSettings);
            hold.OnHoldComplete.AddListener(OpenDevSettings);
        }
    }

    private void OpenDevSettings()
    {
        if (dialogInstance != null) return;

        var prefab = Resources.Load<GameObject>("DevSettings");
        if (prefab == null)
        {
            Debug.LogError("[DevSettings] Resources/DevSettings prefab was not found.");
            return;
        }

        dialogInstance = Instantiate(prefab);
    }

    private static void CloseDevSettings()
    {
        if (dialogInstance == null) return;
        Destroy(dialogInstance);
        dialogInstance = null;
    }

    private static void IncreaseHints()
    {
        GamePreference.hintCount += HintGrantAmount;
        PlayerPrefs.Save();

        foreach (var hintButton in FindObjectsByType<HintButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            hintButton.UpdateHintCountText();
    }

    private static void UnlockAllLevels()
    {
        PlayerPrefs.SetInt(PrefKeys.UnlockAllLevels, 1);
        int levelCount = Mathf.Max(1, LevelConfigLoader.LevelCount);
        if (GamePreference.openLevels < levelCount)
            GamePreference.openLevels = levelCount;
        PlayerPrefs.Save();

        var levelSelect = FindFirstObjectByType<LevelSelectionManager>();
        if (levelSelect != null)
            levelSelect.OnPurchaseSuccess();
    }

    private static bool IsConsoleEnabled() => PlayerPrefs.GetInt(ConsolePrefKey, 0) == 1;

    private static void SetConsole(bool on)
    {
        if (on) ShowConsole();
        else HideConsole();
        PlayerPrefs.SetInt(ConsolePrefKey, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void ShowConsole()
    {
        if (DebugLogManager.Instance == null)
        {
            var prefab = Resources.Load<GameObject>("IngameDebugConsole");
            if (prefab != null)
                Instantiate(prefab);
        }

        if (DebugLogManager.Instance != null)
            DebugLogManager.Instance.PopupEnabled = true;
    }

    private static void HideConsole()
    {
        if (DebugLogManager.Instance == null) return;
        DebugLogManager.Instance.HideLogWindow();
        DebugLogManager.Instance.PopupEnabled = false;
    }
}
