using System.Collections;
using Ommy.Audio;
using UnityEngine;

/// <summary>
/// A wardrobe or under-bed space the player can hide in. Aiming at it (or at one of its doors)
/// shows "Hide"; inside, the Interact button shows "Leave".
///
/// The Nanny can't see or hear a hidden player. If she was chasing and saw them go in, the spot is
/// compromised: she walks up and drags them out (NannyStateManager PullOut). Otherwise she may
/// stop and inspect it for a moment, then moves on.
///
/// The Hide task fires once she has come close and gone away again while the player stayed
/// hidden, or after a while if she never comes.
/// </summary>
public class HideSpot : WorldInteractable
{
    [Header("Placement")]
    [Tooltip("Where the camera sits while hidden, looking along its forward (e.g. out through the gap in the doors).")]
    public Transform viewPoint;
    [Tooltip("A floor point on the NavMesh in front of the spot. The player comes out here, and it's where the Nanny stands to pull them out.")]
    public Transform exitPoint;
    [Tooltip("Wardrobe doors. Aiming at them shows Hide; they open and slam shut around the player.")]
    public DoorController[] doors = new DoorController[0];
    [Tooltip("Can the player look around while hidden?")]
    public bool allowLook = true;
    [SerializeField] float enterDuration = 0.35f;

    [Header("Hide Task")]
    public TaskType onHiddenTask = TaskType.Hide;
    [Tooltip("She counts as having come by once she's this close.")]
    [SerializeField] float nannyNearRadius = 6f;
    [Tooltip("With no Nanny hunting, the task completes after this long hidden.")]
    [SerializeField] float fallbackSeconds = 4f;
    [Tooltip("If she's around but never comes close, the task completes after this long hidden.")]
    [SerializeField] float maxWaitSeconds = 22f;

    [Header("Feedback")]
    [Tooltip("Optional UI overlay (vignette, door slats) shown while hidden.")]
    public GameObject hiddenOverlay;
    public AudioClip enterSFX, exitSFX;

    /// <summary>The spot the player is hiding in, or null.</summary>
    public static HideSpot Occupied { get; private set; }

    /// <summary>The Nanny saw the player go in, so she knows where they are.</summary>
    public bool Compromised { get; set; }

    /// <summary>Seconds the player has been in this spot (0 when empty).</summary>
    public float HiddenFor => Occupied == this ? Time.time - _hiddenSince : 0f;

    float _hiddenSince;

    public Vector3 ExitPosition => exitPoint != null ? exitPoint.position : transform.position;

    PickableItem _stashedItem;
    bool _busy;

    void Awake()
    {
        foreach (DoorController door in doors)
        {
            if (door != null)
                door.HideSpot = this;
        }
    }

    // Only a spot with its own collider (under a bed) goes on the Interactable layer; a wardrobe
    // is reached through its doors and keeps its layer, so its sight and collision rules don't change.
    public override void Start()
    {
        if (collider != null)
            base.Start();
    }

    void OnDestroy()
    {
        if (Occupied == this)
            Occupied = null;
    }

    public override CrosshairState PromptState => CrosshairState.Hide;

    public override bool CanInteract(PickableItem held, out string prompt)
    {
        if (Occupied == this)
        {
            prompt = "Leave";
            return !_busy;
        }

        if (Occupied != null || _busy)
        {
            prompt = null;
            return false;
        }

        if (held is BabyController)
        {
            prompt = "Put the baby down to hide";
            return false;
        }

        prompt = "Hide";
        return true;
    }

    public override void Interact(PickDropController hands)
    {
        if (_busy)
            return;

        if (Occupied == this)
            StartCoroutine(ExitRoutine());
        else if (Occupied == null)
            StartCoroutine(EnterRoutine(hands));
    }

