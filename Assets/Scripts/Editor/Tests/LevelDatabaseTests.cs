using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Guards the 20-night arc: the database, each night's data, and its scene half in GamePlay.unity.
/// Run from Window > General > Test Runner (EditMode).
/// </summary>
public class LevelDatabaseTests
{
    const int ExpectedNights = 20;
    const string ScenePath = "Assets/Scene/GamePlay.unity";

    static LevelDatabase Database => AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Resources/LevelDatabase.asset");

    [Test]
    public void Database_HasTwentyNights_NumberedOneToTwenty_InOrder()
    {
        LevelDatabase db = Database;
        Assert.IsNotNull(db, "Resources/LevelDatabase.asset is missing.");
        Assert.AreEqual(ExpectedNights, db.Count);

        for (int i = 0; i < db.Count; i++)
        {
            Assert.IsNotNull(db.Levels[i], $"Slot {i} is empty.");
            Assert.AreEqual(i + 1, db.Levels[i].level, $"Slot {i} holds night {db.Levels[i].level}.");
        }
    }

    [Test]
    public void EveryNight_ValidatesCleanly_AndFitsTheHud()
    {
        foreach (LevelDefinition def in Database.Levels)
        {
            List<string> problems = def.Validate();
            Assert.IsEmpty(problems, $"Night {def.level} ({def.missionName}): {string.Join("; ", problems)}");
            Assert.LessOrEqual(def.tasks.Length, LevelDefinition.MaxTasks, $"Night {def.level} has more tasks than the HUD shows.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(def.missionName), $"Night {def.level} has no mission name.");
            Assert.IsTrue(def.tasks.All(t => !string.IsNullOrWhiteSpace(t.description)), $"Night {def.level} has a task without text.");
        }
    }

    [Test]
    public void EveryNightAfterTheFirst_HasATeaser_ForTheCliffhanger()
    {
        // A night's teaser is shown on the previous night's win screen, so night 1's is never seen.
        foreach (LevelDefinition def in Database.Levels.Where(d => d.level > 1))
            Assert.IsFalse(string.IsNullOrWhiteSpace(def.teaser), $"Night {def.level} has no teaser.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(Database.endingText), "The last night's win screen needs ending text.");
    }

    [Test]
    public void NannyDamage_RampsFairly()
    {
        foreach (LevelDefinition def in Database.Levels)
        {
            NannyModule nanny = def.GetModule<NannyModule>();
            if (nanny == null || nanny.mode is NannyMode.None or NannyMode.Glimpse)
                continue;

            // 25 for nights 4-6, 34 for 7-15, 50 after; set-piece nights (every 5th) may spike one tier.
            float[] tiers = { 25f, 34f, 50f };
            int tier = def.level <= 6 ? 0 : def.level <= 15 ? 1 : 2;
            if (def.level % 5 == 0)
                tier = Mathf.Min(tier + 1, tiers.Length - 1);
            Assert.LessOrEqual(nanny.attackDamage, tiers[tier], $"Night {def.level} hits harder than the curve allows.");
            Assert.LessOrEqual(nanny.chaseSpeed, 4.2f, $"Night {def.level}: she'd outrun a sprinting player.");
        }
    }

    [Test]
    public void EveryNight_HasItsSceneHalf()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Dictionary<int, LevelSceneSetup> setups = LevelSceneSetup.FindAll()
                .Where(s => s.gameObject.scene == scene && s.definition != null)
                .ToDictionary(s => s.definition.level);

            foreach (LevelDefinition def in Database.Levels)
            {
                Assert.IsTrue(setups.TryGetValue(def.level, out LevelSceneSetup setup), $"Night {def.level} has no Level root in GamePlay.unity.");
                Assert.AreSame(def, setup.definition);
                Assert.IsFalse(setup.gameObject.activeSelf, $"Level ({def.level}) is saved active and would leak into other nights.");
                Assert.IsNotNull(setup.playerSpawnPoint, $"Night {def.level} has no player spawn.");
                Assert.IsNotNull(setup.spawnCullingArea, $"Night {def.level} has no culling area.");
                Assert.AreEqual(def.tasks.Length, setup.taskHints.Length, $"Night {def.level} needs one hint slot per task.");

                // A Glimpse appears where she's placed in the scene; anything that hunts needs a spawn and route.
                NannyModule nanny = def.GetModule<NannyModule>();
                if (nanny != null && nanny.mode is NannyMode.Patrol or NannyMode.Hunter)
                {
                    Assert.IsNotNull(setup.nannySpawnPoint, $"Night {def.level} has the Nanny but no spawn point.");
                    Assert.IsNotNull(setup.nannyPatrolRoute, $"Night {def.level} has the Nanny but no patrol route.");
                }

                if (def.HasTask(TaskType.CollectPieces))
                    Assert.Greater(setup.GetComponentsInChildren<Collectible>(true).Length, 0, $"Night {def.level} has nothing to collect.");
                if (def.HasTask(TaskType.PickKey))
                    Assert.IsNotNull(setup.GetComponentInChildren<KeyItem>(true), $"Night {def.level} has no key.");
                if (def.HasTask(TaskType.PickFlashlight))
                    Assert.IsNotNull(setup.GetComponentInChildren<FlashlightPickup>(true), $"Night {def.level} has no flashlight.");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
