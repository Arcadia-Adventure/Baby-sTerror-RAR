using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BabyItemEffect
{
    public ItemType itemType;
    public GameObject[] objectsToActivate;
    public GameObject[] objectsToDeactivate;
}

public class BabyItemHandler : MonoBehaviour
{
    [SerializeField] private List<BabyItemEffect> itemEffects = new List<BabyItemEffect>();

    public void GiveItemToBaby(PickableItem item)
    {
        var baby = BabyController.Instance;
        Debug.Log("item: " + item.itemType + " given to baby");

        TweenUtilities.Kill(item.transform);
        item.ReleaseObject();
        item.rb.isKinematic = true;
        item.collider.enabled = false;
        TweenUtilities.LocalJump(item.transform, baby.transform.position, 0.5f, 1, 0.5f);
        TweenUtilities.Rotate(item.transform, baby.transform.eulerAngles, 0.5f);
        ObjectiveUIController.OnTaskEventReceived(item.OnDropForBabyTaskType);
        baby.SetAnimation(BabyAnimationType.Happy);

        TweenUtilities.DelayedCall(0.5f, () =>
        {
            ApplyItemEffect(item.itemType);
            baby.requireItem = ItemType.None;
            TweenUtilities.Kill(item.transform);
            Destroy(item.gameObject);
        }, this);
    }

    void ApplyItemEffect(ItemType itemType)
    {
        var effect = itemEffects.Find(e => e.itemType == itemType);
        if (effect != null)
        {
            foreach (var obj in effect.objectsToActivate)
                if (obj != null) obj.SetActive(true);

            foreach (var obj in effect.objectsToDeactivate)
                if (obj != null) obj.SetActive(false);
        }

        if (itemType == ItemType.Talisman)
            CalmDownBaby();
    }

    void CalmDownBaby()
    {
        var baby = BabyController.Instance;
        baby.rb.isKinematic = false;
        baby.rb.useGravity = true;
        baby.babyEyesRed.color = Color.white;
        baby.tag = baby.itemTag;
        baby.SetAnimation(BabyAnimationType.Happy);
        baby.canPickBaby = true;
        baby.requireItem = ItemType.None;
        GamePlayManager.Instance.SetupFlyingFurniture(isFly: false);
        GamePlayManager.Instance.bedroomFireArea.DeactivateFire();
        GamePlayManager.Instance.cradleDropPoint.gameObject.SetActive(true);
    }
}
