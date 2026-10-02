using System;
using System.Collections.Generic;

/// <summary>A one-shot command, run by an event module when its trigger fires.</summary>
[Serializable]
public abstract class LevelAction : IInspectorLabel
{
    public virtual string Label => LevelMenuAttribute.NameOf(GetType());

    public abstract void Run(LevelContext ctx);

    /// <summary>Editor checks: describe anything that won't work in this level.</summary>
    public virtual void Validate(LevelDefinition level, List<string> problems) { }
}
