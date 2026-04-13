using UnityEngine;
using DG.Tweening;
using Ommy.Attributes;
using UnityEngine.Events;
using Ommy.Audio;

public class PlayerAnimationController : MonoBehaviour
{
    public Camera animatedCam, playerCam;

    [Header("Unconscious Settings")]
    public Vector3 unconsciousRotation = new Vector3(0f, 0f, 90f);
    public Vector3 positionOffset = new Vector3(0f, -0.5f, 0f);
    public float unconsciousDuration = 0.8f;

    [Header("Getting Up Settings")]
    public float gettingUpDuration = 1.2f;

    [Header("Impact & Polish")]
    public float impactShakeStrength = 0.15f;
    public int impactShakeVibrato = 12;
    public float stumbleSway = 8f;

    public UnityEvent onUnconsciousComplete;
    public UnityEvent onGettingUpComplete;

    private Sequence activeSequence;

    public void SetAnimatedCameraActive(bool isActive)
    {
        animatedCam.gameObject.SetActive(isActive);
        playerCam.gameObject.SetActive(!isActive);
    }

    public void SetAnimation(PlayerAnimation animation)
    {
        switch (animation)
        {
            case PlayerAnimation.Unconscious:
                AudioManager.Instance.PlaySFX(SFX.Scream);
                PlayUnconsciousAnimation();
                break;
            case PlayerAnimation.GettingUp:
                PlayGettingUpAnimation();
                break;
        }
    }

    public void SetAnimation(int animation)
    {
        SetAnimation((PlayerAnimation)animation);
    }

    [InspectorButton("PlayUnconsciousAnimation")]
    private void PlayUnconsciousAnimation()
    {
        KillActive();
        SetAnimatedCameraActive(true);

        animatedCam.transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation);

        Vector3 fallenPos = animatedCam.transform.localPosition + positionOffset;
        float dur = unconsciousDuration;

        var seq = DOTween.Sequence();

        // Phase 1 - dizzy sway before collapsing
        float swayDur = dur * 0.25f;
        seq.Append(
            animatedCam.transform.DOLocalRotate(
                animatedCam.transform.localEulerAngles + new Vector3(2f, 0f, stumbleSway * 0.4f),
                swayDur)
            .SetEase(Ease.InOutSine));

        // Phase 2 - main fall: camera drops and tilts to the side
        float fallDur = dur * 0.5f;
        seq.Append(
            animatedCam.transform.DOLocalRotate(unconsciousRotation, fallDur)
            .SetEase(Ease.InBack, 1.2f));
        seq.Join(
            animatedCam.transform.DOLocalMove(fallenPos, fallDur)
            .SetEase(Ease.InQuad));

        // Phase 3 - impact bounce + shake
        float bounceDur = dur * 0.25f;
        Vector3 bounceUp = fallenPos + new Vector3(0f, 0.06f, 0f);
        seq.Append(
            animatedCam.transform.DOLocalMove(bounceUp, bounceDur * 0.4f)
            .SetEase(Ease.OutQuad));
        seq.Append(
            animatedCam.transform.DOLocalMove(fallenPos, bounceDur * 0.6f)
            .SetEase(Ease.InQuad));
        seq.Join(
            animatedCam.transform.DOShakeRotation(bounceDur, impactShakeStrength * 40f, impactShakeVibrato)
            .SetEase(Ease.OutExpo));

        // Phase 4 - gentle breathing while on ground
        seq.Append(
            animatedCam.transform.DOLocalRotate(
                unconsciousRotation + new Vector3(1.5f, 0f, -1f),
                0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(2, LoopType.Yoyo));

        seq.OnComplete(() => onUnconsciousComplete?.Invoke());

        activeSequence = seq;
    }

    [InspectorButton("PlayGettingUpAnimation")]
    private void PlayGettingUpAnimation()
    {
        KillActive();
        SetAnimatedCameraActive(true);

        Vector3 standPos = playerCam.transform.localPosition;
        Vector3 standRot = playerCam.transform.localEulerAngles;
        float dur = gettingUpDuration;

        var seq = DOTween.Sequence();

        // Phase 1 - eyes flutter: tiny shake as if regaining consciousness
        seq.Append(
            animatedCam.transform.DOShakeRotation(dur * 0.15f, 3f, 6)
            .SetEase(Ease.OutSine));

        // Phase 2 - lift head off the ground
        Vector3 halfwayRot = new Vector3(standRot.x + 15f, standRot.y, stumbleSway * 0.3f);
        Vector3 halfwayPos = Vector3.Lerp(animatedCam.transform.localPosition, standPos, 0.5f);
        seq.Append(
            animatedCam.transform.DOLocalRotate(halfwayRot, dur * 0.35f)
            .SetEase(Ease.OutSine));
        seq.Join(
            animatedCam.transform.DOLocalMove(halfwayPos, dur * 0.35f)
            .SetEase(Ease.OutSine));

        // Phase 3 - pause and wobble mid-way (disoriented)
        seq.Append(
            animatedCam.transform.DOLocalRotate(
                halfwayRot + new Vector3(-2f, 0f, -stumbleSway * 0.2f),
                dur * 0.12f)
            .SetEase(Ease.InOutSine)
            .SetLoops(2, LoopType.Yoyo));

        // Phase 4 - stand fully upright
        seq.Append(
            animatedCam.transform.DOLocalRotate(standRot, dur * 0.3f)
            .SetEase(Ease.InOutQuad));
        seq.Join(
            animatedCam.transform.DOLocalMove(standPos, dur * 0.3f)
            .SetEase(Ease.InOutQuad));

        // Phase 5 - final stabilize shake
        seq.Append(
            animatedCam.transform.DOShakeRotation(dur * 0.08f, 1.5f, 4)
            .SetEase(Ease.OutSine));

        seq.OnComplete(() =>
        {
            SetAnimatedCameraActive(false);
            onGettingUpComplete?.Invoke();
        });

        activeSequence = seq;
    }

    private void KillActive()
    {
        if (activeSequence != null && activeSequence.IsActive())
            activeSequence.Kill();
    }

    private void OnDestroy()
    {
        KillActive();
    }
}
