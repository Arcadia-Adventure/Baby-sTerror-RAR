using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ommy.Singleton;

public class ObjectiveUIController : Singleton<ObjectiveUIController>
{
    [SerializeField] private TextMeshProUGUI levelnoTxt;
    [SerializeField] private TextMeshProUGUI missionNameTxt;
    [SerializeField] private TextMeshProUGUI[] taskTxt;
    [SerializeField] private Color completeTaskColor;

    private int totalTaskCount;
    private List<TaskInfo> taskInfos;

    public void Initialize(ObjectiveManager.LevelTasks levelTasks)
    {
        levelnoTxt.text = levelTasks.levelNO;
        missionNameTxt.text = levelTasks.missionName;
        totalTaskCount = levelTasks.taskInfos.Count;
        taskInfos = levelTasks.taskInfos;

        for (int i = 0; i < totalTaskCount; i++)
        {
            taskTxt[i].text = levelTasks.taskInfos[i].description;
            taskTxt[i].gameObject.SetActive(i == 0);
        }
    }

    public void UpdateTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= totalTaskCount) return;

        print($"Updating Task [{taskIndex}]: {taskInfos[taskIndex].description}");

        taskTxt[taskIndex].text = $"{taskInfos[taskIndex].description} (complete)";
        taskTxt[taskIndex].color = completeTaskColor;
        taskTxt[taskIndex].gameObject.SetActive(true);

        // Reveal the next pending task that isn't visible yet
        for (int i = 0; i < totalTaskCount; i++)
        {
            if (!taskInfos[i].isCompleted && !taskTxt[i].gameObject.activeSelf)
            {
                taskTxt[i].gameObject.SetActive(true);
                break;
            }
        }
    }
}
