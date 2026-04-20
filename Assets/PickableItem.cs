using System;
using DG.Tweening;
using Ommy.Attributes;
using Ommy.Audio;
using SickscoreGames.HUDNavigationSystem;
using UnityEngine;

public class PickableItem : Interactable
{
    public ItemType itemType = ItemType.None;
    public Vector3 holdPositionOffset = new (0.3f, 0, 0.8f);
    [Header("runtime offset update")]
    public bool SetRotationOffset = false;
    public Vector3 holdRotationOffset = Vector3.zero;
    public string itemTag;
    public ParticleSystem glowParticle;
    public Rigidbody rb;
    public DropPoint currentDropPoint;
    public HUDNavigationElement hUDNavigationElement;
    public AudioClip pickSFX, dropSFX;
    public Action OnPick,OnDrop;

    [Header("Objective Settings")]
    [Tooltip("Task to complete when this item is picked up. Set to None to disable.")]
    public TaskType OnPickTaskType = TaskType.None;
    public TaskType OnDropForBabyTaskType = TaskType.None;
    [InspectorButton("SetupPickable")]
    public void SetupPickable()
    {
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        hUDNavigationElement = GetComponentInChildren<HUDNavigationElement>(true);
        glowParticle = GetComponentInChildren<ParticleSystem>();
    }
    public override void Start() 
    {
        base.Start();
        itemTag = gameObject.tag;
    }
    public virtual void PickObject(Transform parent)
    {
        if(pickSFX != null) AudioManager.Instance.PlaySFX(pickSFX);
        else AudioManager.Instance.PlaySFX(SFX.PickItem);
        if (glowParticle != null) glowParticle.Stop();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 10;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        transform.parent = parent;
        transform.localPosition = holdPositionOffset;
        transform.localEulerAngles = holdRotationOffset;
        gameObject.layer = LayerMask.NameToLayer("HeldItem");
        ObjectiveUIController.OnTaskEventReceived(OnPickTaskType);
        if(currentDropPoint != null)
        {
            currentDropPoint.ClearDropPoint();
            currentDropPoint = null;
        }
        OnPick?.Invoke();
    }
    public virtual void Update()
    {
        if(SetRotationOffset) 
        {
            transform.localEulerAngles = holdRotationOffset;
        }
    }

    public virtual void DropObject()
    {
        if (glowParticle != null) glowParticle.Play();
        rb.AddForce(transform.forward * 1, ForceMode.Impulse);
        rb.AddForce(transform.up * 2, ForceMode.Impulse);
        ReleaseObject();
        if(dropSFX != null) AudioManager.Instance.PlaySFX(dropSFX);
        else AudioManager.Instance.PlaySFX(SFX.DropItem);
        OnDrop?.Invoke();
    }
    public virtual void ReleaseObject()
    {
        rb.useGravity = true;
        rb.linearDamping = 1;
        rb.constraints = RigidbodyConstraints.None;
        collider.enabled = true;
        transform.parent = null;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    protected virtual void OnDisable()
    {
        DOTween.Kill(transform);
    }

    protected virtual void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
