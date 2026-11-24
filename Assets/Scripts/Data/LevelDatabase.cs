using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game's levels, in play order. LevelConfigLoader loads it by name from Resources,
/// so it has to stay at Assets/Resources/LevelDatabase.asset.
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Baby's Terror/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public const string ResourcePath = "LevelDatabase";

    [SerializeField] private LevelDefinition[] levels = Array.Empty<LevelDefinition>();

    [Header("Ending")]
    [Tooltip("Shown on the win screen of the last night, where the next night's teaser would go.")]
    public string endingTitle = "The End?";
    [TextArea(2, 4)] public string endingText;

    public IReadOnlyList<LevelDefinition> Levels => levels;
    public int Count => levels.Length;

    /// <summary>The night after this one, or null when it's the last.</summary>
    public LevelDefinition GetNext(int levelNumber) => Get(levelNumber + 1);

    public LevelDefinition Get(int levelNumber)
    {
        foreach (LevelDefinition definition in levels)
        {
            if (definition != null && definition.level == levelNumber)
                return definition;
        }

        return null;
    }

    private void OnValidate()
    {
        var seen = new HashSet<int>();
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                Debug.LogWarning($"[LevelDatabase] Slot {i} is empty.", this);
            else if (!seen.Add(levels[i].level))
                Debug.LogWarning($"[LevelDatabase] Level {levels[i].level} is listed more than once.", this);
        }
    }
}
