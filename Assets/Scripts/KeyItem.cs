using UnityEngine;

/// <summary>
/// A key that unlocks one door: the door whose SceneObjectTag has the opensDoor key.
/// Pick it up, aim at the locked door and press the door button. The key is used up.
/// </summary>
public class KeyItem : PickableItem
{
    [Tooltip("The SceneObjectKey on the door this key opens.")]
    public SceneObjectKey opensDoor;

    void Reset()
    {
        itemType = ItemType.Key;
        OnPickTaskType = TaskType.PickKey;
        detectionText = "Pick Key";
        crosshairState = CrosshairState.Pick;
    }
}
