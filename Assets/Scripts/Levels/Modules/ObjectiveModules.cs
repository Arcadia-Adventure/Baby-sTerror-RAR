using System;
using System.Collections;
using System.Collections.Generic;
using Ommy.Audio;
using UnityEngine;

/// <summary>
/// "Find the 3 fragments": counts the Collectibles under this level's root, shows "(n/N)" on the
/// task line, and completes the task when the last one is taken.
/// </summary>
[Serializable, LevelMenu("Objectives/Collect Pieces")]
public class CollectPiecesModule : LevelModule
{
    public TaskType task = TaskType.CollectPieces;
    [Tooltip("Shown after the task's text. {0} = found, {1} = total.")]
    public string format = " ({0}/{1})";

    public override string Label => $"Collect Pieces ({task})";

    public override void Setup(LevelContext ctx)
    {
        if (ctx.Scene == null)
            return;

        Collectible[] pieces = ctx.Scene.GetComponentsInChildren<Collectible>(true);
        int total = pieces.Length;
        if (total == 0)
        {
            Debug.LogWarning($"[CollectPiecesModule] Level {ctx.Definition.level} has a {task} task but no Collectibles under its root.", ctx.Scene);
            return;
        }

        // Counted in a local the handlers share: modules must not keep play state in fields.
        int found = 0;
        ObjectiveUIController.Instance.SetTaskProgress(task, string.Format(format, found, total));

        void OnCollected(Collectible piece)
        {
            piece.Collected -= OnCollected;
            if (ctx.LevelEnded)
                return;

            found++;
            ObjectiveUIController.Instance.SetTaskProgress(task, string.Format(format, found, total));
            if (found >= total)
                ObjectiveUIController.OnTaskEventReceived(task);
        }

        foreach (Collectible piece in pieces)
            piece.Collected += OnCollected;
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (!level.HasTask(task))
            problems.Add($"Collect Pieces completes {task}, which isn't one of this level's tasks.");
    }
}

/// <summary>
/// "Survive until dawn": a countdown shown on the task line. Completes the task at zero, and can
/// run actions as time runs down (e.g. the Nanny turns Hunter with 60 seconds left).
/// </summary>
[Serializable, LevelMenu("Objectives/Survive")]
public class SurviveModule : LevelModule, IHasActionLists
{
    [Serializable]
    public class TimedActions
    {
        [Tooltip("Runs when this many seconds are left.")]
        [Min(0f)] public float atSecondsLeft = 60f;
        [SerializeReference, SubclassPicker] public List<LevelAction> actions = new();
    }

    [Min(1f)] public float seconds = 90f;
    [Tooltip("The clock starts when this task completes. None starts it with the level.")]
    public TaskType startOnTask = TaskType.None;
    public TaskType task = TaskType.Survive;
    [Tooltip("Shown after the task's text. {0} = minutes, {1} = seconds.")]
    public string format = " ({0}:{1:00})";
    [Tooltip("Optional tick played every second for the last 10 seconds.")]
    public AudioClip finalSecondsTick;
    public List<TimedActions> thresholds = new();

    public override string Label => $"Survive {seconds:0}s" + (startOnTask != TaskType.None ? $" after {startOnTask}" : "");

    public IEnumerable<List<LevelAction>> ActionLists()
    {
        foreach (TimedActions threshold in thresholds)
        {
            if (threshold != null)
                yield return threshold.actions;
        }
    }

    public override void Setup(LevelContext ctx)
    {
        ObjectiveUIController.Instance.SetTaskProgress(task, Format(seconds));
    }

    public override void Begin(LevelContext ctx)
    {
        if (startOnTask == TaskType.None)
            ctx.Run(Countdown(ctx));
    }

    public override void AfterTaskCompleted(LevelContext ctx, TaskType completed)
    {
        if (startOnTask != TaskType.None && completed == startOnTask)
            ctx.Run(Countdown(ctx));
    }

    IEnumerator Countdown(LevelContext ctx)
    {
        float remaining = seconds;
        int lastShown = -1;
        var fired = new bool[thresholds.Count];

        while (remaining > 0f)
        {
            if (ctx.LevelEnded)
                yield break;

            // Scaled time, so pausing the game pauses the clock.
            remaining -= Time.deltaTime;
            int whole = Mathf.Max(0, Mathf.CeilToInt(remaining));
            if (whole != lastShown)
            {
                lastShown = whole;
                ObjectiveUIController.Instance.SetTaskProgress(task, Format(whole));
                if (finalSecondsTick != null && whole > 0 && whole <= 10 && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(finalSecondsTick);
            }

            for (int i = 0; i < thresholds.Count; i++)
            {
                if (fired[i] || thresholds[i] == null || remaining > thresholds[i].atSecondsLeft)
                    continue;

                fired[i] = true;
                LevelAction.RunAll(ctx, thresholds[i].actions);
            }

            yield return null;
        }

        if (ctx.LevelEnded)
            yield break;

        ObjectiveUIController.Instance.SetTaskProgress(task, null);
        ObjectiveUIController.OnTaskEventReceived(task);
    }

    string Format(float secondsLeft)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
        return string.Format(format, total / 60, total % 60);
    }

    public override void Validate(LevelDefinition level, List<string> problems)
    {
        if (!level.HasTask(task))
            problems.Add($"Survive completes {task}, which isn't one of this level's tasks.");
        if (startOnTask != TaskType.None && !level.HasTask(startOnTask))
            problems.Add($"Survive starts on {startOnTask}, which isn't one of this level's tasks.");

        foreach (TimedActions threshold in thresholds)
        {
            if (threshold == null)
                continue;
            if (threshold.atSecondsLeft >= seconds)
                problems.Add($"Survive: a step at {threshold.atSecondsLeft:0}s left would run the moment the clock starts.");
            LevelAction.ValidateAll($"Survive at {threshold.atSecondsLeft:0}s", threshold.actions, level, problems);
        }
    }
}
