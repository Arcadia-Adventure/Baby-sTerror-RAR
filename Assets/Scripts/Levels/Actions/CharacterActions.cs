using System;
using UnityEngine;

[Serializable, LevelMenu("Player/Play Animation")]
public class PlayPlayerAnimationAction : LevelAction
{
    public PlayerAnimation animation = PlayerAnimation.None;
    [Tooltip("Waits for the Nanny's jump scare on the same trigger to finish, so the two cameras don't fight.")]
    public bool waitForNannyJumpScare;

    public override string Label => $"Player: {animation}" + (waitForNannyJumpScare ? " (after jump scare)" : "");

    public override void Run(LevelContext ctx)
    {
        if (animation == PlayerAnimation.None || ctx.Player == null)
            return;

        if (!waitForNannyJumpScare)
        {
            ctx.Player.SetAnimation(animation);
            return;
        }

        float remaining = ctx.NannyJumpScareEndsAt - Time.time;
        ctx.Delay(remaining, () => ctx.Player.SetAnimation(animation));
    }
}

[Serializable, LevelMenu("Baby/Set Required Item")]
public class SetBabyRequireItemAction : LevelAction
{
    [Tooltip("What the baby now asks for. None means it needs nothing.")]
    public ItemType item = ItemType.None;

    public override string Label => $"Baby needs {item}";

    public override void Run(LevelContext ctx)
    {
        if (ctx.Baby != null)
            ctx.Baby.requireItem = item;
    }
}
