using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time move of the old index-matched level lists into the modular level system: a
/// LevelSceneSetup on every level object, key tags on shared objects, and generated level buttons.
/// Levels that already have a LevelSceneSetup are left alone, so running it again is harmless.
/// </summary>
public static class LevelSystemMigration
{
    const string GamePlayScenePath = "Assets/Scene/GamePlay.unity";
    const string LevelSelectionScenePath = "Assets/Scene/LevelSelection.unity";
    const string KeysFolder = "Assets/GameData/SceneKeys";

    [MenuItem("Baby's Terror/Migrate Scenes To Level System")]
    public static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Migrate scenes to the level system", "Stop Play mode first, then run this again.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog("Migrate scenes to the level system",
            "This opens GamePlay and LevelSelection, moves their level setup into the new components, " +
            "and saves both scenes. Every step can be undone with git.", "Migrate", "Cancel");
        if (!confirmed || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string startScene = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();

        MigrateGamePlay(report);
        MigrateLevelSelection(report);

        if (!string.IsNullOrEmpty(startScene))
            EditorSceneManager.OpenScene(startScene);

        Debug.Log("[LevelSystemMigration]\n" + report);
        EditorUtility.DisplayDialog("Migration finished", report.ToString(), "OK");
    }

    static void MigrateGamePlay(StringBuilder report)
    {
        Scene scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<GamePlayManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            report.AppendLine("GamePlay: no GamePlayManager found, skipped.");
            return;
        }

        var database = Resources.Load<LevelDatabase>(LevelDatabase.ResourcePath);
        var hints = Object.FindFirstObjectByType<HintManager>(FindObjectsInactive.Include);

        int created = 0;
        for (int i = 0; i < manager.levelConfigs.Count; i++)
        {
            GamePlayManager.LevelConfig config = manager.levelConfigs[i];
            int levelNumber = i + 1;
            if (config.levelObject == null)
            {
                report.AppendLine($"GamePlay: level {levelNumber} has no level object, skipped.");
                continue;
            }

            if (config.levelObject.GetComponent<LevelSceneSetup>() != null)
                continue;

            LevelDefinition definition = database != null ? database.Get(levelNumber) : null;
            var setup = Undo.AddComponent<LevelSceneSetup>(config.levelObject);
            setup.definition = definition;
            setup.playerSpawnPoint = config.playerSpawnPoint;
            setup.babySpawnPoint = config.babySpawnPoint;
            setup.initDropPoint = config.initDropPoint;
            setup.spawnCullingArea = config.spawnCullingArea;
            setup.nannySpawnPoint = config.nannySpawnPoint;
            setup.nannyPatrolRoute = config.nannyPatrolRoute;
            setup.taskHints = LegacyHints(hints, i, definition);
            EditorUtility.SetDirty(setup);
            created++;

            if (definition == null)
                report.AppendLine($"GamePlay: no level {levelNumber} in the LevelDatabase; assign the definition on {config.levelObject.name}.");
        }

        report.AppendLine($"GamePlay: added LevelSceneSetup to {created} level object(s).");

        Tag(manager.houseExitDoor != null ? manager.houseExitDoor.gameObject : null, "HouseExitDoor", report);
        Tag(manager.upperRoomDoor != null ? manager.upperRoomDoor.gameObject : null, "UpperRoomDoor", report);
        Tag(manager.bedroomFireArea != null ? manager.bedroomFireArea.gameObject : null, "BedroomFire", report);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static GameObject[] LegacyHints(HintManager hints, int levelIndex, LevelDefinition definition)
    {
        List<LevelObject.LevelTask> tasks = hints != null && hints.levelObjects != null && levelIndex < hints.levelObjects.Count
            ? hints.levelObjects[levelIndex]?.levelTasks
            : null;

        var indicators = new List<GameObject>();
        if (tasks != null)
        {
            foreach (LevelObject.LevelTask task in tasks)
                indicators.Add(task?.indicator);
        }

        // Level 10's hints predate its Banish Nanny task, so the hints after it are one slot early.
        // She moves around, so that task gets no hint of its own.
        int banishIndex = definition != null
            ? System.Array.FindIndex(definition.tasks, t => t != null && t.taskType == TaskType.BanishNanny)
            : -1;
        if (banishIndex >= 0 && indicators.Count == definition.tasks.Length - 1)
            indicators.Insert(banishIndex, null);

        while (definition != null && indicators.Count < definition.tasks.Length)
            indicators.Add(null);

        return indicators.ToArray();
    }

