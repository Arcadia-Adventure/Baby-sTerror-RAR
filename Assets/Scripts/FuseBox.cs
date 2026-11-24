using Ommy.Audio;
using UnityEngine;

/// <summary>
/// The fuse box. Sits on a DropPoint that accepts the Fuse (its onDropTaskType should be
/// RestorePower); putting the fuse in brings the house lights back.
/// </summary>
[RequireComponent(typeof(DropPoint))]
public class FuseBox : MonoBehaviour
{
    [Tooltip("Turn the power back on when a fuse goes in.")]
    public bool restorePower = true;
    public AudioClip insertSFX;
    [Tooltip("Optional: shown once the fuse is in (a lit indicator, an open panel...).")]
    public GameObject poweredIndicator;

    DropPoint _point;

    void Awake()
    {
        _point = GetComponent<DropPoint>();
        _point.acceptableItemType = ItemType.Fuse;
        _point.onItemDrop.AddListener(OnFuseInserted);
        if (poweredIndicator != null)
            poweredIndicator.SetActive(false);
    }

    void OnDestroy()
    {
        if (_point != null)
            _point.onItemDrop.RemoveListener(OnFuseInserted);
    }

    void OnFuseInserted()
    {
        // DropOnPoint runs while the fuse is still the held item.
        PickableItem fuse = PickDropController.Instance != null ? PickDropController.Instance.heldPickable : null;
        if (fuse != null)
        {
            // Once it's in, it stays in.
            TweenUtilities.DelayedCall(0.6f, () =>
            {
                if (fuse != null && fuse.collider != null)
                    fuse.collider.enabled = false;
            }, this);
        }

        if (insertSFX != null)
            AudioManager.Instance.PlaySFX(insertSFX);
        if (poweredIndicator != null)
            poweredIndicator.SetActive(true);
        if (restorePower && HouseLights.Instance != null)
            HouseLights.Instance.Flicker(0.8f, endOn: true);
    }
}
