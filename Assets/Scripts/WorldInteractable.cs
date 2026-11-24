using UnityEngine;

/// <summary>
/// Something in the world the player uses with the Interact button (E) rather than picking it up:
/// a hiding spot, a phone, a collectible, the flashlight on a shelf...
/// PickDropController shows the prompt when the crosshair is on it and calls Interact on the button.
/// </summary>
public abstract class WorldInteractable : Interactable
{
    [Tooltip("Optional icon for the Interact touch button while aiming at this.")]
    public Sprite buttonSprite;

    /// <summary>Whether the player can use this right now, and the prompt to show either way.</summary>
    public abstract bool CanInteract(PickableItem held, out string prompt);

    public abstract void Interact(PickDropController hands);

    public virtual CrosshairState PromptState => CrosshairState.Interact;
}