    IEnumerator EnterRoutine(PickDropController hands)
    {
        PlayerController player = GamePlayManager.Instance.player;
        if (player == null || player.IsHidden || player.Health.IsDead)
            yield break;

        _busy = true;
        Occupied = this;
        Compromised = false;
        _hiddenSince = Time.time;

        // Stash anything in hand (the baby can't come in, see CanInteract).
        _stashedItem = hands.heldPickable;
        if (_stashedItem != null)
        {
            if (_stashedItem is UseableItem useable)
                useable.OnDropDevice();
            _stashedItem.gameObject.SetActive(false);
        }

        player.EnterHide(this);
        hands.EnterHiding();

        NannyStateManager nanny = GamePlayManager.Instance.nanny;
        if (nanny != null && nanny.isActiveAndEnabled)
            nanny.OnPlayerHid(this);

        OpenDoors();
        if (enterSFX != null)
            AudioManager.Instance.PlaySFX(enterSFX);
        yield return new WaitForSeconds(enterDuration);

        if (Occupied != this)
        {
            _busy = false;
            yield break;
        }

        if (viewPoint != null)
            player.TeleportCameraTo(viewPoint);
        CloseDoors(0.25f);

        if (hiddenOverlay != null)
            hiddenOverlay.SetActive(true);

        _busy = false;
        StartCoroutine(WatchForHideTask());
    }

    IEnumerator ExitRoutine()
    {
        _busy = true;
        OpenDoors();
        if (exitSFX != null)
            AudioManager.Instance.PlaySFX(exitSFX);
        yield return new WaitForSeconds(enterDuration * 0.5f);

        Release();
        _busy = false;

        yield return new WaitForSeconds(0.6f);
        if (Occupied != this)
            CloseDoors(0.4f);
    }

    /// <summary>The Nanny drags the player out: no animation, they land at the exit point at once.</summary>
    public void EjectPlayer()
    {
        if (Occupied != this)
            return;

        StopAllCoroutines();
        OpenDoors();
        Release();
        _busy = false;
    }

    void Release()
    {
        PlayerController player = GamePlayManager.Instance.player;
        Occupied = null;
        Compromised = false;

        if (hiddenOverlay != null)
            hiddenOverlay.SetActive(false);

        if (exitPoint != null)
            player.TeleportFeetTo(exitPoint);
        player.ExitHide();

        PickDropController hands = PickDropController.Instance;
        hands.ExitHiding();

        if (_stashedItem != null)
        {
            _stashedItem.gameObject.SetActive(true);
            if (hands.heldPickable == _stashedItem && _stashedItem is UseableItem useable)
                useable.OnPickDevice();
            _stashedItem = null;
        }

        NannyStateManager nanny = GamePlayManager.Instance.nanny;
        if (nanny != null)
            nanny.OnPlayerLeftHiding(this);
    }

    IEnumerator WatchForHideTask()
    {
        float hiddenFor = 0f;
        float nannyAbsentFor = 0f;
        bool nannyCameBy = false;

        while (Occupied == this)
        {
            yield return null;
            hiddenFor += Time.deltaTime;
            if (Compromised)
                continue;

            NannyStateManager nanny = GamePlayManager.Instance.nanny;
            bool hunting = nanny != null && nanny.IsThreat;

            if (!hunting)
            {
                nannyAbsentFor += Time.deltaTime;
                if (nannyAbsentFor >= fallbackSeconds)
                    break;
                continue;
            }

            nannyAbsentFor = 0f;

            // Same floor only: on the stairs below she can be "close" straight through the ceiling.
            Vector3 offset = nanny.transform.position - ExitPosition;
            bool sameFloor = Mathf.Abs(offset.y) < 1.5f;
            float flatDistance = new Vector2(offset.x, offset.z).magnitude;

            if (sameFloor && flatDistance <= nannyNearRadius)
                nannyCameBy = true;
            else if (nannyCameBy && (!sameFloor || flatDistance > nannyNearRadius + 1f))
                break;

            // She never came: don't keep the player waiting forever. If she did come, wait for her
            // to leave, with a hard cap in case she lingers nearby.
            if (!nannyCameBy && hiddenFor >= maxWaitSeconds)
                break;
            if (hiddenFor >= maxWaitSeconds * 2.5f)
                break;
        }

        if (Occupied == this && !Compromised)
            ObjectiveUIController.OnTaskEventReceived(onHiddenTask);
    }

    void OpenDoors()
    {
        foreach (DoorController door in doors)
        {
            if (door != null && !door.isDoorLock)
                door.ForceOpen();
        }
    }

    void CloseDoors(float duration)
    {
        foreach (DoorController door in doors)
        {
            if (door != null)
                door.SlamShut(duration);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (viewPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(viewPoint.position, 0.08f);
            Gizmos.DrawRay(viewPoint.position, viewPoint.forward * 0.6f);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, 0.25f);
            Gizmos.DrawRay(exitPoint.position, exitPoint.forward * 0.6f);
        }
    }
}
