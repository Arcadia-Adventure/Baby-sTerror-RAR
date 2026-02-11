using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Ommy.Singleton;
using Ommy.Prefs;
using Ommy.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public TasksDetail tasksDetail; // for mapping level
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
    private void Start()
    {

        CurrentLevelTasks = levelsTasks[GamePreference.selectedLevel - 1];
        TotalTasks = CurrentLevelTasks.taskInfos.Count;
        objectiveUIController.Initialize(CurrentLevelTasks);
    }

    public static void OnTaskEventReceived(TaskType taskType)
    {
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

    private void OnTaskCompleted(TaskType taskType)
    {
        if (!TryGetTaskInfo(taskType, out var taskInfo)) return;
        
        taskInfo.isCompleted = true;
        taskInfo.OnComplete?.Invoke(taskType);
        
        OnTaskReceived?.Invoke(taskType);
        objectiveUIController.UpdateTask(taskInfo);

        if (IsLevelCompleted()) 
            GamePlayManager.Instance.LevelComplete();
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
    [TextArea(2, 6)]
    public string description;
    public UnityEvent<TaskType> OnComplete;
}