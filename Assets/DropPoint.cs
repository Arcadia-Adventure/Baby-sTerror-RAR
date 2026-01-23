using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
public class DropPoint : Interactable
{
    public TaskType onDropTaskType = TaskType.None;
    public BabyAnimationType whenDropBabyPlayAnim;
    public UnityEvent onItemDrop;

    public override void Start()
    {
        base.Start();
        detectionText = "Drop Baby";
    }
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
    }
}
