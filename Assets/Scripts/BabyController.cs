using System;
using Ommy.Prefs;
using UnityEngine;
using DG.Tweening;

public class BabyController : PickableItem
{
    public static BabyController Instance;
    private void Awake() 
    {
        if(Instance == null) Instance = this;
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
        collider.enabled = true;
        transform.parent = null;
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