using Ommy.Audio;
using UnityEngine;

/// <summary>
/// The flashlight lying somewhere in the house. Taking it equips the player's flashlight (hands stay
/// free) and completes PickFlashlight. Needs a collider so the crosshair can find it.
/// </summary>
public class FlashlightPickup : WorldInteractable
{
    public string prompt = "Take Flashlight";
    public TaskType onPickTask = TaskType.PickFlashlight;
    public AudioClip pickSFX;

    public override bool CanInteract(PickableItem held, out string promptText)
    {
        promptText = prompt;
        return true;
    }

    public override void Interact(PickDropController hands)
    {
        PlayerController player = GamePlayManager.Instance.player;
        if (player == null)
            return;

        player.Flashlight.Equip();
        if (pickSFX != null)
            AudioManager.Instance.PlaySFX(pickSFX);
        else
            AudioManager.Instance.PlaySFX(SFX.PickItem);

        ObjectiveUIController.OnTaskEventReceived(onPickTask);
        gameObject.SetActive(false);
    }
}
