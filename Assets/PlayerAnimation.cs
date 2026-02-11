using UnityEngine;
using DG.Tweening;
using Ommy.Attributes;
using UnityEngine.Events;

public class PlayerAnimationController : MonoBehaviour
{
    public Camera animatedCam, playerCam;
    
    [Header("Unconscious Settings")]
    public Vector3 unconsciousRotation = new Vector3(0f, 0f, 90f);
    public Vector3 positionOffset = new Vector3(0f, -0.5f, 0f);
    public float unconsciousDuration = 0.8f;
    
    [Header("Getting Up Settings")]
    public float gettingUpDuration = 1.2f;
    public UnityEvent onUnconsciousComplete;
    public UnityEvent onGettingUpComplete;

    private void Start()
    {
    }
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
    private void PlayUnconsciousAnimation(bool farward = true)
    {
        SetAnimatedCameraActive(true);
        var initPos = farward ? playerCam.transform.position : animatedCam.transform.localPosition + positionOffset;
        var initRotation = farward ? playerCam.transform.rotation : Quaternion.Euler(unconsciousRotation);
        animatedCam.transform.SetPositionAndRotation(initPos, initRotation);
        // Camera falls and tilts to side
        var targetRotation = farward ? unconsciousRotation : playerCam.transform.localRotation.eulerAngles;
        animatedCam.transform.DOLocalRotate(targetRotation, unconsciousDuration)
            .SetEase(Ease.InQuad);
        var targetPosition = farward ? animatedCam.transform.localPosition + positionOffset : playerCam.transform.localPosition;
        animatedCam.transform.DOLocalMove(targetPosition, unconsciousDuration)
            .SetEase(Ease.InQuad).OnComplete(() =>
            {
                if(farward) onUnconsciousComplete?.Invoke();
            });
    }
    [InspectorButton("PlayGettingUpAnimation")]
    private void PlayGettingUpAnimation()
    {
        PlayUnconsciousAnimation(false);
    }
}
