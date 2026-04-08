using DG.Tweening;
using Ommy.Prefs;
using UnityEngine;

public class BabyController : PickableItem
{
    public static BabyController Instance;
    private void Awake() 
    {
        if(Instance == null) Instance = this;
    }
    public bool canPickBaby = true;
    public ItemType requireItem = ItemType.None;
    [SerializeField] private BabyAnimationController babyAnimationController;
    [SerializeField] private BabyAudioController babyAudioController;
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
    public override void DropObject()
    {
        base.DropObject();
    }
    public void SetActiveAndPositionAndRotation(bool active, Transform targetTransform)
    {
        gameObject.SetActive(active);
        if(!active) return;
        transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
    }
    public void GiveItemToBaby(PickableItem item)
    {
        print("item: "+ item.itemType+" given to baby");
        item.ReleaseObject();
        item.rb.isKinematic = true;
        item.collider.enabled = false;
        item.transform.DOLocalJump(transform.position, 0.5f, 1, 0.5f);
        item.transform.DORotate(transform.eulerAngles, 0.5f);
        ObjectiveManager.OnTaskEventReceived(item.OnDropForBabyTaskType);
        SetAnimation(BabyAnimationType.Happy);
        DOVirtual.DelayedCall(0.5f, () =>
        {
            requireItem = ItemType.None;
            Destroy(item.gameObject);
        });
    }

    public void PlayAudio(BabyAnimationType animationType, bool loop) =>
        babyAudioController.Play(animationType, loop);

    public void StopAudio() => babyAudioController.Stop();

    public void MuteAudio(bool mute) => babyAudioController.Mute(mute);

    public void SetAnimation(BabyAnimationType animationType)
    {
        switch(animationType)
        {
            case BabyAnimationType.Cry:
                PlayAudio(animationType, true);
                break;
            case BabyAnimationType.Happy:
            case BabyAnimationType.AngrySit:
                PlayAudio(animationType, false);
                break;
        }
        babyAnimationController.SetAnimation(animationType);
    }
}