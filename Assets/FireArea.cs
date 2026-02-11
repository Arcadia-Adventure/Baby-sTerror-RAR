using System.Collections.Generic;
using UnityEngine;

public class FireArea : MonoBehaviour
{
    public TaskType onFireEndTask = TaskType.FireEnded;
    public List<ParticleSystem> fireObjs;
    public void RemoveFireObject(GameObject fireObj)
    {
        ParticleSystem ps = fireObj.GetComponent<ParticleSystem>();
        if(ps) fireObjs.Remove(ps);
        Destroy(fireObj);
        if(fireObjs.Count == 0)
        {
            ObjectiveManager.OnTaskEventReceived(onFireEndTask);
        }
    } 
}
