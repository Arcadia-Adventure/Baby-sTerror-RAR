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
    public bool isDoorOpen;
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
        if (isDoorLock)
            detectionText = "Door is Locked";
        else
            detectionText = isDoorOpen ? "Close Door" : "Open Door";

        crosshairState = isDoorOpen ? CrosshairState.DoorClose : CrosshairState.DoorOpen;
    }

    public void DoorOpenClose()
    {
        if (isDoorLock)
        {
            ObjectiveManager.OnTaskEventReceived(onLockedCheckTask);
            AA_AnalyticsManager.Agent.TrackButtonClick("locked_door_hit");
            transform.DOPunchRotation(Vector3.up * 2f, 0.5f, 8, 0.5f)
                .OnComplete(() => transform.DORotate(doorClose, 0.1f));
            AudioManager.Instance.PlaySFX(lockedDoorSFX);
        }
        else if (!isDoorOpen)
        {
            onDoorOpen.Invoke(true);
            transform.DORotate(doorOpen, 0.5f);
            isDoorOpen = true;
            AudioManager.Instance.PlaySFX(doorOpenSFX);
            PlayDoorBell(false);
            StopDoorKnocking();
        }
        else
        {
            onDoorOpen.Invoke(false);
            transform.DORotate(doorClose, 0.5f);
            isDoorOpen = false;
            AudioManager.Instance.PlaySFX(doorCloseSFX);
        }

        UpdateDetectionText();
    }

    void OnCollisionExit(Collision other)
    {
        if (!other.collider.TryGetComponent(out AxeController axe)) return;
        if (!axe.isSwinging || !isDoorLock) return;

        AudioManager.Instance.PlaySFX(SFX.DoorBreak);

        var inactiveCrack = crackEffects.FirstOrDefault(e => !e.activeInHierarchy);
        if (inactiveCrack != null)
        {
            inactiveCrack.SetActive(true);
        }
        else
        {
            AA_AnalyticsManager.Agent.TrackButtonClick("door_break");
            ObjectiveManager.OnTaskEventReceived(onDoorBreakTask);
            SetLocked(false);
            DoorOpenClose();
        }
    }

    public void PlayDoorKnocking(float initialDelay, float interval = 1f)
    {
        if (doorKnockingSource != null)
            doorKnockingSource.PlayRepeating(initialDelay, interval);
    }

    public void StopDoorKnocking()
    {
        if (doorKnockingSource != null)
            doorKnockingSource.Stop();
    }

    public void PlayDoorBell(bool play)
    {
        if (audioSource == null) return;

        if (!play)
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
