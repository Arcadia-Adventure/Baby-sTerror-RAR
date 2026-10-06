using UnityEngine;
using DG.Tweening;
using Ommy.Audio;

public class AxeController : UseableItem
{
    [Header("Swing Settings")]
    public Vector3 windUpRotation = new (20f, 0f, 0f);      // Pull back
    public Vector3 swingRotation = new (-60f, 0f, 0f);      // Swing forward
    public float windUpDuration = 0.15f;
    public float swingDuration = 0.1f;
    public float returnDuration = 0.25f;

    [Header("Nanny Hit")]
    [Tooltip("How far in front of the camera a swing reaches.")]
    public float hitRange = 2f;
    public float hitRadius = 0.3f;
    [Tooltip("Layers a swing can hit. Walls on these layers block it. The HeldItem layer is always skipped.")]
    public LayerMask hitLayers = ~0;
    [Tooltip("Played when a swing lands on the Nanny. Empty falls back to the door-break SFX.")]
    public AudioClip hitNannySFX;
    
    private Vector3 originalRotation => holdRotationOffset;
    public bool isSwinging = false;

    public override void Start()
    {
        base.Start();
    }

    public override void UseDevice()
    {
        base.UseDevice();
        
        if (!isSwinging)
        {
            SwingAxe();
        }
    }

    private void SwingAxe()
    {
        isSwinging = true;
        
        // Wind up - pull back
        TweenUtilities.Rotate(rb, originalRotation + windUpRotation, windUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                TweenUtilities.LocalRotate(transform, originalRotation + swingRotation, swingDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        TryHitNanny();
                        TweenUtilities.LocalRotate(transform, originalRotation, returnDuration)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() => isSwinging = false);
                    });
            });
    }

    /// <summary>
    /// Cast from the camera rather than relying on the blade's collider, which the hold logic
    /// switches off whenever the axe lags behind the player's hands.
    /// </summary>
    private void TryHitNanny()
    {
        Camera playerCamera = GamePlayManager.Instance.player.PlayerCamera;
        Transform view = playerCamera != null ? playerCamera.transform : Camera.main.transform;
        int mask = hitLayers & ~(1 << LayerMask.NameToLayer("HeldItem"));

        if (!Physics.SphereCast(view.position, hitRadius, view.forward, out RaycastHit hit, hitRange,
                mask, QueryTriggerInteraction.Ignore))
            return;

        NannyStateManager nanny = hit.collider.GetComponentInParent<NannyStateManager>();
        if (nanny == null || !nanny.CanBeHurt)
            return;

        nanny.KnockDown();

        if (hitNannySFX != null) AudioManager.Instance.PlaySFX(hitNannySFX);
        else AudioManager.Instance.PlaySFX(SFX.DoorBreak);
    }
}
