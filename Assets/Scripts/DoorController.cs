using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Ommy.Audio;
using UnityEngine.Events;

public class DoorController : Interactable
{
    public List<GameObject> crackEffects;
    public Vector3 doorOpen;
    public Vector3 doorClose;
    public TaskType onLockedCheckTask;
    public TaskType onDoorBreakTask;
    public MyAudioSource audioSource;
    public MyAudioSource doorKnockingSource;
    public AudioClip lockedDoorSFX;
    public AudioClip doorOpenSFX;
    public AudioClip doorCloseSFX;
    public AudioClip doorBellSFX;
    public UnityEvent<bool> onDoorOpen;
    public bool isDoorOpen = false;

    public bool isDoorLock;
    public override void Start() 
    {
        base.Start();
        UpdateDetectionText();
    }

    public void SetLocked(bool locked)
    {
        isDoorLock = locked;
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
        if(isDoorLock)
        {
            ObjectiveManager.OnTaskEventReceived(onLockedCheckTask);
            AA_AnalyticsManager.Agent.TrackButtonClick("locked_door_hit");
        }
        if (isDoorLock == false)
        {
            onDoorOpen.Invoke(!isDoorOpen);
            if (isDoorOpen == false)
            {
                transform.DORotate(doorOpen, 0.5f);
                isDoorOpen = true;
                AudioManager.Instance.PlaySFX(doorOpenSFX);
                PlayDoorBell(false);
                StopDoorKnocking();
            }
            else
            {
                transform.DORotate(doorClose, 0.5f);
                isDoorOpen = false;
                AudioManager.Instance.PlaySFX(doorCloseSFX);
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
            AudioManager.Instance.PlaySFX(lockedDoorSFX);
        }
        UpdateDetectionText();
    }
    void OnCollisionExit(Collision other)
    {
        if(other.collider.TryGetComponent(out AxeController axe))
        {
            if(axe.isSwinging && isDoorLock)
            {
                AudioManager.Instance.PlaySFX(SFX.DoorBreak);
                if(crackEffects.Any(e => e.activeInHierarchy == false))
                {
                    crackEffects.FirstOrDefault(e => e.activeInHierarchy == false)?.SetActive(true);
                }
                else 
                {
                    AA_AnalyticsManager.Agent.TrackButtonClick("door_break");
                    ObjectiveManager.OnTaskEventReceived(onDoorBreakTask);
                    SetLocked(false);
                    DoorOpenClose();
                }
            }
        }
    }
    public void PlayDoorKnocking(float initialDelay, float interval = 1f)
    {
        if (doorKnockingSource == null) return;
        doorKnockingSource.PlayRepeating(initialDelay, interval);
    }

    public void StopDoorKnocking()
    {
        if (doorKnockingSource != null)
            doorKnockingSource.Stop();
    }

    public void PlayDoorBell(bool play)
    {
        if(audioSource == null) return;
        if(!play)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }
        else
        {
            audioSource.clip = doorBellSFX;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}
