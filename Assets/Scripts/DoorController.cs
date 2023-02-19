using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DoorController : MonoBehaviour
{
  
    public Vector3 doorOpen;
    public Vector3 doorClose;
    public bool isDoor = false;
    public void DoorOpenClose()
    {
        if(isDoor == false)
        {
            transform.DORotate(doorOpen, 0.5f);
            isDoor = true;
        }
        else
        {
            transform.DORotate(doorClose, 0.5f);
            isDoor = false;
        }
    }
}
