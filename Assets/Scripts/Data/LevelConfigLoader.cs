using UnityEngine;

public static class LevelConfigLoader
{
    static LevelDatabase _database;

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
        _database = Resources.Load<LevelDatabase>(LevelDatabase.ResourcePath);
        if (_database != null)
            return;

        Debug.LogError($"[LevelConfigLoader] Resources/{LevelDatabase.ResourcePath}.asset not found");
        _database = ScriptableObject.CreateInstance<LevelDatabase>();
    }

    public static LevelDefinition GetLevelData(int levelNumber)
    {
        LevelDefinition data = Database.Get(levelNumber);
        if (data == null)
            Debug.LogError($"[LevelConfigLoader] No config found for level {levelNumber}");
        return data;
    }

    public static int LevelCount => Database.Count;
}
