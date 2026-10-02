using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Runs a list of actions, in order, when its trigger fires.</summary>
[Serializable]
public abstract class LevelEventModule : LevelModule
{
    [Tooltip("Seconds to wait after the trigger before running the actions.")]
    [Min(0f)] public float delay;
    [SerializeReference, SubclassPicker] public List<LevelAction> actions = new();

    protected string DelaySuffix => delay > 0f ? $" +{delay:0.##}s" : "";

    protected void Fire(LevelContext ctx)
    {
        if (delay > 0f)
            ctx.Delay(delay, () => RunActions(ctx));
        else
            RunActions(ctx);
    }

    void RunActions(LevelContext ctx)
    {
        foreach (LevelAction action in actions)
        {
            if (action == null)
                continue;

            try
            {
                action.Run(ctx);
            }
            catch (Exception e)
            {
                Debug.LogException(e, ctx.Definition);
            }
        }
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (actions.Count == 0)
            problems.Add($"{Label} has no actions.");

        foreach (LevelAction action in actions)
        {
            if (action == null)
                problems.Add($"{Label} has an empty action slot.");
            else
                action.Validate(level, problems);
        }
    }
}

[Serializable, LevelMenu("Events/On Level Start")]
public class LevelStartEvent : LevelEventModule
{
    public override string Label => "On Level Start" + DelaySuffix;

    public override void Setup(LevelContext ctx) => Fire(ctx);
}

[Serializable, LevelMenu("Events/On Task Completed")]
public class TaskCompletedEvent : LevelEventModule
{
    public TaskType task = TaskType.None;

    public override string Label => $"On {task} Completed" + DelaySuffix;

    public override void AfterTaskCompleted(LevelContext ctx, TaskType completed)
    {
        if (completed == task)
            Fire(ctx);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (task == TaskType.None)
            problems.Add("On Task Completed event has no task.");
        else if (!level.HasTask(task))
            problems.Add($"{Label}: {task} isn't one of this level's tasks.");

        base.Validate(level, problems);
    }
}
