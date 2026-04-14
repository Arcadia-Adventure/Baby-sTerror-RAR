using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelConfigLoader
{
    static LevelDatabase _database;
    static Dictionary<int, LevelData> _levelMap;

    public static LevelDatabase Database
    {
        get
        {
            if (_database == null) Load();
            return _database;
        }
    }

    public static void Load()
    {
        var json = Resources.Load<TextAsset>("levels");
        if (json == null)
        {
            Debug.LogError("[LevelConfigLoader] levels.json not found in Resources/");
            _database = new LevelDatabase { levels = Array.Empty<LevelData>() };
            _levelMap = new Dictionary<int, LevelData>();
            return;
        }

        _database = JsonUtility.FromJson<LevelDatabase>(json.text);
        _levelMap = new Dictionary<int, LevelData>();
        foreach (var level in _database.levels)
            _levelMap[level.level] = level;
    }

    public static LevelData GetLevelData(int levelNumber)
    {
        if (_levelMap == null) Load();
        if (_levelMap.TryGetValue(levelNumber, out var data))
            return data;

        Debug.LogError($"[LevelConfigLoader] No config found for level {levelNumber}");
        return null;
    }

    public static int LevelCount => Database.levels.Length;

    public static TaskType ParseTaskType(string value)
    {
        if (Enum.TryParse<TaskType>(value, out var result))
            return result;

        Debug.LogWarning($"[LevelConfigLoader] Unknown TaskType: {value}");
        return TaskType.None;
    }

    public static ItemType ParseItemType(string value)
    {
        if (Enum.TryParse<ItemType>(value, out var result))
            return result;

        Debug.LogWarning($"[LevelConfigLoader] Unknown ItemType: {value}");
        return ItemType.None;
    }

    public static BabyAnimationType ParseBabyAnimation(string value)
    {
        if (Enum.TryParse<BabyAnimationType>(value, out var result))
            return result;

        Debug.LogWarning($"[LevelConfigLoader] Unknown BabyAnimationType: {value}");
        return BabyAnimationType.None;
    }

    public static PlayerAnimation ParsePlayerAnimation(string value)
    {
        if (Enum.TryParse<PlayerAnimation>(value, out var result))
            return result;
        return PlayerAnimation.None;
    }
}
