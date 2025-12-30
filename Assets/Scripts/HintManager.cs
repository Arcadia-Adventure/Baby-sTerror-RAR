using System.Collections;
using System.Collections.Generic;
using Ommy.Singleton;
using UnityEngine;

public class HintManager : Singleton<HintManager>
{
    public List<LevelObject> levelObjects;
    public void ShowCurrentHint()
    {
        ActivateIndicator(GameManager.Instance.selectedLevel-1, ObjectiveController.Instance.currentTask);
    }
    public bool IsCurrentHintActive()
    {
        return IsIndicatorActivated(GameManager.Instance.selectedLevel-1, ObjectiveController.Instance.currentTask);
    }
    public void ActivateIndicator(int level, int task=0)
    {
        DeactiveAllIndicators();
        levelObjects[level].levelTasks[task].indicator.SetActive(true);
    }
    public bool IsIndicatorActivated(int level, int task=0)
    {
        return levelObjects[level].levelTasks[task].indicator.activeSelf;
    }
    public void DeactiveAllIndicators()
    {
        foreach (var levelObject in levelObjects)
        {
            foreach (var levelTask in levelObject.levelTasks)
            {
                if(levelTask.indicator!=null) 
                    levelTask.indicator.SetActive(false);
            }
        }
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
