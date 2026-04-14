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
    public ItemType requireItem = ItemType.None;
    [SerializeField] private BabyAnimationController babyAnimationController;
    [SerializeField] private BabyAudioController babyAudioController;
    [SerializeField] private BabyItemHandler babyItemHandler;
    public GameObject diaper;
    public GameObject clothBody;
    public GameObject body;
    public Material babyEyesRed;
    public GameObject babyDirtyFace;

    public override void Start()
    {
        base.Start();
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
    public void GiveItemToBaby(PickableItem item) => babyItemHandler.GiveItemToBaby(item);

    public void PlayAudio(BabyAnimationType animationType) =>
        babyAudioController.Play(animationType);

    public void StopAudio() => babyAudioController.Stop();

    public void SetAnimation(BabyAnimationType animationType, bool withAudio = true, Action onComplete = null)
    {
        if (withAudio) PlayAudio(animationType);
        babyAnimationController.SetAnimation(animationType, onComplete);
    }
}