using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEditor.Experimental.GraphView;

public class DoorController : Interactable
{
    public Vector3 doorOpen;
    public Vector3 doorClose;
    public bool isDoorOpen = false;

    public bool isDoorLock;
    public override void Start() 
    {
        base.Start();
        UpdateDetectionText();
    }
    public void UpdateDetectionText()
    {
        if (isDoorLock) detectionText = "Door is Locked";
        else detectionText = isDoorOpen ? "Close Door" : "Open Door"; 
        crosshairState = isDoorOpen ? CrosshairState.DoorClose : CrosshairState.DoorOpen;
    }
    public void DoorOpenClose()
    {
        if (isDoorLock == false)
        {
            if (isDoorOpen == false)
            {
                transform.DORotate(doorOpen, 0.5f);
                isDoorOpen = true;
                SoundManager.instance.doorOpen.Play();
            }
            else
            {
                transform.DORotate(doorClose, 0.5f);
                isDoorOpen = false;
                SoundManager.instance.doorClose.Play();
            }
        }
        else
        {
            this.transform.DOShakePosition(0.5f, 1, 10, 30);
            SoundManager.instance?.drop?.Stop();
            print("hitting axe");
            GamePlayManager.Instance.doorLock.Play();
        }
        UpdateDetectionText();
    }
}
