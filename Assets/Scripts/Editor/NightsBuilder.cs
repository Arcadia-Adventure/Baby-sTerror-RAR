using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SickscoreGames.HUDNavigationSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the 20-night arc. Run the menu items in order (or "Run All"):
///   0. Install Scene Features (NightsSceneSetup): hiding spots, lures, fuse box, buttons...
///   1. Renumber Existing Nights: the 10 original level assets move to their new slots (GUIDs kept).
///   2. Write Night Data: tasks, modules, tuning and teasers for all 20 nights, plus the database.
///   3. Build Level Roots: the scene half (spawns, items, Nanny routes, hints) for each night.
/// Each step is safe to run again.
/// </summary>
public static class NightsBuilder
{
    const string LevelsFolder = "Assets/GameData/Levels";
    const string DatabasePath = "Assets/Resources/LevelDatabase.asset";

    /// <summary>The original ten nights by asset GUID, and the slot each moves to.</summary>
    static readonly (string guid, int level)[] Originals =
    {
        ("b4e27db69c5542d4b91371b4ab9d77a1", 1),
        ("e762ee3c385f40b78b3a0a3dbe36cad3", 2),
        ("093dd3aaff444189b3dd0723a5eb6ae2", 3),
        ("b39bdb2d6c32457baf9fcf4b81dd9f16", 4),
        ("12116f38462446b9a1e78796831893fa", 5),
        ("f6b1fb77686f4f5290c5255fd4d4f7ef", 6),
        ("55b123087c9d449eb4833089c4420771", 10),
        ("2356bc1b6a6d4111885a6963e6de7c91", 11),
        ("87481e28fcc843aea11a15e4e5e60527", 14),
        ("e066cf69b84d48509383e411d8bd276d", 20),
    };

    static readonly int[] NewNights = { 7, 8, 9, 12, 13, 15, 16, 17, 18, 19 };

    static string LevelPath(int level) => $"{LevelsFolder}/Level {level:00}.asset";

    [MenuItem("Baby's Terror/20 Nights/Run All", priority = 0)]
    public static void RunAll()
    {
        NightsSceneSetup.InstallAll();
        RenumberExisting();
        WriteNightData();
        BuildLevelRoots();
        LevelDefinitionReport();
    }

    #region 1. Renumber

    [MenuItem("Baby's Terror/20 Nights/1. Renumber Existing Nights")]
    public static void RenumberExisting()
    {
        var moves = new List<(string path, LevelDefinition def, int level)>();
        foreach ((string guid, int level) in Originals)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (def == null)
            {
                Debug.LogError($"[NightsBuilder] Original level asset {guid} not found.");
                return;
            }

            moves.Add((path, def, level));
        }

        // Two passes, since e.g. old 7 -> "Level 10" would collide with old 10 still being there.
        foreach ((string path, LevelDefinition def, int level) in moves)
        {
            if (Path.GetFileNameWithoutExtension(path) != $"Level {level:00}")
                Rename(path, $"__night_{level:00}");
        }

        foreach ((_, LevelDefinition def, int level) in moves)
        {
            string path = AssetDatabase.GetAssetPath(def);
            if (Path.GetFileNameWithoutExtension(path) != $"Level {level:00}")
                Rename(path, $"Level {level:00}");

            if (def.level != level)
            {
                def.level = level;
                EditorUtility.SetDirty(def);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NightsBuilder] Original nights renumbered: 1-6 stay, 7->10, 8->11, 9->14, 10->20.");
    }

