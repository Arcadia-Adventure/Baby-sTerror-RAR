using System.Collections;
using System.Collections.Generic;
using Ommy.Prefs;
using Ommy.Singleton;
using SickscoreGames.HUDNavigationSystem;
using UnityEngine;

public class HintManager : Singleton<HintManager>
{
    // Legacy: read once by Baby's Terror > Migrate Scenes To Level System. Hints now live on each LevelSceneSetup.
    [HideInInspector] public List<LevelObject> levelObjects;
    [SerializeField] RingbufferFootSteps footStepTrail;

    LevelSceneSetup[] _setups;

    protected override void Awake()
    {
        base.Awake();
        if (footStepTrail == null)
            footStepTrail = FindFirstObjectByType<RingbufferFootSteps>(FindObjectsInactive.Include);
        _setups = LevelSceneSetup.FindAll();
        DeactiveAllIndicators();
    }

    IEnumerator Start()
    {
        // The trail clears itself in its own Start, so wait for that before starting it.
        yield return null;
        StartAutoTrail();
    }

    void OnEnable() => ObjectiveUIController.OnTaskReceived += OnTaskReceived;
    void OnDisable() => ObjectiveUIController.OnTaskReceived -= OnTaskReceived;

    void OnTaskReceived(TaskType _)
    {
        UpdateHint();
        StartAutoTrail();
    }

    void StartAutoTrail()
    {
        var levelData = LevelConfigLoader.GetLevelData(GamePreference.selectedLevel);
        if (levelData == null || !levelData.showFootstepTrail || !HasCurrentHint())
            return;

        StartTrailToHint(GamePreference.selectedLevel - 1, ObjectiveUIController.Instance.CurrentTaskIndex);
    }

    public void ShowCurrentHint()
    {
        int level = GamePreference.selectedLevel - 1;
        int task = ObjectiveUIController.Instance.CurrentTaskIndex;
        ActivateIndicator(level, task);
        StartTrailToHint(level, task);
    }

    public bool HasCurrentHint() =>
        GetIndicator(GamePreference.selectedLevel - 1, ObjectiveUIController.Instance.CurrentTaskIndex) != null;

    public bool IsCurrentHintActive()
    {
        int level = GamePreference.selectedLevel - 1;
        int task = ObjectiveUIController.Instance.CurrentTaskIndex;
        return IsIndicatorActivated(level, task) || (footStepTrail != null && footStepTrail.IsTrailing);
    }

    public void UpdateHint()
    {
        DeactiveAllIndicators();
        if (footStepTrail != null)
            footStepTrail.StopTrail();
    }

    public void ActivateIndicator(int level, int task = 0)
    {
        DeactiveAllIndicators();
        var indicator = GetIndicator(level, task);
        if (indicator != null)
            indicator.SetActive(true);
    }

    public bool IsIndicatorActivated(int level, int task = 0)
    {
        var indicator = GetIndicator(level, task);
        return indicator != null && indicator.activeSelf;
    }

    public void DeactiveAllIndicators()
    {
        if (_setups == null)
            return;

        foreach (LevelSceneSetup setup in _setups)
        {
            if (setup == null || setup.taskHints == null)
                continue;

            foreach (GameObject indicator in setup.taskHints)
            {
                if (indicator != null)
                    indicator.SetActive(false);
            }
        }
    }

    void StartTrailToHint(int level, int task)
    {
        if (footStepTrail == null)
            return;

        var destination = GetHintDestination(level, task);
        if (destination == null)
        {
            Debug.LogWarning($"[HintManager] No HUD navigation destination for level {level + 1}, task {task}.");
            footStepTrail.StopTrail();
            return;
        }

        footStepTrail.StartTrail(destination);
    }

    Transform GetHintDestination(int level, int task)
    {
        var indicator = GetIndicator(level, task);
        if (indicator == null)
            return null;

        var hud = indicator.GetComponent<HUDNavigationElement>()
            ?? indicator.GetComponentInChildren<HUDNavigationElement>(true);
        return hud != null ? hud.transform : indicator.transform;
    }

    /// <param name="level">Zero-based, so level 1 is 0.</param>
    GameObject GetIndicator(int level, int task)
    {
        if (_setups == null)
            return null;

        foreach (LevelSceneSetup setup in _setups)
        {
            if (setup != null && setup.LevelNumber == level + 1)
                return setup.GetTaskHint(task);
        }

        return null;
    }
}

/// <summary>Legacy per-level hint list, kept only so the scene migration can read it.</summary>
[System.Serializable]
public class LevelObject
{
    public List<LevelTask> levelTasks;

    [System.Serializable]
    public class LevelTask
    {
        public GameObject indicator;
    }
}
