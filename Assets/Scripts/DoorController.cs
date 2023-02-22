using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DoorController : MonoBehaviour
{
    public Vector3 doorOpen;
    public Vector3 doorClose;
    public bool isDoor = false;

    public bool isDoorLock;

    public void DoorOpenClose()
    {
        if (isDoorLock == false)
        {
            if (isDoor == false)
            {
                transform.DORotate(doorOpen, 0.5f);
                isDoor = true;

                SoundManager.instance.doorOpen.Play();
            }
            else
            {
                transform.DORotate(doorClose, 0.5f);
                isDoor = false;

                SoundManager.instance.doorClose.Play();
            }
        }
        else
        {
            transform.DOPunchRotation(Vector3.up*2 , 0.5f).OnComplete(() => {
                transform.DORotate(doorClose, 0.1f);


            });

            GamePlayManager.instance.doorLock.Play();
        }
       
    }
}
