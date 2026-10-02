using System;
using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;

[Serializable, LevelMenu("World/Enable Drop Points")]
public class EnableDropPointsAction : LevelAction
{
    [Tooltip("Reference names of the DropPoints to switch on, e.g. Cradle, Washroom, RockingChair, HiddenDropPoint.")]
    public string[] dropPoints = Array.Empty<string>();

    public override string Label => $"Enable Drop Points ({string.Join(", ", dropPoints)})";

    public override void Run(LevelContext ctx)
    {
        var names = new HashSet<string>(dropPoints);
        foreach (DropPoint point in ctx.Manager.allDropPoints)
        {
            if (point != null && names.Contains(point.referenceName))
                point.gameObject.SetActive(true);
        }
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (dropPoints.Length == 0)
            problems.Add("Enable Drop Points lists no drop points.");
    }
}

[Serializable, LevelMenu("Hazards/Start Fire")]
public class StartFireAction : LevelAction
{
    [Tooltip("Key of the FireArea's SceneObjectTag.")]
    public SceneObjectKey fire;

    public override string Label => fire != null ? $"Start Fire ({fire.name})" : "Start Fire";

    public override void Run(LevelContext ctx) => ctx.Get<FireArea>(fire)?.ActivateFire();

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (fire == null)
            problems.Add("Start Fire has no fire key.");
    }
}

[Serializable, LevelMenu("Hazards/Stop Fire")]
public class StopFireAction : LevelAction
{
    [Tooltip("Key of the FireArea's SceneObjectTag.")]
    public SceneObjectKey fire;

    public override string Label => fire != null ? $"Stop Fire ({fire.name})" : "Stop Fire";

    public override void Run(LevelContext ctx) => ctx.Get<FireArea>(fire)?.DeactivateFire();

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (fire == null)
            problems.Add("Stop Fire has no fire key.");
    }
}

[Serializable, LevelMenu("Hazards/Flying Furniture")]
public class FlyingFurnitureAction : LevelAction
{
    [Tooltip("On makes the furniture float; off drops it back down.")]
    public bool fly = true;

    public override string Label => fly ? "Flying Furniture" : "Drop Furniture";

    public override void Run(LevelContext ctx) => ctx.Manager.SetupFlyingFurniture(fly);
}

[Serializable, LevelMenu("World/Lock Or Unlock Door")]
public class SetDoorLockedAction : LevelAction
{
    [Tooltip("Key of the door's SceneObjectTag.")]
    public SceneObjectKey door;
    public bool locked = true;

    public override string Label => $"{(locked ? "Lock" : "Unlock")} {(door != null ? door.name : "Door")}";

    public override void Run(LevelContext ctx) => ctx.Get<DoorController>(door)?.SetLocked(locked);

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (door == null)
            problems.Add("Lock Or Unlock Door has no door key.");
    }
}

[Serializable, LevelMenu("World/Show Or Hide Objects")]
public class SetObjectsActiveAction : LevelAction
{
    public SceneObjectKey[] objects = Array.Empty<SceneObjectKey>();
    public bool active = true;

    public override string Label => $"{(active ? "Show" : "Hide")} {objects.Length} object(s)";

    public override void Run(LevelContext ctx)
    {
        foreach (SceneObjectKey key in objects)
        {
            GameObject target = ctx.Get(key);
            if (target != null)
                target.SetActive(active);
        }
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (objects.Length == 0)
            problems.Add("Show Or Hide Objects lists no objects.");
    }
}

[Serializable, LevelMenu("Audio/Play Sound")]
public class PlaySoundAction : LevelAction
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    public override string Label => clip != null ? $"Play Sound ({clip.name})" : "Play Sound";

    public override void Run(LevelContext ctx)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, volume);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (clip == null)
            problems.Add("Play Sound has no clip.");
    }
}
