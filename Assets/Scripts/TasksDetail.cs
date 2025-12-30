using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TasksDetails", menuName = "ScriptableObjects/TasksDetails", order = 1)]
public class TasksDetail : ScriptableObject
{
    public List<Objective> Objectives;
    [System.Serializable]
    public class Objective
    {
        public string levelNO;
        public string missionName;
        [TextArea(3,6)]
        public string[] Tasks;
    }
}
