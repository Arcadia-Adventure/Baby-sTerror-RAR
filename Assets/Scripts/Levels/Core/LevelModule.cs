using System;
using System.Collections.Generic;

/// <summary>
/// One self-contained piece of a level: the baby, a door, the Nanny, a scripted event...
/// The runner calls every module in list order: Setup on all of them, then Begin on all of them,
/// then the task and end callbacks as the level plays out.
/// Instances live inside the level asset and outlast a play session, so keep runtime state in the
/// scene or the LevelContext rather than in fields.
/// </summary>
[Serializable]
public abstract class LevelModule : IInspectorLabel
{
    public virtual string Label => LevelMenuAttribute.NameOf(GetType());

    /// <summary>Puts the scene into this level's starting state.</summary>
    public virtual void Setup(LevelContext ctx) { }

    /// <summary>Runs once every module is set up, for work that depends on another module's Setup.</summary>
    public virtual void Begin(LevelContext ctx) { }

    public virtual void OnTaskCompleted(LevelContext ctx, TaskType task) { }

    /// <summary>Runs after every module's OnTaskCompleted for the same task, so it sees their reactions.</summary>
    public virtual void AfterTaskCompleted(LevelContext ctx, TaskType task) { }

    public virtual void OnLevelEnded(LevelContext ctx, bool won) { }

    /// <summary>Editor checks: describe anything that won't work in this level.</summary>
    public virtual void Validate(LevelDefinition level, List<string> problems) { }
}
