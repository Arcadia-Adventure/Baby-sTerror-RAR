using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;

public class FireArea : MonoBehaviour
{
    public TaskType onFireEndTask = TaskType.FireEnded;
    public List<ParticleSystem> fireObjs;
    public MyAudioSource fireAudio;
    public void RemoveFireObject(GameObject fireObj)
    {
        ParticleSystem ps = fireObj.GetComponent<ParticleSystem>();
        if(ps) fireObjs.Remove(ps);
        Destroy(fireObj);
        if(fireObjs.Count == 0)
        {
            ObjectiveUIController.OnTaskEventReceived(onFireEndTask);
            fireAudio.Stop();
        }
    } 
    public void ActivateFire()
    {
        foreach (var fireObj in fireObjs) fireObj.Play();
        fireAudio.Play();
    }
    public void DeactivateFire()
    {
        fireAudio.Stop();
        foreach (var fireObj in fireObjs) fireObj.Stop();
    }
}
