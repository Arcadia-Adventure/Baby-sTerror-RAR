using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Ommy.Singleton;
using Ommy.Prefs;

public class ObjectiveUIController : Singleton<ObjectiveUIController>
{
    [SerializeField] private TextMeshProUGUI levelnoTxt;
    [SerializeField] private TextMeshProUGUI missionNameTxt;
    [SerializeField] private TextMeshProUGUI[] taskTxt;
    [SerializeField] private Color completeTaskColor;

    public int TotalTasks { get; private set; }
    public int CurrentTaskIndex => TotalTasks - PendingTasks;
    public int PendingTasks => _taskInfos.Count(t => !t.isCompleted);

    public static event Action<TaskType> OnTaskReceived;

    List<TaskInfo> _taskInfos = new();
    int _displayableCount;
    float _lastObjectiveTime;
    bool _stallReported;
    const float StallThresholdSeconds = 120f;

    private void Start()
    {
        var levelData = LevelConfigLoader.GetLevelData(GamePreference.selectedLevel);
        if (levelData == null || levelData.tasks == null || levelData.tasks.Length == 0)
        {
            Debug.LogError($"[ObjectiveUIController] No task data for level {GamePreference.selectedLevel}");
            return;
        }

        BuildTasks(levelData);
        InitializeUI(levelData);

        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        StartCoroutine(StallDetectionLoop());
    }

    void BuildTasks(LevelData levelData)
    {
        _taskInfos.Clear();
        foreach (var td in levelData.tasks)
        {
            _taskInfos.Add(new TaskInfo
            {
                taskType = LevelConfigLoader.ParseTaskType(td.taskType),
                description = td.description,
                completePreviousTasks = td.completePreviousTasks,
                isCompleted = false
            });
        }
        TotalTasks = _taskInfos.Count;
    }

    void InitializeUI(LevelData levelData)
    {
        levelnoTxt.text = "Night " + levelData.level;
        missionNameTxt.text = levelData.missionName;
        _displayableCount = Mathf.Min(TotalTasks, taskTxt.Length);

        for (int i = 0; i < taskTxt.Length; i++)
        {
            if (i < TotalTasks)
            {
                taskTxt[i].text = _taskInfos[i].description;
                taskTxt[i].gameObject.SetActive(i == 0);
            }
            else
            {
                taskTxt[i].gameObject.SetActive(false);
            }
        }
    }

    #region Task Completion

    public static void OnTaskEventReceived(TaskType taskType)
    {
        if (taskType == TaskType.None || Instance == null) return;
        Instance.CompleteTask(taskType);
    }

    void CompleteTask(TaskType taskType)
    {
        int taskIndex = _taskInfos.FindIndex(t => t.taskType == taskType && !t.isCompleted);
        if (taskIndex < 0) return;

        var taskInfo = _taskInfos[taskIndex];

        if (taskInfo.completePreviousTasks)
        {
            for (int i = 0; i < taskIndex; i++)
            {
                var prior = _taskInfos[i];
                if (!prior.isCompleted)
                {
                    prior.isCompleted = true;
                    OnTaskReceived?.Invoke(prior.taskType);
                    UpdateTaskUI(i);
                }
            }
        }

        taskInfo.isCompleted = true;
        OnTaskReceived?.Invoke(taskType);
        UpdateTaskUI(taskIndex);

        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        AnalyticsTracker.OnObjectiveCompleted(taskIndex);
        AA_AnalyticsManager.Agent.TrackObjectiveCompleted(
            GamePreference.selectedLevel, taskIndex, taskType.ToString());

        if (_taskInfos.All(t => t.isCompleted))
            GamePlayManager.Instance.LevelComplete();
    }

    void UpdateTaskUI(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= _displayableCount) return;

        taskTxt[taskIndex].text = $"{_taskInfos[taskIndex].description} (complete)";
        taskTxt[taskIndex].color = completeTaskColor;
        taskTxt[taskIndex].gameObject.SetActive(true);

        for (int i = 0; i < _displayableCount; i++)
        {
            if (!_taskInfos[i].isCompleted && !taskTxt[i].gameObject.activeSelf)
            {
                taskTxt[i].gameObject.SetActive(true);
                break;
            }
        }
    }

    #endregion

    #region Queries

    public bool TryGetTaskInfo(TaskType taskType, out TaskInfo taskInfo)
    {
        taskInfo = _taskInfos.Find(t => t.taskType == taskType);
        return taskInfo != null;
    }

    #endregion

    IEnumerator StallDetectionLoop()
    {
        var wait = new WaitForSecondsRealtime(30f);
        while (true)
        {
            yield return wait;
            if (_stallReported) continue;
            float elapsed = Time.realtimeSinceStartup - _lastObjectiveTime;
            if (elapsed >= StallThresholdSeconds)
            {
                _stallReported = true;
                AA_AnalyticsManager.Agent.TrackObjectiveStalled(
                    GamePreference.selectedLevel, CurrentTaskIndex, elapsed);
            }
        }
    }
}

[Serializable]
public class TaskInfo
{
    public bool isCompleted;
    public TaskType taskType;
    public bool completePreviousTasks;
    public string description;
}
