using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ommy.Singleton;
using Ommy.Prefs;

public class ObjectiveUIController : Singleton<ObjectiveUIController>
{
    public TasksDetail tasksDetail;
    public TextMeshProUGUI levelnoTxt, missionNameTxt;
    public TextMeshProUGUI[] taskTxt;
    public Color completeTaskColor;
    public void Start()
    {
        SetObjective();
    }
    void SetObjective()
    {
        levelnoTxt.text = tasksDetail.Objectives[GamePreference.selectedLevel - 1].levelNO;
        missionNameTxt.text = tasksDetail.Objectives[GamePreference.selectedLevel - 1].missionName;
        for (int i = 0; i < tasksDetail.Objectives[GamePreference.selectedLevel-1].Tasks.Length; i++)
        {
            if(i==0)
            {
                taskTxt[i].gameObject.SetActive(true);
            }
            taskTxt[i].text = tasksDetail.Objectives[GamePreference.selectedLevel - 1].Tasks[i];
        }
    }
    public void UpdateTask(int taskNo)
    {
        if(taskTxt[taskNo-1].text.Contains("complete"))
        {
            return;
        }
        taskTxt[taskNo-1].text = taskTxt[taskNo-1].text + "  (complete)";
        if(taskNo < tasksDetail.Objectives[GamePreference.selectedLevel - 1].Tasks.Length)
        {
            taskTxt[taskNo].gameObject.SetActive(true);
        }
        //taskTxt[currentTask].text.Insert(taskTxt[currentTask].text.Length, " (Complete)"); 
        taskTxt[taskNo-1].color = completeTaskColor;
    }
    public IEnumerator WriteObjective(Text objectiveText , string text)
    {
        foreach (var item in text)
        {
            objectiveText.text += item;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
