using UnityEngine;
using DG.Tweening;
using ControlFreak2;
using Ommy.Attributes;
using Ommy.Audio;
using Ommy.Singleton;

public class PickDropController : Singleton<PickDropController>
{

    public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;
    public LayerMask detectionLayers;

    [Header("Pickup Settings")]
    [SerializeField] Transform holdArea;

    [Header("Physics Parameters")]
    [SerializeField] float pickupRange = 10f;
    [SerializeField] float pickupForce = 150f;

    [Header("Wall Stuck Prevention")]
    [SerializeField] float maxDistanceFromHoldArea = 1.5f;

    [Header("Touch Input")]
    [Tooltip("Tapping the look pad does what the crosshair icon shows, same as its touch button.")]
    [SerializeField] TrackPadTap lookPadTap;

    public FirstPersonController fpc;

    public DropPoint dropPoint;
    public BabyController babyController;
    public PickableItem detectedPickable;
    public DoorController doorController;
    public PickableItem heldPickable;
    public WorldInteractable worldTarget;

    Interactable lastDetected;
    RaycastHit hit;
    Vector3 moveDirection;
    bool _hiding;

    /// <summary>True while the player is inside a hiding spot; only Leave works then.</summary>
    public bool IsHiding => _hiding;

    #region Input

    public void DoorOpenCloseBtn()
    {
        if (_hiding) return;
        if (doorController != null)
            doorController.DoorOpenClose();
    }

    public void PickDropBtn()
    {
        if (_hiding) return;
        if (heldPickable != null)
            DropObject();
        else
            PickupObject();
    }

    /// <summary>The Interact button (E): use what the crosshair is on, or leave a hiding spot.</summary>
    public void InteractBtn()
    {
        if (_hiding)
        {
            HideSpot spot = HideSpot.Occupied;
            if (spot != null && spot.CanInteract(heldPickable, out _))
                spot.Interact(this);
            return;
        }

        if (worldTarget != null && worldTarget.CanInteract(heldPickable, out _))
            worldTarget.Interact(this);
    }

    /// <summary>Does what the matching touch button would do for the crosshair icon currently shown.</summary>
    public void Interact()
    {
        switch (UIManager.Instance.CurrentCrosshairState)
        {
            case CrosshairState.DoorOpen:
            case CrosshairState.DoorClose:
                DoorOpenCloseBtn();
                break;

            case CrosshairState.Pick:
            case CrosshairState.Drop:
                PickDropBtn();
                break;

            case CrosshairState.Interact:
            case CrosshairState.Hide:
                InteractBtn();
                break;
        }
    }

    [InspectorButton("ToggleZoom")]
    public void ToggleZoom() => DetectedPickable(fpc.isZoomed);

    public void DetectedPickable(bool detected) => fpc.isZoomed = detected;

    public void ButtonInputs()
    {
        if (CF2Input.GetKeyDown(KeyCode.Mouse1) && doorController != null)
            doorController.DoorOpenClose();

        if (CF2Input.GetKeyDown(KeyCode.P))
            PickDropBtn();

        if (CF2Input.GetKeyDown(KeyCode.F) && heldPickable is UseableItem useable)
            useable.UseDevice();

        if (CF2Input.GetKeyDown(KeyCode.E))
            InteractBtn();

        if (lookPadTap != null && lookPadTap.JustTapped)
            Interact();
    }

    #endregion

    #region Detection

