using System;
using Ommy.Audio;
using UnityEngine;

/// <summary>
/// One of a level's pieces to find (talisman fragments, diary pages...). Put them under the level's
/// root; a Collect Pieces module counts them and completes its task when the last one is taken.
/// Needs a collider so the crosshair can find it.
/// </summary>
public class Collectible : WorldInteractable
{
    [Tooltip("Shown in the prompt: \"Take <name>\".")]
    public string displayName = "Fragment";
    public AudioClip collectSFX;
    [Tooltip("Optional glow or sparkle that's switched off once taken.")]
    public ParticleSystem glow;

    public event Action<Collectible> Collected;
    public bool IsCollected { get; private set; }

    public override bool CanInteract(PickableItem held, out string prompt)
    {
        prompt = IsCollected ? null : "Take " + displayName;
        return !IsCollected;
    }

    public override void Interact(PickDropController hands)
    {
        if (IsCollected)
            return;

        IsCollected = true;
        if (collectSFX != null)
            AudioManager.Instance.PlaySFX(collectSFX);
        else
            AudioManager.Instance.PlaySFX(SFX.PickItem);

        if (glow != null)
            glow.Stop();

        Collected?.Invoke(this);
        gameObject.SetActive(false);
    }
}
