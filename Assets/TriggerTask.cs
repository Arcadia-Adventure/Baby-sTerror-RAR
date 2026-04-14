using UnityEngine;
using UnityEngine.Events;

public class TriggerTask : MonoBehaviour
{
    public ItemType requireItem = ItemType.None;
    public TaskType triggerTaskType = TaskType.None;
    public TaskType collisionTaskType = TaskType.None;
    public UnityEvent onTriggerSuccess;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(HaveRequireItem())
            {
                ObjectiveManager.OnTaskEventReceived(triggerTaskType);
                onTriggerSuccess.Invoke();
            }
        }
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if(HaveRequireItem())
            ObjectiveManager.OnTaskEventReceived(collisionTaskType);
        }
    }
    public bool HaveRequireItem()=>
        requireItem == ItemType.None || 
        (PickDropController.Instance.heldPickable != null 
        && PickDropController.Instance.heldPickable.itemType == requireItem);
}
