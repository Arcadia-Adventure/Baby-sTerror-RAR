using System.Collections.Generic;
using Ommy.Prefs;
using Ommy.Singleton;
using SickscoreGames.HUDNavigationSystem;
using UnityEngine;

public class HintManager : Singleton<HintManager>
{
    public List<LevelObject> levelObjects;
    [SerializeField] RingbufferFootSteps footStepTrail;

    protected override void Awake()
    {
        base.Awake();
        if (footStepTrail == null)
            footStepTrail = FindFirstObjectByType<RingbufferFootSteps>(FindObjectsInactive.Include);
        DeactiveAllIndicators();
    }

    void OnEnable() => ObjectiveUIController.OnTaskReceived += OnTaskReceived;
    void OnDisable() => ObjectiveUIController.OnTaskReceived -= OnTaskReceived;

    void OnTaskReceived(TaskType _) => UpdateHint();

    public void ShowCurrentHint()
    {
        int level = GamePreference.selectedLevel - 1;
        int task = ObjectiveUIController.Instance.CurrentTaskIndex;
        ActivateIndicator(level, task);
        StartTrailToHint(level, task);
    }

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
        if (levelObjects == null)
            return;

        foreach (var levelObject in levelObjects)
        {
            if (levelObject?.levelTasks == null)
                continue;

            foreach (var levelTask in levelObject.levelTasks)
            {
                if (levelTask.indicator != null)
                    levelTask.indicator.SetActive(false);
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

    GameObject GetIndicator(int level, int task)
    {
        if (levelObjects == null || level < 0 || level >= levelObjects.Count)
            return null;

        var tasks = levelObjects[level].levelTasks;
        if (tasks == null || task < 0 || task >= tasks.Count)
            return null;

        return tasks[task].indicator;
    }
}

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
