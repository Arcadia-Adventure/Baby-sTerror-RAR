using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Ommy.Singleton;
using Ommy.Prefs;
using Ommy.Attributes;

public class ObjectiveManager : Singleton<ObjectiveManager>
{
    [SerializeField] private List<LevelTasks> levelsTasks;
    [SerializeField] private ObjectiveUIController objectiveUIController;

    public int TotalTasks { get; private set; }
    public LevelTasks CurrentLevelTasks { get; private set; }

    [ShowInInspector("Current Task Index")]
    public int CurrentTaskIndex => TotalTasks - PendingTasks;

    [ShowInInspector("Pending Tasks")]
    public int PendingTasks => CurrentLevelTasks.taskInfos.Count(t => !t.isCompleted);

    public static event Action<TaskType> OnTaskReceived;

    public TasksDetail tasksDetail;
    [InspectorButton]
    public void MapTaskDetail()
    {
        levelsTasks.Clear();
        foreach (var objective in tasksDetail.Objectives)
        {
            LevelTasks levelTasks = new LevelTasks
            {
                levelNO = objective.levelNO,
                missionName = objective.missionName,
                taskInfos = new List<TaskInfo>()
            };
            levelsTasks.Add(levelTasks);
            foreach (var taskDesc in objective.Tasks)
            {
                TaskInfo taskInfo = new TaskInfo
                {
                    description = taskDesc,
                    isCompleted = false
                };
                levelTasks.taskInfos.Add(taskInfo);
            }
        }
    }

    float _lastObjectiveTime;
    bool _stallReported;
    const float StallThresholdSeconds = 120f;

    private void Start()
    {
        int levelIndex = GamePreference.selectedLevel - 1;

        var jsonData = LevelConfigLoader.GetLevelData(GamePreference.selectedLevel);
        if (jsonData != null && jsonData.tasks != null && jsonData.tasks.Length > 0)
        {
            CurrentLevelTasks = BuildFromJson(jsonData, levelIndex);
        }
        else if (levelIndex >= 0 && levelIndex < levelsTasks.Count)
        {
            CurrentLevelTasks = levelsTasks[levelIndex];
        }
        else
        {
            Debug.LogError($"[ObjectiveManager] No task data for level {GamePreference.selectedLevel}");
            CurrentLevelTasks = new LevelTasks
            {
                levelNO = GamePreference.selectedLevel.ToString(),
                missionName = "Unknown",
                taskInfos = new List<TaskInfo>()
            };
        }

        TotalTasks = CurrentLevelTasks.taskInfos.Count;
        objectiveUIController.Initialize(CurrentLevelTasks);
        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        StartCoroutine(StallDetectionLoop());
    }

    LevelTasks BuildFromJson(LevelData jsonData, int levelIndex)
    {
        var inspectorTasks = (levelIndex >= 0 && levelIndex < levelsTasks.Count)
            ? levelsTasks[levelIndex]
            : null;

        var levelTasks = new LevelTasks
        {
            levelNO = inspectorTasks?.levelNO ?? jsonData.level.ToString(),
            missionName = jsonData.missionName,
            taskInfos = new List<TaskInfo>()
        };

        for (int i = 0; i < jsonData.tasks.Length; i++)
        {
            var taskData = jsonData.tasks[i];
            var taskType = LevelConfigLoader.ParseTaskType(taskData.taskType);

            var newTask = new TaskInfo
            {
                taskType = taskType,
                description = taskData.description,
                completePreviousTasks = taskData.completePreviousTasks,
                isCompleted = false
            };

            if (inspectorTasks != null && i < inspectorTasks.taskInfos.Count)
            {
                var inspectorTask = inspectorTasks.taskInfos[i];
                newTask.OnComplete = inspectorTask.OnComplete;
            }

            levelTasks.taskInfos.Add(newTask);
        }

        return levelTasks;
    }

    public static void OnTaskEventReceived(TaskType taskType)
    {
        if (taskType == TaskType.None || Instance == null || Instance.CurrentLevelTasks == null) return;
        Instance.OnTaskCompleted(taskType);
    }

    public bool IsLevelCompleted()
    {
        return CurrentLevelTasks.taskInfos.All(t => t.isCompleted);
    }

    public bool TryGetTaskInfo(TaskType taskType, out TaskInfo taskInfo)
    {
        taskInfo = CurrentLevelTasks.taskInfos.Find(t => t.taskType == taskType);
        return taskInfo != null;
    }

    void OnTaskCompleted(TaskType taskType)
    {
        int taskIndex = CurrentLevelTasks.taskInfos.FindIndex(t => t.taskType == taskType && !t.isCompleted);
        if (taskIndex < 0) return;

        var taskInfo = CurrentLevelTasks.taskInfos[taskIndex];

        if (taskInfo.completePreviousTasks)
        {
            for (int i = 0; i < taskIndex; i++)
            {
                var priorTask = CurrentLevelTasks.taskInfos[i];
                if (!priorTask.isCompleted)
                {
                    priorTask.isCompleted = true;
                    priorTask.OnComplete?.Invoke(priorTask.taskType);
                    OnTaskReceived?.Invoke(priorTask.taskType);
                    objectiveUIController.UpdateTask(i);
                }
            }
        }

        taskInfo.isCompleted = true;
        taskInfo.OnComplete?.Invoke(taskType);
        OnTaskReceived?.Invoke(taskType);
        objectiveUIController.UpdateTask(taskIndex);

        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        AnalyticsTracker.OnObjectiveCompleted(taskIndex);
        AA_AnalyticsManager.Agent.TrackObjectiveCompleted(
            GamePreference.selectedLevel, taskIndex, taskType.ToString());

        if (IsLevelCompleted())
            GamePlayManager.Instance.LevelComplete();
    }

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

    [Serializable]
    public class LevelTasks
    {
        public string levelNO;
        public string missionName;
        public List<TaskInfo> taskInfos;
    }
}

[Serializable]
public class TaskInfo
{
    public bool isCompleted;
    public int requireLevel = -1;
    public TaskType taskType;
    [Tooltip("When enabled, completing this task will also mark all previous incomplete tasks as completed.")]
    public bool completePreviousTasks;
    [TextArea(2, 6)]
    public string description;
    public UnityEvent<TaskType> OnComplete;
}
