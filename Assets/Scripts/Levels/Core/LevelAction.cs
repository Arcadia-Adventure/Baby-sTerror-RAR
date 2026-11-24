using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A one-shot command, run by an event module when its trigger fires.</summary>
[Serializable]
public abstract class LevelAction : IInspectorLabel
{
    public virtual string Label => LevelMenuAttribute.NameOf(GetType());

    public abstract void Run(LevelContext ctx);

    /// <summary>Editor checks: describe anything that won't work in this level.</summary>
    public virtual void Validate(LevelDefinition level, List<string> problems) { }

    /// <summary>Runs every action in order. One that throws doesn't stop the rest.</summary>
    public static void RunAll(LevelContext ctx, List<LevelAction> actions)
    {
        if (actions == null)
            return;

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

    public static void ValidateAll(string owner, List<LevelAction> actions, LevelDefinition level, List<string> problems)
    {
        if (actions == null)
            return;

        foreach (LevelAction action in actions)
        {
            if (action == null)
                problems.Add($"{owner} has an empty action slot.");
            else
                action.Validate(level, problems);
        }
    }
}

/// <summary>
/// A module that holds [SerializeReference] action lists, so the editor can stop two list slots
/// from sharing one action object.
/// </summary>
public interface IHasActionLists
{
    IEnumerable<List<LevelAction>> ActionLists();
}
