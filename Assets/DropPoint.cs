using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using Ommy.Audio;
using UnityEngine.VFX;
public class DropPoint : Interactable
{
    public ItemType FilledBy = ItemType.None;
    public ItemType acceptableItemType = ItemType.Any;
    public TaskType onDropTaskType = TaskType.None;
    public ParticleSystem dropAreaVFX;
    public BabyAnimationType whenDropBabyPlayAnim;
    public UnityEvent onItemDrop;

    public override void Start()
    {
        base.Start();
        detectionText = "Drop "+ acceptableItemType.ToString();
    }
    // Check if this drop point can accept the given item
    public bool CanAcceptItem(PickableItem item) 
        => acceptableItemType == ItemType.Any || acceptableItemType == item.itemType;
    public void DropOnPoint(PickableItem item)
    {
        dropAreaVFX.Stop();
        item.rb.isKinematic = true;
        item.ReleaseObject();
        DOTween.Kill(item.transform);
        item.transform.DOLocalJump(transform.position, 0.5f, 1, 0.5f);
        item.transform.DORotate(transform.eulerAngles, 0.5f);
        ObjectiveManager.OnTaskEventReceived(onDropTaskType);
        if(item is BabyController)
        {
            BabyController baby = (BabyController)item;
            if(baby.requireItem != ItemType.None)
                baby.SetAnimation(BabyAnimationType.CrySit);
            else
                baby.SetAnimation(whenDropBabyPlayAnim);
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
