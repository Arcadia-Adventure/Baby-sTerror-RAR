using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, LevelMenu("World/Door")]
public class DoorModule : LevelModule
{
    [Tooltip("Key of the door's SceneObjectTag.")]
    public SceneObjectKey door;
    public bool locked = true;
    public DoorKnockSettings knocking = new();
    [Tooltip("Rings the doorbell at level start.")]
    public bool doorBell;

    public override string Label => door != null ? $"Door ({door.name})" : "Door";

    public override void Setup(LevelContext ctx)
    {
        var controller = ctx.Get<DoorController>(door);
        if (controller == null)
            return;

        controller.SetLocked(locked);

        if (knocking.enabled)
            controller.PlayDoorKnocking(knocking.initialDelay, knocking.interval);

        if (doorBell)
            controller.PlayDoorBell(true);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (door == null)
            problems.Add("Door module has no door key.");
    }
}

[Serializable]
public class DoorKnockSettings
{
    public bool enabled;
    [Min(0f)] public float initialDelay;
    [Min(0f)] public float interval;
}