    private void Update()
    {
        // Inside a hiding spot nothing is in reach; the Interact button is the way out.
        if (_hiding)
        {
            if (CF2Input.GetKeyDown(KeyCode.E))
                InteractBtn();
            return;
        }

        ButtonInputs();
        if (_hiding)
            return;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward),
                out hit, pickupRange, detectionLayers, queryTriggerInteraction)
            && hit.transform.TryGetComponent<Interactable>(out var interactable))
        {
            HandleDetection(interactable);
        }
        else
        {
            ClearDetection();
        }
    }

    void HandleDetection(Interactable interactable)
    {
        if (lastDetected != null && lastDetected != interactable)
        {
            lastDetected.Undetected();
            lastDetected = null;
        }

        lastDetected = interactable;
        interactable.Detected();

        // Each detection decides on its own buttons; drop the ones only another kind uses.
        if (interactable is not WorldInteractable && !(interactable is DoorController d && d.HideSpot != null))
        {
            worldTarget = null;
            UIManager.Instance.SetInteractButton(false);
        }

        switch (interactable)
        {
            case BabyController baby:
                HandleBabyDetection(baby);
                break;

            case DropPoint drop:
                HandleDropPointDetection(drop);
                break;

            // A wardrobe door means "hide here", not "open the door".
            case DoorController door when door.HideSpot != null:
                HandleWorldDetection(door.HideSpot);
                break;

            case DoorController door:
                HandleDoorDetection(door);
                break;

            case WorldInteractable world:
                HandleWorldDetection(world);
                break;

            case PickableItem pickable:
                HandlePickableDetection(pickable);
                break;
        }
    }

    void HandleWorldDetection(WorldInteractable world)
    {
        worldTarget = world;
        doorController = null;
        detectedPickable = null;
        dropPoint = null;
        babyController = null;
        DetectedPickable(false);
        UIManager.Instance.SetDoorButtonVisible(false);
        if (heldPickable == null)
            UIManager.Instance.SetPickButtonVisible(false);

        bool usable = world.CanInteract(heldPickable, out string prompt);
        UIManager.Instance.SetCrosshair(usable ? world.PromptState : CrosshairState.None, prompt);
        UIManager.Instance.SetInteractButton(usable, world.buttonSprite);
    }

    void HandleDropPointDetection(DropPoint drop)
    {
        if (heldPickable != null && drop.CanAcceptItem(heldPickable))
        {
            dropPoint = drop;
            UIManager.Instance.SetCrosshair(drop.crosshairState, drop.detectionText);
        }
    }

    void HandleDoorDetection(DoorController door)
    {
        doorController = door;
        string prompt = door.Accepts(heldPickable) ? "Unlock Door" : door.detectionText;
        UIManager.Instance.SetCrosshair(door.crosshairState, prompt);
        UIManager.Instance.SetDoorButtonVisible(true);
    }

    void HandleBabyDetection(BabyController baby)
    {
        babyController = baby;

        if (heldPickable != null && heldPickable.itemType == baby.requireItem)
        {
            UIManager.Instance.SetCrosshair(CrosshairState.Drop,
                "Give " + heldPickable.itemType + " to Baby");
        }
        else if (baby.canPickBaby)
        {
            detectedPickable = baby;
            DetectedPickable(true);
            UIManager.Instance.SetPickButtonVisible(true);
            UIManager.Instance.SetCrosshair(baby.crosshairState, baby.detectionText);
        }
        else
        {
            UIManager.Instance.SetCrosshair(CrosshairState.None,
                "Need " + baby.requireItem);
        }
    }

    void HandlePickableDetection(PickableItem pickable)
    {
        detectedPickable = pickable;
        DetectedPickable(true);
        UIManager.Instance.SetPickButtonVisible(true);
        UIManager.Instance.SetCrosshair(pickable.crosshairState, pickable.detectionText);
    }

    void ClearDetection()
    {
        if (lastDetected != null)
        {
            lastDetected.Undetected();
            lastDetected = null;
        }

        detectedPickable = null;
        doorController = null;
        dropPoint = null;
        babyController = null;
        worldTarget = null;
        DetectedPickable(false);
        UIManager.Instance.SetCrosshair(CrosshairState.None, null);
        UIManager.Instance.SetDoorButtonVisible(false);
        UIManager.Instance.SetInteractButton(false);
        if (heldPickable == null)
            UIManager.Instance.SetPickButtonVisible(false);
    }

    #endregion

    #region Hiding

    /// <summary>Called by HideSpot: clears what's in reach and turns the Interact button into Leave.</summary>
    public void EnterHiding()
    {
        ClearDetection();
        _hiding = true;
        UIManager.Instance.SetPickButtonVisible(false);
        UIManager.Instance.SetUseDeviceButtonVisible(false);
        UIManager.Instance.SetCrouchButtonVisible(false);
        UIManager.Instance.SetSprintButtonVisible(false);
        UIManager.Instance.SetInteractButton(true, UIManager.Instance.exitHideSprite);
        UIManager.Instance.SetCrosshair(CrosshairState.None, null);
    }

    public void ExitHiding()
    {
        _hiding = false;
        UIManager.Instance.SetInteractButton(false);
        UIManager.Instance.SetCrouchButtonVisible(true);
        UIManager.Instance.SetSprintButtonVisible(true);
        if (heldPickable != null)
            UIManager.Instance.SetPickButtonVisible(true);
    }

    #endregion

    #region Pick & Drop

    private void FixedUpdate()
    {
        // While hiding the held item is stashed (inactive), so there's nothing to move.
        if (heldPickable != null && heldPickable.gameObject.activeInHierarchy)
            MoveHeldObject();
    }

    /// <summary>
    /// Uses up the held item (a key in a lock, a fuse in the box): it leaves the player's hands and
    /// the scene. Returns the item, or null if nothing was held.
    /// </summary>
    public PickableItem ConsumeHeld()
    {
        PickableItem item = heldPickable;
        if (item == null)
            return null;

        heldPickable = null;
        item.ReleaseObject();
        item.rb.isKinematic = true;
        if (item is UseableItem useable)
            useable.OnDropDevice();
        item.gameObject.SetActive(false);

        UIManager.Instance.SetPickButtonVisible(false);
        return item;
    }

    void MoveHeldObject()
    {
        Vector3 worldOffset = holdArea.TransformDirection(heldPickable.holdPositionOffset);
        Vector3 targetPosition = holdArea.position + worldOffset;

        moveDirection = targetPosition - heldPickable.transform.position;
        float distanceFromHoldArea = moveDirection.magnitude;

        if (heldPickable != null)
            heldPickable.collider.enabled = distanceFromHoldArea <= maxDistanceFromHoldArea;

        heldPickable.rb.AddForce(moveDirection * pickupForce, ForceMode.Force);
    }

    public void PickupObject()
    {
        if (detectedPickable == null) return;
        heldPickable = detectedPickable;
        heldPickable.PickObject(holdArea);
    }

    public void DropObject()
    {
        if (heldPickable == null) return;

        var droppedItem = heldPickable;

        if (dropPoint != null)
            dropPoint.DropOnPoint(heldPickable);
        else if (babyController != null && babyController.requireItem == heldPickable.itemType)
            babyController.GiveItemToBaby(heldPickable);
        else
            heldPickable.DropObject();

        heldPickable = null;

        if (droppedItem.dropSFX != null)
            AudioManager.Instance.PlaySFX(droppedItem.dropSFX);
        else
            Debug.LogWarning("No drop sfx for " + droppedItem.itemType);
    }

    #endregion
}
