using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, LevelMenu("Characters/Nanny")]
public class NannyModule : LevelModule
{
    public NannyMode mode = NannyMode.Patrol;
    [Tooltip("She appears when this task completes. None means she appears at level start, after Spawn Delay.")]
    public TaskType activateOnTask = TaskType.None;
    [Min(0f)] public float spawnDelay;
    [Tooltip("When this task completes she appears in the player's face and screams.")]
    public TaskType jumpScareOnTask = TaskType.None;
    public bool crawling;

    [Header("Movement")]
    [Min(0f)] public float patrolSpeed = 0.8f;
    [Tooltip("The player walks at 3 and sprints at 7.")]
    [Min(0f)] public float chaseSpeed = 3f;

    [Header("Senses")]
    [Min(0f)] public float viewDistance = 10f;
    [Range(0f, 360f)] public float viewAngle = 110f;
    [Min(0f)] public float hearingRadius = 4f;
    [Tooltip("Extra hearing radius while the player carries the baby.")]
    [Min(0f)] public float babyNoiseBonus = 3f;
    [Min(0f)] public float loseInterestTime = 6f;

    [Header("Threat")]
    [Min(0f)] public float attackDamage = 34f;
    [Tooltip("Hunter mode only: seconds between drifts toward the player.")]
    [Min(0f)] public float huntInterval = 15f;
    [Tooltip("Seconds she bangs on a closed door before bursting through.")]
    [Min(0f)] public float doorBreakTime = 2.5f;

    public override string Label => $"Nanny ({mode})";

    public override void Setup(LevelContext ctx)
    {
        if (ctx.Nanny == null || mode == NannyMode.None)
            return;

        ctx.Nanny.Configure(this, ctx.Scene != null ? ctx.Scene.PatrolWaypoints() : Array.Empty<Transform>());

        if (activateOnTask == TaskType.None)
            ctx.Delay(spawnDelay, () => Activate(ctx));
    }

    public override void OnTaskCompleted(LevelContext ctx, TaskType task)
    {
        if (ctx.Nanny == null || mode == NannyMode.None)
            return;

        if (task == activateOnTask)
            Activate(ctx);

        if (task == jumpScareOnTask)
        {
            ctx.Nanny.JumpScare(ctx.Player);
            ctx.NannyJumpScareEndsAt = Time.time + ctx.Nanny.JumpScareDuration;
        }
    }

    public override void OnLevelEnded(LevelContext ctx, bool won)
    {
        if (won && ctx.Nanny != null)
            ctx.Nanny.StandDown();
    }

    static void Activate(LevelContext ctx)
    {
        if (ctx.LevelEnded || ctx.Nanny == null)
            return;

        ctx.Nanny.Activate(ctx.Scene != null ? ctx.Scene.nannySpawnPoint : null);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (activateOnTask != TaskType.None && !level.HasTask(activateOnTask))
            problems.Add($"Nanny appears on {activateOnTask}, which isn't one of this level's tasks.");
        if (jumpScareOnTask != TaskType.None && !level.HasTask(jumpScareOnTask))
            problems.Add($"Nanny jump-scares on {jumpScareOnTask}, which isn't one of this level's tasks.");
    }
}
