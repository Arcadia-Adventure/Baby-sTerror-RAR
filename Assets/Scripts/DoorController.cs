using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Ommy.Audio;
using UnityEngine.AI;
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

    [Header("Knock Punch")]
    public Vector3 knockPunch = new Vector3(0f, 6f, 0f);
    public float knockInDuration = 0.05f;
    public float knockOutDuration = 0.08f;
    public float[] knockHitTimes = { 0.07f, 0.25f, 0.39f, 0.55f, 0.7f };

    [Header("Nanny")]
    [Tooltip("Carves the NavMesh while the door is locked so the Nanny routes around it. " +
             "Added and fitted to the BoxCollider automatically if left empty.")]
    [SerializeField] NavMeshObstacle navBlocker;
    [SerializeField] float forceOpenDuration = 0.2f;

    public override void Start()
    {
        base.Start();
        UpdateDetectionText();
        UpdateNavBlocker();
    }

    public void SetLocked(bool locked)
    {
        isDoorLock = locked;
        UpdateDetectionText();
        UpdateNavBlocker();
    }

    /// <summary>
    /// The NavMesh is baked through every doorway, so only a locked door needs to cut it.
    /// Closed but unlocked doors are handled by the Nanny banging on them and breaking through.
    /// </summary>
    void UpdateNavBlocker()
    {
        if (navBlocker == null && !TryCreateNavBlocker())
            return;

        navBlocker.enabled = isDoorLock && !isDoorOpen;
    }

    bool TryCreateNavBlocker()
    {
        if (!TryGetComponent(out BoxCollider box))
            return false;

        navBlocker = gameObject.AddComponent<NavMeshObstacle>();
        navBlocker.shape = NavMeshObstacleShape.Box;
        navBlocker.center = box.center;
        // The leaf is only a few centimetres thick, which is too thin to carve reliably.
        navBlocker.size = new Vector3(box.size.x, box.size.y, Mathf.Max(box.size.z, 0.2f));
        navBlocker.carving = true;
        navBlocker.carveOnlyStationary = true;
        return true;
    }

    public void UpdateDetectionText()
    {
        if (isDoorLock)
            detectionText = "Door is Locked";
        else
            detectionText = isDoorOpen ? "Close Door" : "Open Door";

        crosshairState = isDoorOpen ? CrosshairState.DoorClose : CrosshairState.DoorOpen;
    }
    public void DoorPunchRotation()
    {
        PlayKnockHits(0f, 0.16f);
    }

    void PlayKnockAnimation()
    {
        PlayKnockHits(knockHitTimes);
    }

    void PlayKnockHits(params float[] times)
    {
        if (isDoorOpen || times == null || times.Length == 0) return;

        TweenUtilities.Kill(this);
        transform.rotation = Quaternion.Euler(doorClose);

        var seq = TweenUtilities.Sequence(this);
        for (int i = 0; i < times.Length; i++)
        {
            float t = times[i];
            Vector3 hitAngle = doorClose + knockPunch * (i == times.Length - 1 ? 1.2f : 1f);

            seq.Insert(t, TweenUtilities.Rotate(transform, hitAngle, knockInDuration)
                .SetEase(Ease.OutCubic)
                .Pause());
            seq.Insert(t + knockInDuration, TweenUtilities.Rotate(transform, doorClose, knockOutDuration)
                .SetEase(Ease.InCubic)
                .Pause());
        }

        seq.OnComplete(() =>
        {
            if (!isDoorOpen)
                transform.rotation = Quaternion.Euler(doorClose);
        });
    }

    public void DoorOpenClose()
    {
        if (isDoorLock)
        {
            ObjectiveUIController.OnTaskEventReceived(onLockedCheckTask);
            AA_AnalyticsManager.Agent.TrackButtonClick("locked_door_hit");
            DoorPunchRotation();
            AudioManager.Instance.PlaySFX(lockedDoorSFX);
        }
        else if (!isDoorOpen)
        {
            onDoorOpen.Invoke(true);
            StopDoorKnocking();
            TweenUtilities.Rotate(transform, doorOpen, 0.5f);
            isDoorOpen = true;
            AudioManager.Instance.PlaySFX(doorOpenSFX);
            PlayDoorBell(false);
        }
        else
        {
            onDoorOpen.Invoke(false);
            TweenUtilities.Rotate(transform, doorClose, 0.5f);
            isDoorOpen = false;
            AudioManager.Instance.PlaySFX(doorCloseSFX);
        }

        UpdateDetectionText();
        UpdateNavBlocker();
    }

    /// <summary>Someone pounding on the other side: the door jolts in its frame but stays shut.</summary>
    public void BangOnDoor(AudioClip bangSFX = null)
    {
        DoorPunchRotation();

        if (bangSFX == null)
            AudioManager.Instance.PlaySFX(SFX.DoorBreak);
        else if (audioSource != null)
            audioSource.PlayOneShot(bangSFX);
        else
            AudioManager.Instance.PlaySFX(bangSFX);
    }

    /// <summary>
    /// Bursts the door open from the Nanny's side. Unlike DoorOpenClose there is no lock check
    /// and no task event, since the player didn't open it.
    /// </summary>
    public void ForceOpen()
    {
        if (isDoorOpen)
            return;

        StopDoorKnocking();
        TweenUtilities.Rotate(transform, doorOpen, forceOpenDuration).SetEase(Ease.OutBack);
        isDoorOpen = true;
        AudioManager.Instance.PlaySFX(doorOpenSFX);
        onDoorOpen.Invoke(true);
        UpdateDetectionText();
        UpdateNavBlocker();
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
            ObjectiveUIController.OnTaskEventReceived(onDoorBreakTask);
            SetLocked(false);
            DoorOpenClose();
        }
    }

    public void PlayDoorKnocking(float initialDelay, float interval = 1f)
    {
        StopDoorKnocking();
        if (doorKnockingSource == null) return;

        doorKnockingSource.PlayRepeating(initialDelay, interval, 0f, PlayKnockAnimation);
    }

    public void StopDoorKnocking()
    {
        if (doorKnockingSource != null)
            doorKnockingSource.Stop();

        TweenUtilities.Kill(this);
        if (!isDoorOpen)
            transform.rotation = Quaternion.Euler(doorClose);
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
