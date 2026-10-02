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

    public IReadOnlyList<LevelDefinition> Levels => levels;
    public int Count => levels.Length;

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
