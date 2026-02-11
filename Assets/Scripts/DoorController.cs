using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class DoorController : Interactable
{
    public List<GameObject> crackEffects;
    public Vector3 doorOpen;
    public Vector3 doorClose;
    public TaskType onDoorBreakTask;
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
            // Door locked effect - punch then snap back
            transform.DOPunchRotation(Vector3.up * 2f, 0.5f, 8, 0.5f)
                .OnComplete(() => 
                {
                    transform.DORotate(doorClose, 0.1f);
                });
            SoundManager.instance?.drop?.Stop();
            print("hitting axe");
            GamePlayManager.Instance.doorLock.Play();
        }
        UpdateDetectionText();
    }
    void OnCollisionExit(Collision other)
    {
        if(other.collider.TryGetComponent(out AxeController axe))
        {
            if(axe.isSwinging && isDoorLock)
            {
                if(crackEffects.Any(e => e.activeInHierarchy == false))
                {
                    crackEffects.FirstOrDefault(e => e.activeInHierarchy == false)?.SetActive(true);
                }
                else 
                {
                    ObjectiveManager.OnTaskEventReceived(onDoorBreakTask);
                    isDoorLock = false;
                    DoorOpenClose();
                }
            }
        }
    }
}
