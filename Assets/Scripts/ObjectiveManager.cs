using System;
using System.Collections.Generic;
using UnityEngine;
using Ommy.Singleton;
using Ommy.Prefs;
using Ommy.Attributes;
public class ObjectiveManager : Singleton<ObjectiveManager>
{
    [Serializable]
    public class LevelTasks
    {
        public List<TaskType> taskTypes;
    }
    public List<LevelTasks> levelTasks;
    public int TotalTasks;
    [ShowInInspector("Current Task Index")]
    public int currentTaskIndex => TotalTasks-pendingTasks;
    [ShowInInspector("Pending Tasks")]
    public int pendingTasks => currentLevelTasks.taskTypes.Count;
    public LevelTasks currentLevelTasks;
    public ObjectiveUIController objectiveUIController;
    public void Start()
    {
        currentLevelTasks = levelTasks[GamePreference.selectedLevel - 1];
        TotalTasks = currentLevelTasks.taskTypes.Count;
    }
    void OnTaskCompleted(TaskType taskType)
    {
        currentLevelTasks.taskTypes.Remove(taskType);
        objectiveUIController.UpdateTask(currentTaskIndex);
        if(currentLevelTasks.taskTypes.Count == 0)
        {
            GamePlayManager.Instance.LevelComplete();
        }
    }
    public static void OnTaskEventReceived(TaskType taskType)
    {
        Instance.OnTaskCompleted(taskType);
    }
}