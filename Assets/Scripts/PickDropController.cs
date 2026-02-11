using UnityEngine;
using DG.Tweening;
using ControlFreak2;
using Ommy.Attributes;

public class PickDropController : MonoBehaviour
{
    public static PickDropController instance;

    private void Awake()
    {
        instance = this;
    }
    public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;
    public LayerMask detectionLayers;

    [Header("Pickup Settings")]
    [SerializeField] Transform holdArea;

    [Header("Physics Parameters")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private float pickupForce = 150.0f;

    [Header("Wall Stuck Prevention")]
    [SerializeField] private float maxDistanceFromHoldArea = 1.5f;

    public FirstPersonController fpc;

    public void DoorOpenCloseBtn()
    {
        doorController.DoorOpenClose();
        GamePlayManager.Instance.doorBell.Stop();
    }
    [InspectorButton("ToggleZoom")]
    public void ToggleZoom()
    {
        DetectedPickable(fpc.isZoomed);
    }
    public void DetectedPickable(bool detected)
    {
        fpc.isZoomed = detected;
    }
    RaycastHit hit;
    public DropPoint dropPoint;
    public BabyController babyController;
    public PickableItem detectedPickable;
    public DoorController doorController;
    public PickableItem heldPickable;
    private void Update()
    {
        ButtonInputs();
        //RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickupRange, detectionLayers, queryTriggerInteraction))
        {
            Interactable interactable;
            if(hit.transform.TryGetComponent<Interactable>(out interactable))
            {
                interactable.Detected();
                if(interactable as DropPoint)
                {
                    bool canAccept = heldPickable != null && ((DropPoint)interactable).CanAcceptItem(heldPickable);
                    if(canAccept)
                    {
                        dropPoint = (DropPoint)interactable;
                        UIManager.instance.SetCrosshair(dropPoint.crosshairState, dropPoint.detectionText);
                    }
                }
                if(interactable as DoorController)
                {
                    doorController = (DoorController)interactable;
                    UIManager.instance.SetCrosshair(doorController.crosshairState, doorController.detectionText);
                    UIManager.instance.SetDoorButtonVisible(true);
                }
                if(interactable as BabyController)
                {
                    babyController = (BabyController)interactable;
                    if (heldPickable != null && heldPickable.itemType == babyController.requireItem)
                    {
                        UIManager.instance.SetCrosshair(CrosshairState.Drop, "Give " + heldPickable.itemType.ToString() + " to Baby");
                    }
                    else if (babyController.canPickBaby)
                    {
                        detectedPickable = babyController;
                        DetectedPickable(true);
                        UIManager.instance.SetPickButtonVisible(true);
                        UIManager.instance.SetCrosshair(detectedPickable.crosshairState, detectedPickable.detectionText);
                    }
                    else
                    {
                        UIManager.instance.SetCrosshair(CrosshairState.None, "Need "+babyController.requireItem.ToString());
                    }
                }
                else if(interactable as PickableItem)
                {
                    detectedPickable = (PickableItem)interactable;
                    DetectedPickable(true);
                    UIManager.instance.SetPickButtonVisible(true);
                    UIManager.instance.SetCrosshair(detectedPickable.crosshairState, detectedPickable.detectionText);
                }
            }
        }
        else
        {
            detectedPickable = null;
            doorController = null;
            dropPoint = null;
            babyController = null;
            DetectedPickable(false);
            UIManager.instance.SetCrosshair(CrosshairState.None, null);
            UIManager.instance.SetDoorButtonVisible(false);
            if(heldPickable==null) UIManager.instance.SetPickButtonVisible(false);
        }
    }
    public void ButtonInputs()
    {
        if (CF2Input.GetKeyDown(KeyCode.Mouse1))
        {
            doorController.DoorOpenClose();
        }
        if (CF2Input.GetKeyDown(KeyCode.P))
        {
            if (heldPickable != null)
                DropObject();
            else
                PickupObject();
        }
        if(CF2Input.GetKeyDown(KeyCode.F))
        {
            if(heldPickable is UseableItem)
            {
                ((UseableItem)heldPickable).UseDevice();
            }
        }
    }
    private void FixedUpdate()
    {
        if (heldPickable != null)
        {
            //MoveObject
            MoveObject();
        }
    }
    Vector3 moveDirection;
    void MoveObject()
    {
        // Transform local offset to world space relative to holdArea's orientation
        Vector3 worldOffset = holdArea.TransformDirection(heldPickable.holdPositionOffset);
        Vector3 targetPosition = holdArea.position + worldOffset;
        
        moveDirection = targetPosition - heldPickable.transform.position;
        float distanceFromHoldArea = moveDirection.magnitude;

        // If object is too far (stuck in wall), disable collider temporarily
        if (heldPickable != null)
        {
            if (distanceFromHoldArea > maxDistanceFromHoldArea)
            {
                heldPickable.collider.enabled = false; // Disable to pass through walls
            }
            else
            {
                heldPickable.collider.enabled = true; // Re-enable when close
            }
        }

        heldPickable.rb.AddForce(moveDirection * pickupForce, ForceMode.Force);
    }

    public void PickupObject()
    {
        heldPickable = detectedPickable;
        heldPickable.PickObject(holdArea);
    }
    public void DropObject()
    {
        if(heldPickable == null) return;
        if(dropPoint != null)
        {
            dropPoint.DropOnPoint(heldPickable);
            heldPickable = null;
            SoundManager.instance?.DropItem();
        }
        else if(babyController != null && babyController.requireItem == heldPickable.itemType)
        {
            babyController.GiveItemToBaby(heldPickable);
            heldPickable = null;
            SoundManager.instance?.DropItem();
        }
        else
        {
            heldPickable.DropObject();
            heldPickable = null;
            SoundManager.instance?.DropItem();
        }
    }
}