    static void Rename(string path, string newName)
    {
        string error = AssetDatabase.RenameAsset(path, newName);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"Rename {path} -> {newName}: {error}");
    }

    #endregion

    #region 2. Night data

    static SceneObjectKey ExitDoor, NurseryDoor, NorthDoor, Fire;

    [MenuItem("Baby's Terror/20 Nights/2. Write Night Data")]
    public static void WriteNightData()
    {
        ExitDoor = NightsSceneSetup.EnsureKey("HouseExitDoor");
        NurseryDoor = NightsSceneSetup.EnsureKey("UpperRoomDoor");
        NorthDoor = NightsSceneSetup.EnsureKey("FirstFloorDoor");
        Fire = NightsSceneSetup.EnsureKey("BedroomFire");

        var defs = new Dictionary<int, LevelDefinition>();
        for (int level = 1; level <= 20; level++)
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath(level));
            if (def == null)
            {
                if (!NewNights.Contains(level))
                {
                    Debug.LogError($"[NightsBuilder] {LevelPath(level)} is missing. Run step 1 first.");
                    return;
                }

                def = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(def, LevelPath(level));
            }

            def.level = level;
            defs[level] = def;
        }

        PatchOriginals(defs);
        FillNewNights(defs);

        foreach (LevelDefinition def in defs.Values)
            EditorUtility.SetDirty(def);

        WriteDatabase(defs);
        AssetDatabase.SaveAssets();
        Debug.Log("[NightsBuilder] Night data written for all 20 nights.");
    }

    static void WriteDatabase(Dictionary<int, LevelDefinition> defs)
    {
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        var so = new SerializedObject(db);
        SerializedProperty levels = so.FindProperty("levels");
        levels.arraySize = defs.Count;
        int i = 0;
        foreach (LevelDefinition def in defs.OrderBy(p => p.Key).Select(p => p.Value))
            levels.GetArrayElementAtIndex(i++).objectReferenceValue = def;

        so.FindProperty("endingTitle").stringValue = "The baby sleeps... for now.";
        so.FindProperty("endingText").stringValue =
            "You broke her curse. But old houses keep their secrets. More nights are coming soon.";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(db);
    }

    /// <summary>The original ten nights keep their scripting; they get teasers and the retunes from the plan.</summary>
    static void PatchOriginals(Dictionary<int, LevelDefinition> d)
    {
        d[1].teaser = "A new job. A crying baby. A doorbell that won't stop ringing.";

        d[2].teaser = "The baby won't stop crying. Something in the kitchen is waiting.";

        d[3].teaser = "Who was the woman in the kitchen? The baby needs changing... and she's still here.";
        d[3].showFootstepTrail = true;

        LevelDefinition n4 = d[4];
        n4.teaser = "The house is freezing. Footsteps downstairs. Stay low: she listens.";
        n4.tasks = new[]
        {
            Task(TaskType.PickCloth, "-Find warm clothes for the baby. Crouch to move quietly."),
            Task(TaskType.DropClothOnBaby, "-Get back to the baby. Something is following you."),
        };
        SetNanny(n4, damage: 25f);

        LevelDefinition n5 = d[5];
        n5.missionName = "Footsteps on the Stairs";
        n5.teaser = "She heard you last night. Tonight she's coming to the nursery.";
        n5.tasks = new[]
        {
            Task(TaskType.PickToy, "-Find a toy to cheer up the baby. Stay out of her sight."),
            Task(TaskType.DropToyOnBaby, "-Give the toy to the baby."),
            Task(TaskType.Hide, "-She's coming up the stairs! Hide in the wardrobe and wait."),
        };
        SetNanny(n5, damage: 25f);
        n5.modules.RemoveAll(m => m is TaskCompletedEvent e && e.task == TaskType.DropToyOnBaby);
        n5.modules.Add(OnTask(TaskType.DropToyOnBaby, 1.5f,
            Sound("Assets/GameSounds/ghosts-rumble-moan_f1acdSNO_NWM.mp3", 0.8f),
            new SendNannyToAction { point = null, linger = 6f, activateIfInactive = true }));

        LevelDefinition n6 = d[6];
        n6.teaser = "You hid. She left. But when you opened the wardrobe, the cradle was empty.";
        n6.tasks = new[]
        {
            Task(TaskType.PickBaby, "-The cradle is empty... Find the baby hiding in the house."),
            Task(TaskType.DropBabyCradle, "-Bring the baby back to the cradle. Its crying draws her: crouch to hush it."),
        };
        SetNanny(n6, damage: 25f);

        d[10].missionName = "Trapped by Fire";
        d[10].teaser = "Tonight she locks you in with the fire.";

        d[11].teaser = "You escaped the flames, but the fire is still burning. Who lit it?";
        SetNanny(d[11], damage: 34f);

        d[14].teaser = "The baby is calling for you... from somewhere it shouldn't be.";
        // The scripted knockout night sits before the night-15 set piece, so it follows the 34 curve.
        SetNanny(d[14], damage: 34f);

        d[20].teaser = "The talisman is whole again. Face her. End this.";
    }

    static void FillNewNights(Dictionary<int, LevelDefinition> d)
    {
        Sprite Thumb(int from) => d[from].thumbnail;

        // 7 — Lights Out: introduces the power cut, flashlight and fuse box.
        Night(d[7], "Lights Out", Thumb(14),
            "Then every light in the house went out.",
            new[]
            {
                Task(TaskType.PickFlashlight, "-The power is out. Find the flashlight."),
                Task(TaskType.PickFuse, "-Find a spare fuse downstairs."),
                Task(TaskType.RestorePower, "-Put the fuse in the fuse box in the downstairs bathroom."),
            },
            new PowerModule { startPowerOff = true, flickerBeforeCut = 2f },
            Door(ExitDoor, true),
            Baby(anim: BabyAnimationType.Idle, canPick: false),
            Nanny(NannyMode.Glimpse, 0.8f, 3.2f, 10f, 3.5f, 3f, 34f, spawnDelay: 6f),
            OnTask(TaskType.PickFuse, 0f, new ConfigureNannyAction
            {
                activateIfInactive = true,
                retune = Retune(NannyMode.Patrol, 0.8f, 3.2f, 10f, 3.5f, 3f, 34f, 15f),
            }));

        // 8 — Locked In: introduces keys.
        Night(d[8], "Locked In", Thumb(10),
            "The lights are back... and the nursery door is locked. The baby is crying behind it.",
            new[]
            {
                Task(TaskType.PickKey, "-Find the key to the nursery."),
                Task(TaskType.PickToy, "-Find a toy to calm the baby."),
                Task(TaskType.UnlockDoor, "-Unlock the nursery door upstairs."),
                Task(TaskType.DropToyOnBaby, "-Give the toy to the baby."),
            },
            Door(ExitDoor, true),
            Door(NurseryDoor, true),
            Baby(require: ItemType.Toy, anim: BabyAnimationType.CrySit),
            Nanny(NannyMode.Patrol, 0.8f, 3.3f, 10f, 3.5f, 3f, 34f, spawnDelay: 3f));

        // 9 — The Ringing Phone: introduces the noise lure.
        Night(d[9], "The Ringing Phone", Thumb(2),
            "She's waiting in the kitchen with the baby's bottle. Give her something else to listen to.",
            new[]
            {
                Task(TaskType.DistractNanny, "-She's guarding the kitchen. Ring the old phone in the hall to lure her away."),
                Task(TaskType.PickFeeder, "-Grab the baby's bottle from the kitchen."),
                Task(TaskType.DropFeederOnBaby, "-Feed the baby upstairs."),
            },
            Door(ExitDoor, true),
            Baby(require: ItemType.Feeder, anim: BabyAnimationType.CrySit),
            Nanny(NannyMode.Patrol, 0.7f, 3.4f, 11f, 3.5f, 3f, 34f, spawnDelay: 1f, angle: 120f, lose: 7f));

        // 12 — Until Dawn: introduces the survive timer, with the Nanny escalating as it runs down.
        Night(d[12], "Until Dawn", Thumb(6),
            "She won't stop until morning. Survive until dawn.",
            new[]
            {
                Task(TaskType.PickAxe, "-Find something to defend yourself."),
                Task(TaskType.Survive, "-Survive until dawn. Hide, crouch, run"),
            },
            Door(ExitDoor, true),
            Baby(anim: BabyAnimationType.Idle, canPick: false),
            Nanny(NannyMode.Patrol, 0.9f, 3.4f, 10f, 4f, 3f, 34f, spawnDelay: 6f),
            Survive(120f, TaskType.PickAxe,
                (60f, new LevelAction[] { new ConfigureNannyAction { retune = Retune(NannyMode.Hunter, hunt: 12f, damage: 34f) } }),
                (20f, new LevelAction[]
                {
                    new ConfigureNannyAction { alertToPlayer = true, retune = new NannyRetune { changeSpeeds = true, patrolSpeed = 1f, chaseSpeed = 3.8f } },
                })));

        // 13 — Pieces of Her: introduces collectibles, in the dark.
        Night(d[13], "Pieces of Her", Thumb(20),
            "The old talisman was shattered. Its pieces are hidden in the dark, and so is she.",
            new[]
            {
                Task(TaskType.PickFlashlight, "-The lights died again. Find the flashlight."),
                Task(TaskType.CollectPieces, "-Find the talisman fragments"),
            },
            new PowerModule { startPowerOff = true, flickerBeforeCut = 2.5f },
            Door(ExitDoor, true),
            Baby(anim: BabyAnimationType.AngrySit, canPick: false, possessed: true),
            Nanny(NannyMode.Hunter, 0.9f, 3.5f, 10f, 4f, 3f, 34f, spawnDelay: 8f, hunt: 16f),
            new CollectPiecesModule());

        // 15 — Wake Up: set piece. Dark + key + hide, straight after the knockout of night 14.
        Night(d[15], "Wake Up", Thumb(14),
            "You wake on the floor, in the dark, behind a locked door.",
            new[]
            {
                Task(TaskType.PickFlashlight, "-You're locked in. Find the flashlight."),
                Task(TaskType.PickKey, "-Search the room for the key."),
                Task(TaskType.UnlockDoor, "-Unlock the door."),
                Task(TaskType.Hide, "-She heard the lock! Hide in the wardrobe outside."),
            },
            new PowerModule { startPowerOff = true, flickerBeforeCut = 0f },
            Door(ExitDoor, true),
            Door(NorthDoor, true),
            Baby(anim: BabyAnimationType.AngrySit, canPick: false, possessed: true),
            OnStart(0f, new PlayPlayerAnimationAction { animation = PlayerAnimation.GettingUp }),
            Nanny(NannyMode.Hunter, 1f, 3.8f, 12f, 4f, 3f, 50f, activateOn: TaskType.UnlockDoor, hunt: 14f),
            OnTask(TaskType.UnlockDoor, 2.5f, new SendNannyToAction { point = null, linger = 6f }));

        // 16 — Her Room: lure + key, breather after the set piece.
        Night(d[16], "Her Room", Thumb(4),
            "There's a room upstairs she never lets anyone open. Tonight, you will.",
            new[]
            {
                Task(TaskType.PickKey, "-Find the key to her room. It's somewhere in the kitchen."),
                Task(TaskType.DistractNanny, "-She's guarding her door. Turn on the TV to draw her downstairs."),
                Task(TaskType.UnlockDoor, "-Unlock her room upstairs."),
                Task(TaskType.CollectPieces, "-Find her diary"),
            },
            Door(ExitDoor, true),
            Door(NorthDoor, true),
            Baby(anim: BabyAnimationType.Idle, canPick: false),
            Nanny(NannyMode.Patrol, 0.8f, 3.5f, 11f, 4f, 3f, 34f, spawnDelay: 1f),
            new CollectPiecesModule());

        // 17 — Blackout Hunt: full blackout + collect + restore power.
        Night(d[17], "Blackout Hunt", Thumb(11),
            "Her diary told you everything. The rest of the talisman is in this house. The power is not.",
            new[]
            {
                Task(TaskType.PickFlashlight, "-Find the flashlight."),
                Task(TaskType.CollectPieces, "-Find the last talisman fragments"),
                Task(TaskType.PickFuse, "-Find a fuse."),
                Task(TaskType.RestorePower, "-Restore the power at the fuse box."),
            },
            new PowerModule { startPowerOff = true, flickerBeforeCut = 1.5f },
            Door(ExitDoor, true),
            Baby(anim: BabyAnimationType.AngrySit, canPick: false, possessed: true),
            Nanny(NannyMode.Hunter, 1f, 3.8f, 11f, 4f, 4f, 50f, spawnDelay: 5f, hunt: 13f),
            new CollectPiecesModule());

        // 18 — One Last Lullaby: the care routine under pressure.
        Night(d[18], "One Last Lullaby", Thumb(5),
            "The baby is slipping away. Feed it, warm it, and don't let her near.",
            new[]
            {
                Task(TaskType.PickFeeder, "-Find the baby's bottle."),
                Task(TaskType.DropFeederOnBaby, "-Feed the baby on the rocking chair."),
                Task(TaskType.PickCloth, "-Find warm clothes upstairs."),
                Task(TaskType.DropClothOnBaby, "-Dress the baby."),
            },
            Door(ExitDoor, true),
            Baby(require: ItemType.Feeder, anim: BabyAnimationType.CrySit, canPick: false, possessed: true, clothed: false),
            Nanny(NannyMode.Hunter, 0.9f, 3.8f, 11f, 4f, 5f, 50f, spawnDelay: 3f, hunt: 14f),
            // After the feeder: the baby clears its need 0.5s after an item lands, so wait past that.
            OnTask(TaskType.DropFeederOnBaby, 1f, new SetBabyRequireItemAction { item = ItemType.Cloth }));

        // 19 — The Longest Night: fire, then a long survive with two escalations.
        Night(d[19], "The Longest Night", Thumb(11),
            "One more night. The house is burning, and she has never been angrier.",
            new[]
            {
                Task(TaskType.PickFireExtinguisher, "-Fire! Find the fire extinguisher."),
                Task(TaskType.FireEnded, "-Put out the fire in the nursery."),
                Task(TaskType.Survive, "-Survive until dawn"),
            },
            Door(ExitDoor, true),
            Baby(anim: BabyAnimationType.Idle, canPick: false),
            OnStart(0f, new StartFireAction { fire = Fire }, new FlyingFurnitureAction { fly = true }),
            Nanny(NannyMode.Patrol, 0.9f, 3.5f, 10f, 4f, 3f, 50f, spawnDelay: 6f, hunt: 12f),
            Survive(150f, TaskType.FireEnded,
                (100f, new LevelAction[] { new ConfigureNannyAction { retune = Retune(NannyMode.Hunter, hunt: 12f, damage: 50f) } }),
                (50f, new LevelAction[]
                {
                    new ConfigureNannyAction { alertToPlayer = true, retune = new NannyRetune { changeSpeeds = true, patrolSpeed = 1.1f, chaseSpeed = 4f } },
                })));
    }

    #endregion

    #region Data helpers

    static void Night(LevelDefinition def, string mission, Sprite thumbnail, string teaser, TaskData[] tasks, params LevelModule[] modules)
    {
        def.missionName = mission;
        if (def.thumbnail == null)
            def.thumbnail = thumbnail;
        def.teaser = teaser;
        def.tasks = tasks;
        def.showFootstepTrail = false;
        def.modules = modules.ToList();
    }

    static TaskData Task(TaskType type, string description, bool completePrevious = false) =>
        new TaskData { taskType = type, description = description, completePreviousTasks = completePrevious };

    static void SetNanny(LevelDefinition def, float damage)
    {
        NannyModule nanny = def.GetModule<NannyModule>();
        if (nanny != null)
            nanny.attackDamage = damage;
    }

    static DoorModule Door(SceneObjectKey key, bool locked) => new DoorModule { door = key, locked = locked };

    static BabyModule Baby(ItemType require = ItemType.None, BabyAnimationType anim = BabyAnimationType.CrySit,
        bool canPick = true, bool possessed = false, bool clothed = true) =>
        new BabyModule
        {
            active = true,
            requireItem = require,
            canPickBaby = canPick,
            initialAnimation = anim,
            possessed = possessed,
            clothed = clothed,
        };

    static NannyModule Nanny(NannyMode mode, float patrol, float chase, float view, float hearing, float babyNoise, float damage,
        float spawnDelay = 0f, TaskType activateOn = TaskType.None, float hunt = 15f, float lose = 6f, float angle = 110f) =>
        new NannyModule
        {
            mode = mode,
            activateOnTask = activateOn,
            spawnDelay = spawnDelay,
            patrolSpeed = patrol,
            chaseSpeed = chase,
            viewDistance = view,
            viewAngle = angle,
            hearingRadius = hearing,
            babyNoiseBonus = babyNoise,
            loseInterestTime = lose,
            attackDamage = damage,
            huntInterval = hunt,
            doorBreakTime = 2.5f,
        };

    static NannyRetune Retune(NannyMode mode, float patrol, float chase, float view, float hearing, float babyNoise, float damage, float hunt) =>
        new NannyRetune
        {
            changeMode = true, mode = mode,
            changeSpeeds = true, patrolSpeed = patrol, chaseSpeed = chase,
            changeSenses = true, viewDistance = view, viewAngle = 110f, hearingRadius = hearing, babyNoiseBonus = babyNoise, loseInterestTime = 6f,
            changeThreat = true, attackDamage = damage, huntInterval = hunt,
        };

    static NannyRetune Retune(NannyMode mode, float hunt, float damage) =>
        new NannyRetune { changeMode = true, mode = mode, changeThreat = true, huntInterval = hunt, attackDamage = damage };

    static LevelStartEvent OnStart(float delay, params LevelAction[] actions) =>
        new LevelStartEvent { delay = delay, actions = actions.ToList() };

    static TaskCompletedEvent OnTask(TaskType task, float delay, params LevelAction[] actions) =>
        new TaskCompletedEvent { task = task, delay = delay, actions = actions.ToList() };

    static PlaySoundAction Sound(string path, float volume) =>
        new PlaySoundAction { clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path), volume = volume };

    static SurviveModule Survive(float seconds, TaskType startOn, params (float left, LevelAction[] actions)[] steps) =>
        new SurviveModule
        {
            seconds = seconds,
            startOnTask = startOn,
            finalSecondsTick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameSounds/Generated/clock-tick.wav"),
            thresholds = steps.Select(s => new SurviveModule.TimedActions { atSecondsLeft = s.left, actions = s.actions.ToList() }).ToList(),
        };

    #endregion

    #region 3. Scene roots

    // Nanny routes through the house (snapped onto her NavMesh when built).
    static readonly Vector3[] RouteGround = { new(-6f, 0f, 5.5f), new(-5f, 0f, 10.5f), new(4.5f, 0f, 10.5f), new(3f, 0f, 5f), new(-6f, 0f, 2f) };
    static readonly Vector3[] RouteKitchen = { new(-8.5f, 0f, 10.5f), new(-4f, 0f, 10.5f), new(-5f, 0f, 9.3f), new(-1f, 0f, 10.5f) };
    static readonly Vector3[] RouteUpstairs = { new(-6f, 3f, 2.5f), new(3f, 3f, 2.5f), new(-7f, 3f, -1.5f), new(-2f, 3f, 2.5f) };
    static readonly Vector3[] RouteGuardNorthDoor = { new(-2f, 3f, 2.5f), new(3f, 3f, 2.5f), new(-6f, 3f, 2.5f), new(1f, 3f, -1f) };
    static readonly Vector3[] RouteWhole = { new(-6f, 0f, 5.5f), new(-5f, 0f, 10.5f), new(3f, 0f, 5f), new(-6f, 3f, 2.5f), new(3f, 3f, 2.5f), new(-7f, 3f, -1.5f) };

    static readonly Vector3 Kitchen = new(-5f, 0f, 10.5f);
    static readonly Vector3 StairsBottom = new(-4f, 0f, -1.5f);
    static readonly Vector3 UpstairsCommon = new(-7f, 3f, -1.5f);
    static readonly Vector3 UpstairsHall = new(-6f, 3f, 2.5f);
    static readonly Vector3 NorthEast = new(4.5f, 0f, 10.5f);

    /// <summary>Nanny spawn and route for every night she appears in (none of the originals had one).</summary>
    static readonly Dictionary<int, (Vector3 spawn, Vector3[] route)> NannyPlan = new()
    {
        [4] = (Kitchen, RouteGround),
        [5] = (Kitchen, RouteGround),
        [6] = (Kitchen, RouteGround),
        [7] = (StairsBottom, RouteGround),
        [8] = (Kitchen, RouteGround),
        [9] = (Kitchen, RouteKitchen),
        [10] = (UpstairsHall, RouteWhole),
        [11] = (UpstairsCommon, RouteWhole),
        [12] = (UpstairsHall, RouteWhole),
        [13] = (NorthEast, RouteGround),
        [14] = (Kitchen, RouteGround),
        [15] = (UpstairsCommon, RouteUpstairs),
        [16] = (new Vector3(-2f, 3f, 2.5f), RouteGuardNorthDoor),
        [17] = (UpstairsCommon, RouteWhole),
        [18] = (Kitchen, RouteWhole),
        [19] = (Kitchen, RouteWhole),
        [20] = (UpstairsCommon, RouteWhole),
    };

    [MenuItem("Baby's Terror/20 Nights/3. Build Level Roots")]
    public static void BuildLevelRoots() => BuildLevelRoots(rebuildNewNights: false);

    [MenuItem("Baby's Terror/20 Nights/Rebuild New Night Roots (overwrites hand edits)")]
    public static void RebuildNewNightRoots() => BuildLevelRoots(rebuildNewNights: true);

    static void BuildLevelRoots(bool rebuildNewNights)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != NightsSceneSetup.GamePlayScene)
            scene = EditorSceneManager.OpenScene(NightsSceneSetup.GamePlayScene);

        Transform levelObjects = NightsSceneSetup.Find("LevelObjects").transform;
        var setups = LevelSceneSetup.FindAll().Where(s => s.definition != null).ToDictionary(s => s.definition.level);

        // Renumbered originals: names follow their (GUID-linked) definitions.
        foreach (LevelSceneSetup setup in setups.Values)
            setup.gameObject.name = $"Level ({setup.definition.level})";

        foreach (int level in NewNights)
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath(level));
            if (def == null)
            {
                Debug.LogError($"[NightsBuilder] {LevelPath(level)} missing. Run step 2 first.");
                return;
            }

            if (setups.TryGetValue(level, out LevelSceneSetup existing))
            {
                if (!rebuildNewNights)
                    continue;
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject($"Level ({level})");
            root.transform.SetParent(levelObjects, false);
            var setup = root.AddComponent<LevelSceneSetup>();
            setup.definition = def;
            BuildNewNight(level, setup);
            setups[level] = setup;
        }

        // Nanny points for every night she's in, plus the hint for night 5's new Hide task.
        foreach ((int level, LevelSceneSetup setup) in setups)
        {
            NannyModule nanny = setup.definition.GetModule<NannyModule>();
            if (nanny != null && nanny.mode != NannyMode.None && NannyPlan.TryGetValue(level, out var plan)
                && (setup.nannySpawnPoint == null || setup.nannyPatrolRoute == null || rebuildNewNights && NewNights.Contains(level)))
                BuildNannyPoints(level, setup, plan.spawn, plan.route);

            if (setup.taskHints.Length != setup.definition.tasks.Length)
                Array.Resize(ref setup.taskHints, setup.definition.tasks.Length);
            EditorUtility.SetDirty(setup);
        }

        LevelSceneSetup five = setups[5];
        five.taskHints[2] = HudUnder("HideSpots/HideSpot BabyRoom");

        // Order 1..20 and keep every root off; GamePlayManager switches on the one being played.
        foreach (LevelSceneSetup setup in setups.Values.OrderBy(s => s.definition.level))
        {
            setup.transform.SetAsLastSibling();
            setup.gameObject.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[NightsBuilder] Level roots ready: {setups.Count} nights.");
    }

    static void BuildNewNight(int level, LevelSceneSetup s)
    {
        Transform root = s.transform;
        SceneObjectKey nursery = NightsSceneSetup.EnsureKey("UpperRoomDoor");
        SceneObjectKey north = NightsSceneSetup.EnsureKey("FirstFloorDoor");
        // The baby holds a toy with its own HUD marker, so name the baby's marker exactly.
        GameObject babyHud = NightsSceneSetup.Find("Baby/Baby HUD Element");
        GameObject fuseBoxHud = HudUnder("PowerProps/FuseBox Slot");

        switch (level)
        {
            case 7:
            {
                Spawn(s, "Player Cradle Position", "CullingAreaFirstFloor");
                Cradle(s);
                var flashlight = MakeFlashlight(root, Surface(new Vector3(-1.4f, 3f, -1.0f)));
                var fuse = MakeFuse(root, Surface(new Vector3(-6.7f, 0f, 10.9f)));
                s.taskHints = new[] { Hud(flashlight), Hud(fuse), fuseBoxHud };
                break;
            }
            case 8:
            {
                Spawn(s, "Start Position", "CullingAreaGround");
                Cradle(s);
                var key = MakeKey(root, Surface(new Vector3(-7.7f, 0f, 1.0f)), nursery);
                var toy = CopyItem("LevelObjects/Level (5)/Toy", root, Surface(new Vector3(4.8f, 0f, 2.7f)));
                s.taskHints = new[] { Hud(key), Hud(toy), HudUnder("Environment/House/First Floor/BabyRoomDoor"), babyHud };
                break;
            }
            case 9:
            {
                Spawn(s, "Player Cradle Position", "CullingAreaFirstFloor");
                Cradle(s);
                var feeder = CopyItem("LevelObjects/Level (2)/BabyFeeder", root, Surface(new Vector3(-6.0f, 0f, 8.55f)));
                s.taskHints = new[] { HudUnder("Environment/House/Phone/Noise HallPhone"), Hud(feeder), babyHud };
                break;
            }
            case 12:
            {
                Spawn(s, "player ground room position", "CullingAreaGround");
                Cradle(s);
                var axe = CopyItem("LevelObjects/Level (10)/VikingAxe", root, Surface(new Vector3(4.3f, 0f, 2.4f)));
                s.taskHints = new[] { Hud(axe), HudUnder("HideSpots/HideSpot LivingRoom") };
                break;
            }
            case 13:
            {
                Spawn(s, "player ground room position", "CullingAreaGround");
                Cradle(s);
                var flashlight = MakeFlashlight(root, Surface(new Vector3(-8.6f, 0f, 7.3f)));
                var first = MakeFragment(root, Surface(new Vector3(-6.7f, 0f, 10.9f)));
                MakeFragment(root, Surface(new Vector3(4.8f, 0f, 2.7f)));
                MakeFragment(root, Surface(new Vector3(-9.4f, 3f, -1.3f)));
                s.taskHints = new[] { Hud(flashlight), Hud(first) };
                break;
            }
            case 15:
            {
                Transform start = NewPlayerSpawn("Night 15 Start", new Vector3(-3f, 3.85f, 6.5f), 90f);
                s.playerSpawnPoint = start;
                s.spawnCullingArea = Culling("CullingAreaFirstFloor");
                Cradle(s);
                var flashlight = MakeFlashlight(root, Surface(new Vector3(-1.6f, 3f, 7.0f)));
                var key = MakeKey(root, Surface(new Vector3(2.8f, 3f, 6.6f)), north);
                s.taskHints = new[] { Hud(flashlight), Hud(key), HudUnder("Environment/House/First Floor/Doors1 (1)"), HudUnder("HideSpots/HideSpot NorthHall") };
                break;
            }
            case 16:
            {
                Spawn(s, "Start Position", "CullingAreaGround");
                Cradle(s);
                var key = MakeKey(root, Surface(new Vector3(-5.0f, 0f, 8.55f)), north);
                var diary = MakeDiary(root, Surface(new Vector3(-5.5f, 3f, 7.2f)));
                s.taskHints = new[] { Hud(key), HudUnder("Environment/House/Ground Floor/TV1/Noise LivingTV"), HudUnder("Environment/House/First Floor/Doors1 (1)"), Hud(diary) };
                break;
            }
            case 17:
            {
                Spawn(s, "Player unconsious position", "CullingAreaGround");
                Cradle(s);
                var flashlight = MakeFlashlight(root, Surface(new Vector3(5.6f, 0f, 7.2f)));
                var first = MakeFragment(root, Surface(new Vector3(-6f, 3f, 6.5f)));
                MakeFragment(root, Surface(new Vector3(-7f, 0f, -2f)));
                MakeFragment(root, Surface(new Vector3(-7.0f, 0f, 8.55f)));
                var fuse = MakeFuse(root, Surface(new Vector3(-6.5f, 0f, 4.8f)));
                s.taskHints = new[] { Hud(flashlight), Hud(first), Hud(fuse), fuseBoxHud };
                break;
            }
            case 18:
            {
                Spawn(s, "Player Cradle Position", "CullingAreaFirstFloor");
                s.initDropPoint = DropPointNamed("RockingChair");
                s.babySpawnPoint = NightsSceneSetup.Find("BabySpawnLevelPositions/Position (9)")?.transform;
                var feeder = CopyItem("LevelObjects/Level (2)/BabyFeeder", root, Surface(new Vector3(3.0f, 0f, 9.6f)));
                var cloth = CopyItem("LevelObjects/Level (4)/Cloth Shirt", root, Surface(new Vector3(-9.4f, 3f, -1.3f)));
                s.taskHints = new[] { Hud(feeder), babyHud, Hud(cloth), babyHud };
                break;
            }
            case 19:
            {
                Spawn(s, "Start Position", "CullingAreaGround");
                Cradle(s);
                var extinguisher = CopyItem("LevelObjects/Level (11)/fire extinguisher", root, Surface(new Vector3(-7.5f, 0f, -2.5f)));
                s.taskHints = new[] { Hud(extinguisher), HudUnder("Items/BedRoom Fire"), HudUnder("HideSpots/HideSpot NorthHall") };
                break;
            }
        }
    }

    static void Spawn(LevelSceneSetup s, string playerSpawn, string culling)
    {
        s.playerSpawnPoint = NightsSceneSetup.Find($"PlayerSpawnPositions/{playerSpawn}")?.transform;
        s.spawnCullingArea = Culling(culling);
        if (s.playerSpawnPoint == null)
            Debug.LogError($"[NightsBuilder] Player spawn '{playerSpawn}' not found.");
    }

    static void Cradle(LevelSceneSetup s)
    {
        s.initDropPoint = DropPointNamed("Cradle");
        s.babySpawnPoint = NightsSceneSetup.Find("BabySpawnLevelPositions/Position (2)")?.transform;
    }

    static Transform NewPlayerSpawn(string name, Vector3 position, float yaw)
    {
        Transform parent = NightsSceneSetup.Find("PlayerSpawnPositions").transform;
        GameObject go = NightsSceneSetup.EnsureChild(parent, name);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return go.transform;
    }

    static CullingArea Culling(string name) =>
        Object.FindObjectsByType<CullingArea>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == name);

    static DropPoint DropPointNamed(string referenceName) =>
        Object.FindFirstObjectByType<GamePlayManager>(FindObjectsInactive.Include).allDropPoints
            .FirstOrDefault(p => p != null && p.referenceName == referenceName);

    static void BuildNannyPoints(int level, LevelSceneSetup setup, Vector3 spawn, Vector3[] route)
    {
        Transform points = NightsSceneSetup.EnsureRoot("NannyPoints");
        Transform levelRoot = NightsSceneSetup.EnsureChild(points, $"Level {level}").transform;
        var agent = Object.FindFirstObjectByType<NannyStateManager>(FindObjectsInactive.Include).GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };

        Vector3 OnNav(Vector3 p) => NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, filter) ? hit.position : p;

        Transform spawnPoint = NightsSceneSetup.EnsureChild(levelRoot, "Spawn").transform;
        spawnPoint.position = OnNav(spawn);
        Vector3 toFirst = OnNav(route[0]) - spawnPoint.position;
        toFirst.y = 0f;
        spawnPoint.rotation = toFirst.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toFirst) : Quaternion.identity;

        Transform routeRoot = NightsSceneSetup.EnsureChild(levelRoot, "Route").transform;
        for (int i = routeRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(routeRoot.GetChild(i).gameObject);
        for (int i = 0; i < route.Length; i++)
        {
            GameObject waypoint = NightsSceneSetup.EnsureChild(routeRoot, $"Waypoint {i + 1}");
            waypoint.transform.position = OnNav(route[i]);
        }

        setup.nannySpawnPoint = spawnPoint;
        setup.nannyPatrolRoute = routeRoot;
    }

    #endregion

    #region Items

    /// <summary>The top surface under a point (a table, a shelf, or the floor).</summary>
    static Vector3 Surface(Vector3 point)
    {
        Vector3 from = point + Vector3.up * 1.6f;
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 2.4f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
        {
            // Skip the player and anything that moves (other items).
            if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic)
                continue;
            return hit.point;
        }

        return point;
    }

    /// <summary>Rests the item's visible mesh on the surface. Particle and HUD bounds are ignored.</summary>
    static void SitOn(GameObject item, Vector3 surface)
    {
        item.transform.position = surface;
        Renderer[] meshes = item.GetComponentsInChildren<Renderer>(true)
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer)
            .ToArray();
        if (meshes.Length == 0)
            return;

        float bottom = meshes.Min(r => r.bounds.min.y);
        item.transform.position += Vector3.up * (surface.y - bottom + 0.01f);
    }

    static GameObject CopyItem(string sourcePath, Transform root, Vector3 surface)
    {
        GameObject source = NightsSceneSetup.Find(sourcePath);
        if (source == null)
            throw new InvalidOperationException($"Item template {sourcePath} not found.");

        GameObject copy = Object.Instantiate(source, root);
        copy.name = source.name;
        copy.SetActive(true);
        SitOn(copy, surface);
        return copy;
    }

    static GameObject Glow(GameObject parent)
    {
        var feeder = NightsSceneSetup.Find("LevelObjects/Level (2)/BabyFeeder")?.GetComponent<PickableItem>();
        if (feeder == null || feeder.glowParticle == null)
            return null;

        GameObject glow = Object.Instantiate(feeder.glowParticle.gameObject, parent.transform);
        glow.name = "Glow";
        glow.transform.localPosition = Vector3.zero;
        return glow;
    }

    static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localEuler, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localEulerAngles = localEuler;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    static T Pickable<T>(GameObject go, ItemType type, TaskType onPick, string prompt, Vector3 colliderSize) where T : PickableItem
    {
        var box = go.AddComponent<BoxCollider>();
        box.size = colliderSize;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var item = go.AddComponent<T>();
        item.itemType = type;
        item.OnPickTaskType = onPick;
        item.detectionText = prompt;
        item.crosshairState = CrosshairState.Pick;
        item.collider = box;
        item.rb = rb;
        item.holdPositionOffset = new Vector3(0.25f, -0.1f, 0.6f);
        GameObject glow = Glow(go);
        if (glow != null)
            item.glowParticle = glow.GetComponent<ParticleSystem>();
        item.hUDNavigationElement = NightsSceneSetup.EnsureHud(go.transform, "Item HUD Element", Vector3.up * 0.15f)?.GetComponent<HUDNavigationElement>();
        return item;
    }

    static GameObject MakeKey(Transform root, Vector3 surface, SceneObjectKey door)
    {
        var go = new GameObject("Key");
        go.transform.SetParent(root, false);
        Material brass = NightsSceneSetup.EnsureMaterial("KeyBrass", new Color(0.78f, 0.6f, 0.22f), new Color(0.25f, 0.18f, 0.04f));
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.07f), new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.008f, 0.07f), brass);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.014f, 0.014f, 0.13f), brass);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0.018f, 0f, 0.07f), Vector3.zero, new Vector3(0.025f, 0.012f, 0.012f), brass);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0.015f, 0f, 0.045f), Vector3.zero, new Vector3(0.02f, 0.012f, 0.01f), brass);

        var key = Pickable<KeyItem>(go, ItemType.Key, TaskType.PickKey, "Pick Key", new Vector3(0.2f, 0.08f, 0.28f));
        key.opensDoor = door;
        SitOn(go, surface);
        return go;
    }

    static GameObject MakeFuse(Transform root, Vector3 surface)
    {
        var go = new GameObject("Fuse");
        go.transform.SetParent(root, false);
        Material ceramic = NightsSceneSetup.EnsureMaterial("FuseCeramic", new Color(0.9f, 0.88f, 0.82f));
        Material metal = NightsSceneSetup.EnsureMaterial("FuseMetal", new Color(0.55f, 0.55f, 0.58f), new Color(0.08f, 0.08f, 0.1f));
        Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.045f, 0.05f, 0.045f), ceramic);
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(0.055f, 0f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.05f, 0.008f, 0.05f), metal);
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(-0.055f, 0f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.05f, 0.008f, 0.05f), metal);

        Pickable<PickableItem>(go, ItemType.Fuse, TaskType.PickFuse, "Pick Fuse", new Vector3(0.22f, 0.12f, 0.12f));
        SitOn(go, surface);
        return go;
    }

    static GameObject MakeFlashlight(Transform root, Vector3 surface)
    {
        var go = new GameObject("Flashlight");
        go.transform.SetParent(root, false);
        go.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        Material body = NightsSceneSetup.EnsureMaterial("FlashlightBody", new Color(0.08f, 0.08f, 0.09f));
        Material lens = NightsSceneSetup.EnsureMaterial("FlashlightLens", new Color(1f, 0.95f, 0.7f), new Color(1.5f, 1.3f, 0.7f));
        Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.04f, 0.08f, 0.04f), body);
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.09f), new Vector3(90f, 0f, 0f), new Vector3(0.06f, 0.02f, 0.06f), body);
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.111f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.002f, 0.05f), lens);

        // A faint glow so it can be found in a dark house.
        var glowGo = new GameObject("Glow Light");
        glowGo.transform.SetParent(go.transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 0.15f, 0.1f);
        var glow = glowGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 1.6f;
        glow.intensity = 1.2f;
        glow.color = new Color(1f, 0.85f, 0.55f);
        glow.shadows = LightShadows.None;
        glow.lightmapBakeType = LightmapBakeType.Realtime;

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.4f, 0.25f, 0.45f);
        var pickup = go.AddComponent<FlashlightPickup>();
        pickup.collider = box;
        pickup.detectionText = pickup.prompt;
        pickup.crosshairState = CrosshairState.Interact;
        pickup.pickSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scene/mixkit-gear-fast-lock-tap-2857.wav");
        NightsSceneSetup.EnsureHud(go.transform, "Flashlight HUD Element", Vector3.up * 0.15f);
        SitOn(go, surface);
        return go;
    }

    static GameObject MakeFragment(Transform root, Vector3 surface)
    {
        GameObject source = NightsSceneSetup.Find("LevelObjects/Level (20)/Talisman") ?? NightsSceneSetup.Find("LevelObjects/Level (10)/Talisman");
        GameObject go = Object.Instantiate(source, root);
        go.name = "Talisman Fragment";
        go.SetActive(true);
        go.transform.localScale = source.transform.localScale * 0.6f;

        foreach (PickableItem item in go.GetComponents<PickableItem>())
            Object.DestroyImmediate(item);
        foreach (Rigidbody rb in go.GetComponents<Rigidbody>())
            Object.DestroyImmediate(rb);

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
            box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size *= 1.6f;

        var piece = go.AddComponent<Collectible>();
        piece.displayName = "Talisman Fragment";
        piece.detectionText = "Take Talisman Fragment";
        piece.crosshairState = CrosshairState.Interact;
        piece.collider = box;
        piece.glow = go.GetComponentInChildren<ParticleSystem>(true);

        if (go.GetComponentInChildren<HUDNavigationElement>(true) == null)
            NightsSceneSetup.EnsureHud(go.transform, "Fragment HUD Element", Vector3.up * 0.15f);
        SitOn(go, surface);
        return go;
    }

    static GameObject MakeDiary(Transform root, Vector3 surface)
    {
        var go = new GameObject("Her Diary");
        go.transform.SetParent(root, false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vintage House/Assets/Prefab_Props/Book_01.prefab");
        if (prefab != null)
        {
            var book = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
            foreach (Collider c in book.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
            book.transform.localPosition = Vector3.zero;
            book.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            book.isStatic = false;
            foreach (Transform t in book.GetComponentsInChildren<Transform>(true))
                t.gameObject.isStatic = false;
        }
        else
        {
            Part(go.transform, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.18f, 0.04f, 0.24f),
                NightsSceneSetup.EnsureMaterial("DiaryCover", new Color(0.35f, 0.05f, 0.05f)));
        }

        Bounds b = NightsSceneSetup.RendererBounds(go);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = b.size + Vector3.one * 0.15f;

        var piece = go.AddComponent<Collectible>();
        piece.displayName = "her diary";
        piece.detectionText = "Take her diary";
        piece.crosshairState = CrosshairState.Interact;
        piece.collider = box;
        GameObject glow = Glow(go);
        if (glow != null)
            piece.glow = glow.GetComponent<ParticleSystem>();
        NightsSceneSetup.EnsureHud(go.transform, "Diary HUD Element", Vector3.up * 0.15f);
        SitOn(go, surface);
        return go;
    }

    static GameObject Hud(GameObject item)
    {
        var hud = item != null ? item.GetComponentInChildren<HUDNavigationElement>(true) : null;
        return hud != null ? hud.gameObject : null;
    }

    static GameObject HudUnder(string path)
    {
        GameObject go = NightsSceneSetup.Find(path);
        if (go == null)
        {
            Debug.LogWarning($"[NightsBuilder] Hint target {path} not found.");
            return null;
        }

        return Hud(go);
    }

    #endregion

    #region Report

    [MenuItem("Baby's Terror/20 Nights/Report")]
    public static void LevelDefinitionReport()
    {
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        var lines = new List<string>();
        int problems = 0;
        foreach (LevelDefinition def in db.Levels)
        {
            List<string> issues = def.Validate();
            problems += issues.Count;
            lines.Add($"Night {def.level,2}: {def.missionName} — {def.tasks.Length} tasks{(issues.Count > 0 ? " | " + string.Join("; ", issues) : "")}");
        }

        string report = string.Join("\n", lines);
        if (problems > 0)
            Debug.LogWarning($"[NightsBuilder] {problems} problem(s):\n{report}");
        else
            Debug.Log($"[NightsBuilder] All {db.Count} nights valid:\n{report}");
    }

    #endregion
}