    static void Tag(GameObject target, string keyName, StringBuilder report)
    {
        var key = AssetDatabase.LoadAssetAtPath<SceneObjectKey>($"{KeysFolder}/{keyName}.asset");
        if (key == null)
        {
            report.AppendLine($"GamePlay: key asset {KeysFolder}/{keyName}.asset is missing, so nothing was tagged with it.");
            return;
        }

        if (target == null)
        {
            report.AppendLine($"GamePlay: no object to tag as {keyName}; add a SceneObjectTag with that key by hand.");
            return;
        }

        foreach (SceneObjectTag existing in target.GetComponents<SceneObjectTag>())
        {
            if (existing.key == key)
                return;
        }

        var tag = Undo.AddComponent<SceneObjectTag>(target);
        tag.key = key;
        EditorUtility.SetDirty(tag);
        report.AppendLine($"GamePlay: tagged {target.name} as {keyName}.");
    }

    static void MigrateLevelSelection(StringBuilder report)
    {
        Scene scene = EditorSceneManager.OpenScene(LevelSelectionScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<LevelSelectionManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            report.AppendLine("LevelSelection: no LevelSelectionManager found, skipped.");
            return;
        }

        if (manager.buttonTemplate != null)
        {
            report.AppendLine("LevelSelection: already has a button template, skipped.");
            return;
        }

        // Level 2's button becomes the template because, unlike level 1's, it has the lock panel.
        GameObject templateObject = FindTemplateButton(manager);
        if (templateObject == null)
        {
            report.AppendLine("LevelSelection: couldn't find \"Button (2)\" to use as the template, skipped.");
            return;
        }

        var template = Undo.AddComponent<LevelButton>(templateObject);
        template.AutoWire();
        EditorUtility.SetDirty(template);

        Button button = templateObject.GetComponent<Button>();
        Undo.RecordObject(button, "Clear template click");
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        var handPlaced = new List<GameObject>();
        foreach (Transform sibling in templateObject.transform.parent)
        {
            if (sibling.gameObject != templateObject && sibling.name.StartsWith("Button (") && sibling.GetComponent<Button>() != null)
                handPlaced.Add(sibling.gameObject);
        }

        foreach (GameObject old in handPlaced)
            Undo.DestroyObjectImmediate(old);

        Undo.RecordObject(templateObject, "Hide level button template");
        templateObject.name = "Level Button Template";
        templateObject.SetActive(false);

        Undo.RecordObject(manager, "Assign level button template");
        manager.buttonTemplate = template;
        manager.lockSprite = new GameObject[0];
        manager.barImg = new GameObject[0];
        EditorUtility.SetDirty(manager);

        report.AppendLine($"LevelSelection: made a button template and removed {handPlaced.Count} hand-placed button(s).");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static GameObject FindTemplateButton(LevelSelectionManager manager)
    {
        if (manager.scrollView != null && manager.scrollView.content != null)
        {
            Transform byName = manager.scrollView.content.Find("Button (2)");
            if (byName != null)
                return byName.gameObject;
        }

        GameObject firstLock = manager.lockSprite != null && manager.lockSprite.Length > 0 ? manager.lockSprite[0] : null;
        return firstLock != null && firstLock.transform.parent != null ? firstLock.transform.parent.gameObject : null;
    }
}
