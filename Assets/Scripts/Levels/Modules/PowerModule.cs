using System;
using UnityEngine;

/// <summary>A level that starts in a power cut, and/or with the flashlight already in hand.</summary>
[Serializable, LevelMenu("World/Power")]
public class PowerModule : LevelModule
{
    [Tooltip("The house starts dark. Bring the lights back with a fuse in the fuse box (RestorePower).")]
    public bool startPowerOff = true;
    [Tooltip("The lights stutter for this long before going out. 0 starts dark.")]
    [Min(0f)] public float flickerBeforeCut = 1.5f;
    [Tooltip("The player starts with the flashlight.")]
    public bool giveFlashlight;

    public override string Label =>
        (startPowerOff ? "Power Cut" : "Power On") + (giveFlashlight ? " + Flashlight" : "");

    public override void Setup(LevelContext ctx)
    {
        HouseLights lights = HouseLights.Instance;
        if (startPowerOff)
        {
            if (lights == null)
                Debug.LogWarning("[PowerModule] No HouseLights in the scene, so the power can't be cut.", ctx.Definition);
            else if (flickerBeforeCut > 0f)
                lights.Flicker(flickerBeforeCut, endOn: false);
            else
                lights.SetPower(false, playSound: false);
        }
    }

    public override void Begin(LevelContext ctx)
    {
        if (giveFlashlight && ctx.Player != null)
            ctx.Player.Flashlight.Equip();
    }
}
