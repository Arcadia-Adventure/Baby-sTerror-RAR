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
    private int displayableTaskCount;
    private List<TaskInfo> taskInfos;

    public void Initialize(ObjectiveManager.LevelTasks levelTasks)
    {
        levelnoTxt.text = levelTasks.levelNO;
        missionNameTxt.text = levelTasks.missionName;
        totalTaskCount = levelTasks.taskInfos.Count;
        displayableTaskCount = Mathf.Min(totalTaskCount, taskTxt.Length);
        taskInfos = levelTasks.taskInfos;

        for (int i = 0; i < taskTxt.Length; i++)
        {
            if (i < totalTaskCount)
            {
                taskTxt[i].text = levelTasks.taskInfos[i].description;
                taskTxt[i].gameObject.SetActive(i == 0);
            }
            else
            {
                taskTxt[i].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= displayableTaskCount) return;

        taskTxt[taskIndex].text = $"{taskInfos[taskIndex].description} (complete)";
        taskTxt[taskIndex].color = completeTaskColor;
        taskTxt[taskIndex].gameObject.SetActive(true);

        for (int i = 0; i < displayableTaskCount; i++)
        {
            if (!taskInfos[i].isCompleted && !taskTxt[i].gameObject.activeSelf)
            {
                taskTxt[i].gameObject.SetActive(true);
                break;
            }
        }
    }
}
