using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Ommy.Audio;

public class BabyController : PickableItem
{
    public static BabyController Instance { get; private set; }

    public float cryThreshold = 10f;
    public bool canPickBaby = true;
    public bool playHorrorOnPick;

    [SerializeField] AudioClip firstPickHorrorSFX;
    bool _playedFirstPickHorror;

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
    [SerializeField] private RagdollController ragdoll;

    [Header("Ragdoll Carry")]
    [Tooltip("Animation played while carried. Body, head and face keep animating; only arms and legs go physical.")]
    [SerializeField] private BabyAnimationType carryPose = BabyAnimationType.Fly;
    [Tooltip("Go fully limp when dropped, then get back up.")]
    [SerializeField] private bool fullRagdollOnDrop = true;

    public GameObject diaper;
    public GameObject clothBody;
    public GameObject body;
    public Material babyEyesRed;
    public GameObject babyDirtyFace;

    private CapsuleCollider _capsule;
    private int _standingDirection;
    private Vector3 _standingCenter;
    Coroutine _ragdollRecover;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (ragdoll == null)
            ragdoll = GetComponent<RagdollController>();
    }

    public override void Start()
    {
        base.Start();
        _capsule = collider as CapsuleCollider;
        if (_capsule != null)
        {
            _standingDirection = _capsule.direction;
            _standingCenter = _capsule.center;
        }
    }

    public void SetClothed(bool clothed)
    {
        body.SetActive(!clothed);
        diaper.SetActive(!clothed);
        clothBody.SetActive(clothed);
    }

    public override void PickObject(Transform parent)
    {
        StopRagdollRecover();

        base.PickObject(parent);
        StopAudio();
        SetAnimation(carryPose);
        TryPlayFirstPickHorror();

        // Only the arms and legs go physical, so the carried pose and the face
        // keep playing exactly as before.
        if (ragdoll != null)
            ragdoll.EnableLimbRagdoll(parent);
    }

    void TryPlayFirstPickHorror()
    {
        if (!playHorrorOnPick || _playedFirstPickHorror || firstPickHorrorSFX == null)
            return;

        _playedFirstPickHorror = true;
        AudioManager.Instance.PlaySFX(firstPickHorrorSFX);
    }

    public override void ReleaseObject()
    {
        StopRagdollRecover();
        if (ragdoll != null)
        {
            ragdoll.DisableLimbRagdoll();
            if (ragdoll.IsHeld || ragdoll.IsRagdoll)
                ragdoll.DisableRagdoll(true);
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.linearDamping = 1;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        if (collider != null)
            collider.enabled = true;
        transform.parent = null;
        Vector3 targetRotation = new Vector3(0f, transform.eulerAngles.y, 0f);
        TweenUtilities.Rotate(transform, targetRotation, 0.3f).SetEase(Ease.OutSine);
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    public override void DropObject()
    {
        if (ragdoll != null && fullRagdollOnDrop)
        {
            DropAsRagdoll();
            return;
        }

        if (ragdoll != null)
            ragdoll.DisableLimbRagdoll();
        base.DropObject();
        OnDropBaby();
    }

    void DropAsRagdoll()
    {
        if (glowParticle != null) glowParticle.Play();
        if (dropSFX != null) AudioManager.Instance.PlaySFX(dropSFX);
        else AudioManager.Instance.PlaySFX(SFX.DropItem);

        Vector3 throwForce = transform.forward * 1f + Vector3.up * 2f;
        ragdoll.DisableLimbRagdoll();
        ragdoll.EnableRagdoll(throwForce);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        if (collider != null)
            collider.enabled = false;
        transform.parent = null;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        OnDrop?.Invoke();

        StopRagdollRecover();
        _ragdollRecover = StartCoroutine(RecoverWhenSettled());
    }

    IEnumerator RecoverWhenSettled()
    {
        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        while (elapsed < 2.2f)
        {
            var hips = ragdoll != null ? ragdoll.Hips : null;
            if (hips != null && hips.linearVelocity.magnitude < 0.4f)
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _ragdollRecover = null;
        if (ragdoll != null && ragdoll.IsRagdoll)
            ragdoll.DisableRagdoll(true);

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearDamping = 1;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        if (collider != null)
            collider.enabled = true;

        OnDropBaby();
    }

    void StopRagdollRecover()
    {
        if (_ragdollRecover == null)
            return;
        StopCoroutine(_ragdollRecover);
        _ragdollRecover = null;
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
        if (!active) return;
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

    public void SetAnimation(BabyAnimationType animationType, bool withAudio = true,
        BabyAnimationType overrideSound = BabyAnimationType.None, Action onComplete = null)
    {
        if (withAudio)
            PlayAudio(overrideSound != BabyAnimationType.None ? overrideSound : animationType);
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
