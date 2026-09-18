using UnityEngine;
using DG.Tweening;

public class AxeController : UseableItem
{
    [Header("Swing Settings")]
    public Vector3 windUpRotation = new (20f, 0f, 0f);      // Pull back
    public Vector3 swingRotation = new (-60f, 0f, 0f);      // Swing forward
    public float windUpDuration = 0.15f;
    public float swingDuration = 0.1f;
    public float returnDuration = 0.25f;
    
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
                        TweenUtilities.LocalRotate(transform, originalRotation, returnDuration)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() => isSwinging = false);
                    });
            });
    }
}
