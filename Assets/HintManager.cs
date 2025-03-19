using System.Collections;
using System.Collections.Generic;
using Ommy.Singleton;
using UnityEngine;

public class HintManager : Singleton<HintManager>
{
    public List<LevelObject> levelObjects;
    public void ActivateIndicator(int level, int task=0)
    {
        DeactiveAllIndicators();
        levelObjects[level].levelTasks[task].indicator.SetActive(true);
    }
    public void DeactiveAllIndicators()
    {
        foreach (var levelObject in levelObjects)
        {
            foreach (var levelTask in levelObject.levelTasks)
            {
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
