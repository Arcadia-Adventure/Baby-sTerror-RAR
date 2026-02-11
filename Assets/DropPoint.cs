using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
public class DropPoint : Interactable
{
    public ItemType FilledBy = ItemType.None;
    public ItemType acceptableItemType = ItemType.Any;
    public TaskType onDropTaskType = TaskType.None;
    public BabyAnimationType whenDropBabyPlayAnim;
    public UnityEvent onItemDrop;

    public override void Start()
    {
        base.Start();
        FilledBy = ItemType.None;
        detectionText = "Drop "+ acceptableItemType.ToString();
    }
    // Check if this drop point can accept the given item
    public bool CanAcceptItem(PickableItem item) 
        => acceptableItemType == ItemType.Any || acceptableItemType == item.itemType;
    public void DropOnPoint(PickableItem item)
    {
        item.rb.isKinematic = true;
        item.ReleaseObject();
        item.transform.DOLocalJump(transform.position, 0.5f, 1, 0.5f);
        item.transform.DORotate(transform.eulerAngles, 0.5f);
        ObjectiveManager.OnTaskEventReceived(onDropTaskType);
        if(item is BabyController)
        {
            BabyController baby = (BabyController)item;
            baby.babyAnimationController.SetAnimation(whenDropBabyPlayAnim);
        }
        onItemDrop.Invoke();
        FilledBy = item.itemType;
        item.currentDropPoint = this;
        collider.enabled = false;
    }
    public void ClearDropPoint()
    {
        collider.enabled = true;
        FilledBy = ItemType.None;
    }
}
