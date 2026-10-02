using System;
using UnityEngine;

/// <summary>
/// The scene half of a level: where things spawn, the Nanny's route, and the hint for each task.
/// Sits on the level's root object, which is only switched on while that level plays.
/// </summary>
public class LevelSceneSetup : MonoBehaviour
{
    [Tooltip("The level asset this object belongs to.")]
    public LevelDefinition definition;

    [Header("Spawns")]
    public Transform playerSpawnPoint;
    public Transform babySpawnPoint;
    [Tooltip("If set, the baby starts on this drop point instead of at its spawn point.")]
    public DropPoint initDropPoint;
    public CullingArea spawnCullingArea;

    [Header("Nanny")]
    [Tooltip("Where the Nanny appears. Keep it out of sight of the player spawn.")]
    public Transform nannySpawnPoint;
    [Tooltip("Parent whose children are her patrol waypoints, in order. Empty means she wanders.")]
    public Transform nannyPatrolRoute;

    [Header("Hints")]
    [Tooltip("The hint indicator for each task, in the same order as the level's tasks.")]
    public GameObject[] taskHints = Array.Empty<GameObject>();

    public int LevelNumber => definition != null ? definition.level : 0;

    public GameObject GetTaskHint(int taskIndex) =>
        taskHints != null && taskIndex >= 0 && taskIndex < taskHints.Length ? taskHints[taskIndex] : null;

    public Transform[] PatrolWaypoints()
    {
        if (nannyPatrolRoute == null)
            return Array.Empty<Transform>();

        var points = new Transform[nannyPatrolRoute.childCount];
        for (int i = 0; i < points.Length; i++)
            points[i] = nannyPatrolRoute.GetChild(i);
        return points;
    }

    public static LevelSceneSetup[] FindAll()
    {
        var setups = FindObjectsByType<LevelSceneSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Array.Sort(setups, (a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
        return setups;
    }

    public static LevelSceneSetup FindFor(LevelDefinition level)
    {
        if (level == null)
            return null;

        foreach (LevelSceneSetup setup in FindAll())
        {
            if (setup.definition == level)
                return setup;
        }

        return null;
    }

    void OnValidate()
    {
        int taskCount = definition != null && definition.tasks != null ? definition.tasks.Length : 0;
        taskHints ??= Array.Empty<GameObject>();
        if (taskHints.Length < taskCount)
            Array.Resize(ref taskHints, taskCount);
    }
}
