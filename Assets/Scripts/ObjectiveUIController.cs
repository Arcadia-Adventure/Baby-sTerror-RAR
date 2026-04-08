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

    private int currentTaskIndex;
    private int totalTaskCount;

    public void Initialize(ObjectiveManager.LevelTasks levelTasks)
    {
        levelnoTxt.text = levelTasks.levelNO;
        missionNameTxt.text = levelTasks.missionName;
        totalTaskCount = levelTasks.taskInfos.Count;

        for (int i = 0; i < totalTaskCount; i++)
        {
            taskTxt[i].text = levelTasks.taskInfos[i].description;
            taskTxt[i].gameObject.SetActive(i == 0);
        }
        currentTaskIndex = 0;
    }

    public void UpdateTask(TaskInfo taskInfo)
    {
        int index = currentTaskIndex;
        print($"Updating Task: {taskInfo.description}");

        if (taskTxt[index].text.Contains("complete")) return;

        taskTxt[index].text = $"{taskInfo.description} (complete)";
        taskTxt[index].color = completeTaskColor;

        int nextIndex = index + 1;
        if (nextIndex < totalTaskCount && taskTxt[nextIndex] != null)
        {
            taskTxt[nextIndex].gameObject.SetActive(true);
        }
        currentTaskIndex++;
    }
}
