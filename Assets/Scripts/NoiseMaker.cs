using Ommy.Audio;
using UnityEngine;

/// <summary>
/// A phone, TV or radio the player can set off to lure the Nanny away: she goes to look and lingers
/// there for a while. Completes DistractNanny only if she actually takes the bait.
/// Needs a collider so the crosshair can find it.
/// </summary>
public class NoiseMaker : WorldInteractable
{
    [Tooltip("Shown on the crosshair, e.g. \"Ring the phone\".")]
    public string prompt = "Make a noise";
    public AudioClip noiseClip;
    [Tooltip("3D source the noise plays from. One is added if empty.")]
    public AudioSource source;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Where she goes to look. Defaults to this object.")]
    public Transform investigatePoint;
    [Tooltip("Seconds she looks around once she gets there.")]
    [Min(0f)] public float lingerSeconds = 6f;
    [Tooltip("Seconds before it can be used again after luring her.")]
    [Min(0f)] public float cooldown = 20f;
    [Tooltip("Uses per level; 0 for unlimited.")]
    [Min(0)] public int maxUses;
    [Tooltip("Electric: does nothing during a power cut.")]
    public bool requiresPower;
    public TaskType onDistractTask = TaskType.DistractNanny;

    const float FailedCooldown = 3f;

    float _readyAt;
    int _uses;

    public override bool CanInteract(PickableItem held, out string promptText)
    {
        if (maxUses > 0 && _uses >= maxUses)
        {
            promptText = null;
            return false;
        }

        if (requiresPower && !HouseLights.IsPowerOn)
        {
            promptText = "No power";
            return false;
        }

        if (Time.time < _readyAt)
        {
            promptText = null;
            return false;
        }

        promptText = prompt;
        return true;
    }

    public override void Interact(PickDropController hands)
    {
        if (CanInteract(hands.heldPickable, out _))
            Trigger(byPlayer: true);
    }

    /// <summary>Plays the noise and sends the Nanny to look. Returns whether she took the bait.</summary>
    public bool Trigger(bool byPlayer)
    {
        PlayNoise();

        NannyStateManager nanny = GamePlayManager.Instance != null ? GamePlayManager.Instance.nanny : null;
        Vector3 point = investigatePoint != null ? investigatePoint.position : transform.position;
        bool lured = nanny != null && nanny.Investigate(point, lingerSeconds);

        _readyAt = Time.time + (lured ? cooldown : FailedCooldown);
        if (lured)
        {
            _uses++;
            if (byPlayer)
                ObjectiveUIController.OnTaskEventReceived(onDistractTask);
        }

        return lured;
    }

    void PlayNoise()
    {
        if (noiseClip == null)
            return;

        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 30f;
        }

        source.PlayOneShot(noiseClip, volume);
    }
}
