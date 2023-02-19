using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveController : MonoBehaviour
{
    public static ObjectiveController instance; 
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }    
    }

    public TextMeshProUGUI levelnoTxt, missionNameTxt;
    public TextMeshProUGUI[] taskTxt;
    public Color completeTaskColor;

    public void Start()
    {
        SetObjective();
    }
    public void SetObjective()
    {
        levelnoTxt.text = Objtive[GameManager.instance.selectedLevel - 1].levelNO;
        missionNameTxt.text = Objtive[GameManager.instance.selectedLevel - 1].missionName;
        for (int i = 0; i < Objtive[GameManager.instance.selectedLevel-1].Tasks.Length; i++)
        {
            taskTxt[i].gameObject.SetActive(true);
            taskTxt[i].text = Objtive[GameManager.instance.selectedLevel - 1].Tasks[i];
        }
    }
    public void UpdateTask(int taskNo)
    {
        if(taskTxt[taskNo-1].text.Contains("complete"))
        {
            return;
        }

        taskTxt[taskNo-1].text = taskTxt[taskNo-1].text + "(complete)";
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



    public Objective[] Objtive;
    [System.Serializable]
    public class Objective
    {
        public string levelNO;
        public string missionName;
        [TextArea(3,6)]
        public string[] Tasks;
    }
}
