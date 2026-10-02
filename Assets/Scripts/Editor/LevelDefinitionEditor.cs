using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDefinition))]
public class LevelDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        foreach (string problem in ((LevelDefinition)target).Validate())
            EditorGUILayout.HelpBox(problem, MessageType.Warning);

        DrawDefaultInspector();
    }

    [MenuItem("Baby's Terror/Validate Levels")]
    public static void ValidateLevels()
    {
        var database = Resources.Load<LevelDatabase>(LevelDatabase.ResourcePath);
        if (database == null)
        {
            Debug.LogError($"[Levels] Resources/{LevelDatabase.ResourcePath}.asset not found.");
            return;
        }

        int problemCount = 0;
        foreach (LevelDefinition level in database.Levels)
        {
            if (level == null)
                continue;

            foreach (string problem in level.Validate())
            {
                Debug.LogWarning($"[Levels] {level.name}: {problem}", level);
                problemCount++;
            }
        }

        if (problemCount == 0)
            Debug.Log($"[Levels] All {database.Count} levels look fine.", database);
        else
            Debug.LogWarning($"[Levels] {problemCount} problem(s) found, listed above.", database);
    }
}
