using System;
using UnityEngine;

public enum PowerChange
{
    Off = 0,
    On = 1,
    /// <summary>Stutters, then comes back on.</summary>
    FlickerOn = 2,
    /// <summary>Stutters, then goes out.</summary>
    FlickerOff = 3,
}

[Serializable, LevelMenu("World/Set Power")]
public class SetPowerAction : LevelAction
{
    public PowerChange change = PowerChange.Off;
    [Tooltip("Flicker length in seconds.")]
    [Min(0f)] public float duration = 1.5f;

    public override string Label => $"Power {change}";

    public override void Run(LevelContext ctx)
    {
        HouseLights lights = HouseLights.Instance;
        if (lights == null)
        {
            Debug.LogWarning("[SetPowerAction] No HouseLights in the scene.", ctx.Definition);
            return;
        }

        switch (change)
        {
            case PowerChange.Off: lights.SetPower(false); break;
            case PowerChange.On: lights.SetPower(true); break;
            case PowerChange.FlickerOn: lights.Flicker(duration, endOn: true); break;
            case PowerChange.FlickerOff: lights.Flicker(duration, endOn: false); break;
        }
    }
}

[Serializable, LevelMenu("Player/Give Flashlight")]
public class GiveFlashlightAction : LevelAction
{
    public override void Run(LevelContext ctx)
    {
        if (ctx.Player != null)
            ctx.Player.Flashlight.Equip();
    }
}
