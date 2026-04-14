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

    public FirstPersonController fpc;

    public DropPoint dropPoint;
    public BabyController babyController;
    public PickableItem detectedPickable;
    public DoorController doorController;
    public PickableItem heldPickable;

    RaycastHit hit;
    Vector3 moveDirection;

    #region Input

    public void DoorOpenCloseBtn()
    {
        if (doorController != null)
            doorController.DoorOpenClose();
    }

    [InspectorButton("ToggleZoom")]
    public void ToggleZoom() => DetectedPickable(fpc.isZoomed);

    public void DetectedPickable(bool detected) => fpc.isZoomed = detected;

    public void ButtonInputs()
    {
        if (CF2Input.GetKeyDown(KeyCode.Mouse1) && doorController != null)
            doorController.DoorOpenClose();

        if (CF2Input.GetKeyDown(KeyCode.P))
        {
            if (heldPickable != null)
                DropObject();
            else
                PickupObject();
        }

        if (CF2Input.GetKeyDown(KeyCode.F) && heldPickable is UseableItem useable)
            useable.UseDevice();
    }

    #endregion

    #region Detection

    private void Update()
    {
        ButtonInputs();

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward),
                out hit, pickupRange, detectionLayers, queryTriggerInteraction))
        {
            if (hit.transform.TryGetComponent<Interactable>(out var interactable))
                HandleDetection(interactable);
        }
        else
        {
            ClearDetection();
        }
    }

    void HandleDetection(Interactable interactable)
    {
        interactable.Detected();

        switch (interactable)
        {
            case BabyController baby:
                HandleBabyDetection(baby);
                break;

            case DropPoint drop:
                HandleDropPointDetection(drop);
                break;

            case DoorController door:
                HandleDoorDetection(door);
                break;

            case PickableItem pickable:
                HandlePickableDetection(pickable);
                break;
        }
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
        UIManager.Instance.SetCrosshair(door.crosshairState, door.detectionText);
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
        detectedPickable = null;
        doorController = null;
        dropPoint = null;
        babyController = null;
        DetectedPickable(false);
        UIManager.Instance.SetCrosshair(CrosshairState.None, null);
        UIManager.Instance.SetDoorButtonVisible(false);
        if (heldPickable == null)
            UIManager.Instance.SetPickButtonVisible(false);
    }

    #endregion

    #region Pick & Drop

    private void FixedUpdate()
    {
        if (heldPickable != null)
            MoveHeldObject();
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
