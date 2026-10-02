using System;
using UnityEngine;

[Serializable, LevelMenu("Characters/Baby")]
public class BabyModule : LevelModule
{
    [Tooltip("Off hides the baby for the whole level.")]
    public bool active = true;
    [Tooltip("Item the baby needs before it can be helped. None means it needs nothing.")]
    public ItemType requireItem = ItemType.None;
    public bool canPickBaby = true;
    [Tooltip("Plays the horror sting the first time the baby is picked up.")]
    public bool playHorrorOnPick;
    public BabyAnimationType initialAnimation = BabyAnimationType.CrySit;
    [Tooltip("Plays this animation's sound instead of the initial animation's. None keeps the default.")]
    public BabyAnimationType overrideSound = BabyAnimationType.None;
    [Tooltip("Red eyes, floating, can't be pushed around.")]
    public bool possessed;
    public bool dirtyFace;
    [Tooltip("Dressed in clothes. Off keeps the diaper.")]
    public bool clothed;

    public override string Label => active ? $"Baby ({initialAnimation})" : "Baby (hidden)";

    public override void Setup(LevelContext ctx)
    {
        BabyController baby = ctx.Baby;
        if (baby == null)
            return;

        baby.requireItem = requireItem;
        baby.canPickBaby = canPickBaby;
        baby.playHorrorOnPick = playHorrorOnPick;

        if (dirtyFace)
            baby.babyDirtyFace.SetActive(true);

        if (clothed)
            baby.SetClothed(true);

        if (!active)
            baby.SetActiveAndPositionAndRotation(false, null);

        if (possessed)
        {
            baby.babyEyesRed.color = Color.red;
            baby.rb.isKinematic = true;
            baby.rb.useGravity = false;
        }
    }

    // In Begin rather than Setup so the level's drop points are already switched on.
    public override void Begin(LevelContext ctx)
    {
        BabyController baby = ctx.Baby;
        if (baby == null || !baby.gameObject.activeSelf || ctx.Scene == null)
            return;

        if (ctx.Scene.initDropPoint != null)
            ctx.Scene.initDropPoint.DropOnPoint(baby, jumpDuration: 0f, rotationDuration: 0f);
        else
            baby.SetActiveAndPositionAndRotation(ctx.Scene.babySpawnPoint != null, ctx.Scene.babySpawnPoint);

        baby.SetAnimation(initialAnimation, overrideSound: overrideSound);
    }
}
