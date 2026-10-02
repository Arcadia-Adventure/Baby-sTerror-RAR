using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One night: its task list, and the modules that set up and drive everything else.</summary>
[CreateAssetMenu(fileName = "Level", menuName = "Baby's Terror/Level")]
public class LevelDefinition : ScriptableObject
{
    [Tooltip("Shown to the player as \"Night N\". Must be unique across the LevelDatabase.")]
    [Min(1)] public int level = 1;
    public string missionName;
    [Tooltip("Picture on this level's button in level select.")]
    public Sprite thumbnail;

    public TaskData[] tasks = Array.Empty<TaskData>();

    [Tooltip("What happens in this level. Modules run top to bottom.")]
    [SerializeReference, SubclassPicker] public List<LevelModule> modules = new();

    public bool HasTask(TaskType task)
    {
        foreach (TaskData data in tasks)
        {
            if (data != null && data.taskType == task)
                return true;
        }

        return false;
    }

    public T GetModule<T>() where T : LevelModule
    {
        foreach (LevelModule module in modules)
        {
            if (module is T match)
                return match;
        }

        return null;
    }

    /// <summary>Everything that won't work as set up. Empty when the level is fine.</summary>
    public List<string> Validate()
    {
        var problems = new List<string>();
        if (tasks.Length == 0)
            problems.Add("No tasks, so the level can never be completed.");

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] == null)
                problems.Add($"Module {i + 1} is empty (or its class was renamed). Pick a type or remove it.");
            else
                modules[i].Validate(this, problems);
        }

        return problems;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        GiveSharedEntriesTheirOwnInstance(modules);
        foreach (LevelModule module in modules)
        {
            if (module is LevelEventModule levelEvent)
                GiveSharedEntriesTheirOwnInstance(levelEvent.actions);
        }
    }

    // The list "+" button can copy a [SerializeReference] slot's reference rather than its object,
    // which would leave two slots editing the same module.
    static void GiveSharedEntriesTheirOwnInstance<T>(List<T> list) where T : class
    {
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i] == null)
                continue;

            for (int j = 0; j < i; j++)
            {
                if (ReferenceEquals(list[i], list[j]))
                {
                    list[i] = (T)Activator.CreateInstance(list[i].GetType());
                    break;
                }
            }
        }
    }
#endif
}

[Serializable]
public class TaskData
{
    public TaskType taskType;
    [TextArea(1, 3)] public string description;
    [Tooltip("Completing this task also completes every task listed before it.")]
    public bool completePreviousTasks;
}
