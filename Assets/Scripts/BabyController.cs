using System;
using Ommy.Prefs;
using UnityEngine;
using DG.Tweening;
using Ommy.Audio;

public class BabyController : PickableItem
{
    public static BabyController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }
    public float cryThreshold = 10f;
    public bool canPickBaby = true;

    [SerializeField] private ItemType _requireItem = ItemType.None;
    [SerializeField] private RequireItemIndicator requireItemIndicator;

    public ItemType requireItem
    {
        get => _requireItem;
        set
        {
            _requireItem = value;
            if (requireItemIndicator != null)
                requireItemIndicator.SetItem(value);
        }
    }

    [SerializeField] private BabyAnimationController babyAnimationController;
    [SerializeField] private BabyAudioController babyAudioController;
    [SerializeField] private BabyItemHandler babyItemHandler;
    public GameObject diaper;
    public GameObject clothBody;
    public GameObject body;
    public Material babyEyesRed;
    public GameObject babyDirtyFace;

    private CapsuleCollider _capsule;
    private int _standingDirection;
    private Vector3 _standingCenter;

    public override void Start()
    {
        base.Start();
        _capsule = collider as CapsuleCollider;
        if (_capsule != null)
        {
            _standingDirection = _capsule.direction;
            _standingCenter = _capsule.center;
        }
        if (GamePreference.selectedLevel > 4)
        {
            body.SetActive(false);
            diaper.SetActive(false);
            clothBody.SetActive(true);
        }
    }
    public override void PickObject(Transform parent)
    {
        base.PickObject(parent);
        StopAudio();
        babyAnimationController.SetAnimation(BabyAnimationType.Fly);
    }
    public override void ReleaseObject()
    {
        rb.useGravity = true;
        rb.linearDamping = 1;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        collider.enabled = true;
        transform.parent = null;
        Vector3 targetRotation = new Vector3(0f, transform.eulerAngles.y, 0f);
        transform.DORotate(targetRotation, 0.3f).SetEase(Ease.OutSine);
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    public override void DropObject()
    {
        base.DropObject();
        OnDropBaby();
    }
    public void OnDropBaby()
    {
        SetAnimation(BabyAnimationType.Drop,
            onComplete: () =>
            {
                SetAnimation(BabyAnimationType.CryStand);
            }
        );
    }
    public void SetActiveAndPositionAndRotation(bool active, Transform targetTransform)
    {
        gameObject.SetActive(active);
        if(!active) return;
        transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
    }
    public override void Detected()
    {
        if (_requireItem != ItemType.None)
            requireItemIndicator?.Show();
    }

    public override void Undetected()
    {
        requireItemIndicator?.Hide();
    }

    public void GiveItemToBaby(PickableItem item) => babyItemHandler.GiveItemToBaby(item);

    public void PlayAudio(BabyAnimationType animationType) =>
        babyAudioController.Play(animationType);

    public void StopAudio() => babyAudioController.Stop();

    public void SetAnimation(BabyAnimationType animationType, bool withAudio = true, Action onComplete = null)
    {
        if (withAudio) PlayAudio(animationType);
        babyAnimationController.SetAnimation(animationType, onComplete);
        UpdateColliderForAnimation(animationType);
    }

    private void UpdateColliderForAnimation(BabyAnimationType animationType)
    {
        if (_capsule == null) return;

        if (animationType == BabyAnimationType.CryLay)
        {
            _capsule.direction = 2; // Z-axis (laying down)
            _capsule.center = new Vector3(_standingCenter.x, _standingCenter.z, _standingCenter.y);
        }
        else
        {
            _capsule.direction = _standingDirection;
            _capsule.center = _standingCenter;
        }
    }

}