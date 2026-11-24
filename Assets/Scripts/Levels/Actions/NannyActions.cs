using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A mid-level change to the Nanny's tuning. Only the groups switched on are applied.</summary>
[Serializable]
public class NannyRetune
{
    public bool changeMode;
    public NannyMode mode = NannyMode.Hunter;

    [Space]
    public bool changeSpeeds;
    [Min(0f)] public float patrolSpeed = 1f;
    [Tooltip("The player walks at 3 and sprints at about 8.")]
    [Min(0f)] public float chaseSpeed = 3.5f;

    [Space]
    public bool changeSenses;
    [Min(0f)] public float viewDistance = 12f;
    [Range(0f, 360f)] public float viewAngle = 110f;
    [Min(0f)] public float hearingRadius = 4f;
    [Min(0f)] public float babyNoiseBonus = 3f;
    [Min(0f)] public float loseInterestTime = 8f;

    [Space]
    public bool changeThreat;
    [Min(0f)] public float attackDamage = 34f;
    [Tooltip("Hunter mode only: seconds between drifts toward the player.")]
    [Min(0f)] public float huntInterval = 12f;

    [Space]
    public bool changeCrawling;
    public bool crawling;

    public string Describe()
    {
        var parts = new List<string>();
        if (changeMode) parts.Add(mode.ToString());
        if (changeSpeeds) parts.Add($"chase {chaseSpeed:0.#}");
        if (changeSenses) parts.Add($"view {viewDistance:0.#}");
        if (changeThreat) parts.Add($"dmg {attackDamage:0}");
        if (changeCrawling) parts.Add(crawling ? "crawl" : "stand");
        return parts.Count > 0 ? string.Join(", ", parts) : "no change";
    }
}

[Serializable, LevelMenu("Nanny/Retune")]
public class ConfigureNannyAction : LevelAction
{
    public NannyRetune retune = new();
    [Tooltip("Bring her into the level first if she isn't in it yet.")]
    public bool activateIfInactive;
    [Tooltip("She screams and charges at where the player is right now.")]
    public bool alertToPlayer;

    public override string Label => $"Nanny: {retune.Describe()}" + (alertToPlayer ? " + alert" : "");

    public override void Run(LevelContext ctx)
    {
        NannyStateManager nanny = ctx.Nanny;
        if (nanny == null)
            return;

        nanny.Retune(retune);

        if (activateIfInactive && !nanny.gameObject.activeSelf)
            ctx.ActivateNanny();

        // A hidden player stays hidden: the final rush shouldn't home in on a wardrobe.
        if (alertToPlayer && nanny.gameObject.activeSelf && ctx.Player != null && !ctx.Player.IsHidden)
            nanny.AlertTo(ctx.Player.transform.position);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (level.GetModule<NannyModule>() == null)
            problems.Add("Nanny Retune is used, but the level has no Nanny module to configure her first.");
    }
}

[Serializable, LevelMenu("Nanny/Send To")]
public class SendNannyToAction : LevelAction
{
    [Tooltip("Where she goes to look. Empty sends her to where the player is right now (without her knowing it's them).")]
    public SceneObjectKey point;
    [Tooltip("Seconds she looks around once she gets there.")]
    [Min(0f)] public float linger = 4f;
    [Tooltip("Bring her into the level first if she isn't in it yet.")]
    public bool activateIfInactive = true;

    public override string Label => $"Nanny: go to {(point != null ? point.name : "player")}";

    public override void Run(LevelContext ctx)
    {
        NannyStateManager nanny = ctx.Nanny;
        if (nanny == null)
            return;

        if (activateIfInactive && !nanny.gameObject.activeSelf)
            ctx.ActivateNanny();

        Vector3 target;
        if (point != null)
        {
            GameObject go = ctx.Get(point);
            if (go == null)
                return;
            target = go.transform.position;
        }
        else if (ctx.Player != null)
        {
            target = ctx.Player.transform.position;
        }
        else
        {
            return;
        }

        nanny.Investigate(target, linger);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (level.GetModule<NannyModule>() == null)
            problems.Add("Nanny Send To is used, but the level has no Nanny module.");
    }
}
