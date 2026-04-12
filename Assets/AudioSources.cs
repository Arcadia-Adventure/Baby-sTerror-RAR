using UnityEngine;
using Ommy.Singleton;
using DG.Tweening;
public class AudioSources : Singleton<AudioSources>
{
    public AudioSource doorKnocking;
    public void PlayDoorKnocking(float delay, bool repeat)
    {
        DOVirtual.DelayedCall(delay, () => 
        {
            doorKnocking.Play();
            if(repeat)
                PlayDoorKnocking(delay + 1f, repeat);
            else
                doorKnocking.Stop();
        });
    }
}
