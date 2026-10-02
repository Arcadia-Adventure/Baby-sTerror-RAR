using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plays one level's modules: the start phases, task reactions and the level's end.</summary>
public class LevelRunner
{
    readonly List<LevelModule> _modules = new();

    public LevelRunner(LevelContext context)
    {
        Context = context;
        if (context.Definition == null || context.Definition.modules == null)
            return;

        foreach (LevelModule module in context.Definition.modules)
        {
            if (module != null)
                _modules.Add(module);
        }
    }

    public LevelContext Context { get; }

    public void Start()
    {
        ForEach(module => module.Setup(Context));
        ForEach(module => module.Begin(Context));
    }

    public void TaskCompleted(TaskType task)
    {
        ForEach(module => module.OnTaskCompleted(Context, task));
        ForEach(module => module.AfterTaskCompleted(Context, task));
    }

    public void End(bool won)
    {
        if (Context.LevelEnded)
            return;

        Context.LevelEnded = true;
        ForEach(module => module.OnLevelEnded(Context, won));
    }

    // One broken module shouldn't stop the rest of the level from setting up.
    void ForEach(Action<LevelModule> call)
    {
        foreach (LevelModule module in _modules)
        {
            try
            {
                call(module);
            }
            catch (Exception e)
            {
                Debug.LogException(e, Context.Definition);
            }
        }
    }
}
