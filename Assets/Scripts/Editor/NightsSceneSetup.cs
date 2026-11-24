using System.Collections.Generic;
using System.IO;
using System.Linq;
using ControlFreak2;
using ControlFreak2.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// One-click scene install for the 20-night update: hiding spots, noise lures, the fuse box,
/// house lights and the power-cut lightmap, the new touch buttons, the next-night teaser, keyed
/// doors and the URP light setting. Safe to run again: everything is found by name first.
/// </summary>
public static class NightsSceneSetup
{
    public const string GamePlayScene = "Assets/Scene/GamePlay.unity";
    public const string KeysFolder = "Assets/GameData/SceneKeys";
    const string LightingFolder = "Assets/GameData/Lighting";
    const string MaterialsFolder = "Assets/GameData/Materials";
    const string SoundsFolder = "Assets/GameSounds/Generated";
    const string Cf2Sprites = "Assets/Plugins/Control-Freak-2/Sprites";
    const string HudTemplatePath = "LevelObjects/Level (2)/BabyFeeder/Feeder HUD Element";

    [MenuItem("Baby's Terror/20 Nights/0. Install Scene Features")]
    public static void InstallAll()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != GamePlayScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            scene = EditorSceneManager.OpenScene(GamePlayScene);
        }

        var log = new List<string>();
        KeyDoors(log);
        HideSpots(log);
        NoiseMakers(log);
        FuseBox(log);
        Lights(log);
        TouchButtons(log);
        NextNightPanel(log);
        PlayerTuning(log);
        UrpPerPixelLights(log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[NightsSceneSetup] Done:\n- " + string.Join("\n- ", log));
    }

    #region Doors

    static void KeyDoors(List<string> log)
    {
        TagDoor("Environment/House/First Floor/Doors1 (1)", "FirstFloorDoor", log);
        TagDoor("Environment/House/Ground Floor/Wall_01_ext_doors/Doors1", "GroundFloorDoor", log);

        // Hint target for the north room door, like the baby room door has.
        GameObject north = Find("Environment/House/First Floor/Doors1 (1)");
        if (north != null)
            EnsureHud(north.transform, "Door HUD Element", Vector3.up * 0.2f);

        AudioClip unlock = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameSounds/DoorSounds/-door-lock.wav");
        foreach (DoorController door in Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door.unlockSFX == null && unlock != null)
            {
                door.unlockSFX = unlock;
                EditorUtility.SetDirty(door);
            }
        }
    }

    static void TagDoor(string path, string keyName, List<string> log)
    {
        GameObject door = Find(path);
        if (door == null)
        {
            log.Add($"MISSING door {path}");
            return;
        }

        SceneObjectKey key = EnsureKey(keyName);
        var tag = door.GetComponent<SceneObjectTag>();
        if (tag == null)
            tag = door.AddComponent<SceneObjectTag>();
        tag.key = key;
        EditorUtility.SetDirty(tag);
        log.Add($"door {door.name} tagged {keyName}");
    }

    public static SceneObjectKey EnsureKey(string keyName)
    {
        string path = $"{KeysFolder}/{keyName}.asset";
        var key = AssetDatabase.LoadAssetAtPath<SceneObjectKey>(path);
        if (key != null)
            return key;

        Directory.CreateDirectory(KeysFolder);
        key = ScriptableObject.CreateInstance<SceneObjectKey>();
        AssetDatabase.CreateAsset(key, path);
        return key;
    }

    #endregion

    #region Hiding spots

    struct HideSpec
    {
        public string name, wardrobe;
        public Vector3 view, exit;
        public float yaw;
        public bool useDoors;
        public Vector3 triggerCenter, triggerSize;
    }

    static void HideSpots(List<string> log)
    {
        Transform root = EnsureRoot("HideSpots");
        GameObject overlay = EnsureHideOverlay();
        AudioClip open = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameSounds/DoorSounds/DoorOpen.wav");
        AudioClip close = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameSounds/DoorSounds/DoorClose.wav");

        var specs = new[]
        {
            // Wardrobes that open: aiming at their doors shows Hide.
            new HideSpec { name = "HideSpot LivingRoom", wardrobe = "Environment/House/Ground Floor/Wardrobe3_A (1)", useDoors = true,
                view = new Vector3(-2.58f, 1.62f, 6.97f), exit = new Vector3(-3.6f, 0f, 6.97f), yaw = 270f },
            new HideSpec { name = "HideSpot UpstairsHall", wardrobe = "Environment/House/Commen/Wardrobe3_A (2)", useDoors = true,
                view = new Vector3(-9.54f, 4.65f, -2.92f), exit = new Vector3(-8.5f, 3.03f, -2.92f), yaw = 90f },
            // Solid wardrobes: a trigger in front of the face, and the peek overlay hides the inside.
            new HideSpec { name = "HideSpot NorthEast", wardrobe = "Environment/House/Ground Floor/Wardrobe3_A",
                view = new Vector3(-0.40f, 1.62f, 11.52f), exit = new Vector3(-0.40f, 0f, 10.5f), yaw = 180f,
                triggerCenter = new Vector3(-0.40f, 1.2f, 11.02f), triggerSize = new Vector3(1.1f, 2f, 0.3f) },
            new HideSpec { name = "HideSpot BabyRoom", wardrobe = "Environment/House/First Floor/Wardrobe4",
                view = new Vector3(-1.32f, 4.62f, -2.56f), exit = new Vector3(-0.3f, 3.0f, -2.56f), yaw = 90f,
                triggerCenter = new Vector3(-0.82f, 4.2f, -2.56f), triggerSize = new Vector3(0.3f, 2f, 1.5f) },
            new HideSpec { name = "HideSpot NorthHall", wardrobe = "Environment/House/First Floor/Wardrobe5",
                view = new Vector3(1.64f, 4.62f, 3.40f), exit = new Vector3(1.66f, 3.03f, 2.68f), yaw = 180f,
                triggerCenter = new Vector3(1.64f, 4.2f, 2.98f), triggerSize = new Vector3(1.1f, 2f, 0.3f) },
            new HideSpec { name = "HideSpot EastRoom", wardrobe = "Environment/House/Ground Floor/Wardrobe2",
                view = new Vector3(4.58f, 1.62f, 0.64f), exit = new Vector3(4.72f, 0f, 1.5f), yaw = 0f,
                triggerCenter = new Vector3(4.58f, 1.2f, 1.14f), triggerSize = new Vector3(1.0f, 2f, 0.3f) },
        };

        foreach (HideSpec s in specs)
        {
            GameObject wardrobe = Find(s.wardrobe);
            if (wardrobe == null)
            {
                log.Add($"MISSING wardrobe {s.wardrobe}");
                continue;
            }

            GameObject go = EnsureChild(root, s.name);
            go.transform.position = wardrobe.transform.position + Vector3.up * 1.2f;
            go.transform.rotation = Quaternion.Euler(0f, s.yaw, 0f);

            var spot = GetOrAdd<HideSpot>(go);
            Transform view = EnsureChild(go.transform, "View").transform;
            view.SetPositionAndRotation(s.view, Quaternion.Euler(4f, s.yaw, 0f));
            Transform exit = EnsureChild(go.transform, "Exit").transform;
            exit.SetPositionAndRotation(OnNavMesh(s.exit), Quaternion.Euler(0f, s.yaw, 0f));

            spot.viewPoint = view;
            spot.exitPoint = exit;
            spot.allowLook = false;
            spot.hiddenOverlay = overlay;
            spot.enterSFX = open;
            spot.exitSFX = open;
            spot.crosshairState = CrosshairState.Hide;
            spot.detectionText = "Hide";

            if (s.useDoors)
            {
                spot.doors = wardrobe.GetComponentsInChildren<DoorController>(true);
                var oldCollider = go.GetComponent<BoxCollider>();
                if (oldCollider != null)
                    Object.DestroyImmediate(oldCollider);
                spot.collider = null;
            }
            else
            {
                spot.doors = new DoorController[0];
                var box = GetOrAdd<BoxCollider>(go);
                box.isTrigger = true;
                box.center = go.transform.InverseTransformPoint(s.triggerCenter);
                box.size = Quaternion.Inverse(go.transform.rotation) * s.triggerSize;
                box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
                spot.collider = box;
                go.layer = LayerMask.NameToLayer("Interactable");
            }

            EnsureHud(go.transform, "Hide HUD Element", Vector3.zero);
            EditorUtility.SetDirty(spot);
            log.Add($"hide spot {s.name}");
        }
        _ = close;
    }

    /// <summary>Two dark bars with a slit between: peeking out through the wardrobe doors.</summary>
    static GameObject EnsureHideOverlay()
    {
        Canvas canvas = FindCanvas("CF2-Canvas");
        GameObject overlay = EnsureChild(canvas.transform, "HideOverlay");
        overlay.transform.SetAsFirstSibling();
        var rt = GetOrAdd<RectTransform>(overlay);
        Stretch(rt);

        Bar(overlay.transform, "Top", new Vector2(0f, 0.58f), new Vector2(1f, 1f));
        Bar(overlay.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0.36f));
        Bar(overlay.transform, "Left", new Vector2(0f, 0f), new Vector2(0.06f, 1f));
        Bar(overlay.transform, "Right", new Vector2(0.94f, 0f), new Vector2(1f, 1f));
        overlay.SetActive(false);
        return overlay;
    }

    static void Bar(Transform parent, string name, Vector2 min, Vector2 max)
    {
        GameObject bar = EnsureChild(parent, name);
        var rt = GetOrAdd<RectTransform>(bar);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = GetOrAdd<Image>(bar);
        img.color = new Color(0.02f, 0.015f, 0.01f, 0.97f);
        img.raycastTarget = false;
    }

    #endregion

    #region Noise lures

    static void NoiseMakers(List<string> log)
    {
        AudioClip ring = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/phone-ring.wav");
        AudioClip tv = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/tv-static.wav");

        MakeNoise("Environment/House/Phone", "Noise HallPhone", "Ring the old phone", ring, false, new Vector3(4.9f, 0f, 4.9f), log);
        MakeNoise("Environment/House/KitchenObjects/Phone (1)", "Noise KitchenPhone", "Ring the old phone", ring, false, new Vector3(-2.6f, 0f, 9.8f), log);
        MakeNoise("Environment/House/Phone (3)", "Noise UpstairsPhone", "Ring the old phone", ring, false, new Vector3(-1.8f, 3f, 2.4f), log);
        MakeNoise("Environment/House/Ground Floor/TV1", "Noise LivingTV", "Turn on the TV", tv, true, new Vector3(-3.6f, 0f, 5.86f), log);
        MakeNoise("Environment/House/First Floor/TV1 (2)", "Noise UpstairsTV", "Turn on the TV", tv, true, new Vector3(3.0f, 3f, 2.4f), log);
    }

    static void MakeNoise(string propPath, string name, string prompt, AudioClip clip, bool electric, Vector3 lookPoint, List<string> log)
    {
        GameObject prop = Find(propPath);
        if (prop == null)
        {
            log.Add($"MISSING noise prop {propPath}");
            return;
        }

        Bounds b = RendererBounds(prop);
        GameObject go = EnsureChild(prop.transform, name);
        go.transform.position = b.center;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = LayerMask.NameToLayer("Interactable");

        var box = GetOrAdd<BoxCollider>(go);
        box.isTrigger = true;
        box.center = Vector3.zero;
        Vector3 size = b.size + Vector3.one * 0.15f;
        Vector3 scale = prop.transform.lossyScale;
        box.size = new Vector3(size.x / Mathf.Max(0.0001f, scale.x), size.y / Mathf.Max(0.0001f, scale.y), size.z / Mathf.Max(0.0001f, scale.z));

        var noise = GetOrAdd<NoiseMaker>(go);
        noise.collider = box;
        noise.prompt = prompt;
        noise.detectionText = prompt;
        noise.crosshairState = CrosshairState.Interact;
        noise.noiseClip = clip;
        noise.requiresPower = electric;
        noise.lingerSeconds = 8f;
        noise.cooldown = 25f;

        Transform look = EnsureChild(go.transform, "Investigate Point").transform;
        look.position = OnNavMesh(lookPoint);
        noise.investigatePoint = look;

        var source = GetOrAdd<AudioSource>(go);
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 30f;
        noise.source = source;

        EnsureHud(go.transform, "Noise HUD Element", Vector3.up * 0.2f);
        EditorUtility.SetDirty(noise);
        log.Add($"noise {name}");
    }

    #endregion

    #region Fuse box

    static void FuseBox(List<string> log)
    {
        Transform root = EnsureRoot("PowerProps");
        GameObject old = Find("PowerProps/FuseBox");
        if (old != null)
            Object.DestroyImmediate(old);

        // A small wall-mounted electrical box (0.35 x 0.49 m), pivot on its back face, front along +Z.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vintage House/Assets/Prefab_Props/Electr_box4.prefab");
        GameObject box = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, root) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "FuseBox";
        box.transform.SetParent(root, true);
        box.isStatic = false;

        // On the ground-floor bathroom's west wall, by the door, at chest height.
        Vector3 origin = new Vector3(0.5f, 1.2f, -1.2f);
        if (Physics.Raycast(origin, Vector3.left, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            box.transform.rotation = Quaternion.LookRotation(hit.normal);
            box.transform.position = hit.point + hit.normal * 0.005f;
        }
        else
        {
            log.Add("fuse box: no wall found in the bathroom; placed at the fallback point");
            box.transform.position = new Vector3(-1.79f, 1.45f, -1.2f);
            box.transform.rotation = Quaternion.LookRotation(Vector3.right);
        }

        Bounds bounds = RendererBounds(box);
        Vector3 front = box.transform.forward;

        GameObject slot = EnsureChild(root, "FuseBox Slot");
        slot.transform.position = bounds.center + front * (Vector3.Dot(bounds.extents, Abs(front)) + 0.05f);
        slot.transform.rotation = box.transform.rotation;
        slot.layer = LayerMask.NameToLayer("Interactable");

        var trigger = GetOrAdd<BoxCollider>(slot);
        trigger.isTrigger = true;
        trigger.center = Vector3.zero;
        trigger.size = new Vector3(0.6f, 0.7f, 0.3f);

        var point = GetOrAdd<DropPoint>(slot);
        point.referenceName = "FuseBox";
        point.acceptableItemType = ItemType.Fuse;
        point.onDropTaskType = TaskType.RestorePower;
        point.crosshairState = CrosshairState.Drop;
        point.collider = trigger;

        var fuseBox = GetOrAdd<global::FuseBox>(slot);
        fuseBox.insertSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Epic Toon FX/Sound/etfx_impact_metal01.wav");

        GameObject lamp = EnsureChild(slot.transform, "Power Indicator");
        if (lamp.GetComponent<MeshFilter>() == null)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.AddComponent<MeshFilter>().sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            lamp.AddComponent<MeshRenderer>();
            Object.DestroyImmediate(sphere);
        }
        lamp.transform.localPosition = new Vector3(0.12f, 0.2f, -0.06f);
        lamp.transform.localScale = Vector3.one * 0.035f;
        lamp.GetComponent<MeshRenderer>().sharedMaterial = EnsureMaterial("PowerIndicatorOn", new Color(0.1f, 1f, 0.2f), new Color(0.2f, 2.5f, 0.4f));
        fuseBox.poweredIndicator = lamp;

        EnsureHud(slot.transform, "FuseBox HUD Element", Vector3.zero);
        EnsureKeyTag(slot, "FuseBox");
        EditorUtility.SetDirty(point);
        EditorUtility.SetDirty(fuseBox);
        log.Add($"fuse box at {box.transform.position}");
    }

    #endregion

    #region Lights

    [MenuItem("Baby's Terror/Lighting/Create Power-Cut Lightmap")]
    public static Texture2D CreatePowerCutLightmap()
    {
        Directory.CreateDirectory(LightingFolder);
        string path = $"{LightingFolder}/PowerCutLightmap.exr";
        if (!File.Exists(path))
        {
            // A tiny, almost-black blue: moonlit darkness. 4x4 so every block-compressed format accepts it.
            var tex = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, true);
            var pixels = Enumerable.Repeat(new Color(0.012f, 0.014f, 0.024f, 1f), 16).ToArray();
            tex.SetPixels(pixels);
            File.WriteAllBytes(path, tex.EncodeToEXR(Texture2D.EXRFlags.None));
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Lightmap)
        {
            importer.textureType = TextureImporterType.Lightmap;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Lights(List<string> log)
    {
        GameObject house = Find("Environment/House");
        var lights = GetOrAdd<HouseLights>(house);
        lights.darkLightmap = CreatePowerCutLightmap();
        lights.powerOffSFX = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/power-down.wav");
        lights.powerOnSFX = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/power-up.wav");
        lights.flickerSFX = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/light-flicker.wav");
        lights.moonLight = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(l => l.type == LightType.Directional);

        // Glowing bulbs and shades go dark with the power.
        lights.emissiveRenderers = house.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.sharedMaterials.Any(m => m != null && m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor")
                                                    && m.GetColor("_EmissionColor").maxColorComponent > 0.05f))
            .ToArray();
        EditorUtility.SetDirty(lights);
        log.Add($"house lights: {lights.emissiveRenderers.Length} glowing renderers, moon={(lights.moonLight ? lights.moonLight.name : "none")}");
    }

    static void UrpPerPixelLights(List<string> log)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
            SerializedProperty mode = so.FindProperty("m_AdditionalLightsRenderingMode");
            if (mode == null || mode.intValue == 1)
                continue;
            mode.intValue = 1; // Per Pixel, so the flashlight cone renders. Every other light is baked.
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"URP additional lights -> Per Pixel ({path})");
        }
    }

    #endregion

    #region UI

    static void TouchButtons(List<string> log)
    {
        var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        Transform template = ui.useDevice.transform.parent;

        ui.interact = EnsureTouchButton(template, "InteractCF2", KeyCode.E, new Vector2(-376f, 370f), "CF-Button-Use");
        ui.crouch = EnsureTouchButton(template, "CrouchCF2", KeyCode.C, new Vector2(-613f, 184f), "CF-Button-Crouch");
        // Grid right of the joystick: columns at x -139/-376/-613, rows at y 184/370/556. The top of the
        // rightmost column is left free for the Pause and Hint buttons.
        ui.flashlight = EnsureTouchButton(template, "FlashlightCF2", KeyCode.L, new Vector2(-376f, 556f), "CF-Button-Flashlight");
        ui.sprint = EnsureTouchButton(template, "SprintCF2", KeyCode.LeftShift, new Vector2(-613f, 370f), "CF-Button-Sprint");

        ui.interactSprite = Sprite("CF-Button-Use");
        ui.exitHideSprite = Sprite("CF-Button-Action");
        ui.crouchSprite = Sprite("CF-Button-Crouch");
        ui.standSprite = Sprite("CF-Button-Up");
        ui.flashlightOnSprite = Sprite("CF-Button-Flashlight");
        ui.flashlightOffSprite = Sprite("CF-Button-Flashlight");
        EditorUtility.SetDirty(ui);
        log.Add("touch buttons: Interact (E), Crouch (C), Flashlight (L), Sprint (LeftShift)");
    }

    static TouchButtonSpriteAnimator EnsureTouchButton(Transform template, string name, KeyCode key, Vector2 position, string spriteName)
    {
        Transform parent = template.parent;
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = Object.Instantiate(template.gameObject, parent);
            go.name = name;
        }

        foreach (Transform child in go.transform)
        {
            if (child.name.EndsWith("-Sprite"))
                child.name = $"{name}-Sprite";
        }

        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;

        var button = go.GetComponent<TouchButton>();
        var so = new SerializedObject(button);
        SerializedProperty keys = so.FindProperty("pressBinding.keyList");
        so.FindProperty("pressBinding.enabled").boolValue = true;
        keys.arraySize = 1;
        keys.GetArrayElementAtIndex(0).intValue = (int)key;
        so.ApplyModifiedPropertiesWithoutUndo();

        var animator = go.GetComponentInChildren<TouchButtonSpriteAnimator>(true);
        Sprite sprite = Sprite(spriteName);
        if (animator != null && sprite != null)
        {
            animator.SetSprite(sprite);
            EditorUtility.SetDirty(animator);
        }

        return animator;
    }

    static void NextNightPanel(List<string> log)
    {
        var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        Transform safe = ui.levelCompletePanel.transform.Find("BGSafe");
        var titleSource = safe.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        TMP_FontAsset horror = titleSource != null ? titleSource.font : TMP_Settings.defaultFontAsset;
        TMP_FontAsset plain = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : horror;

        GameObject group = EnsureChild(safe, "NextNight");
        var rt = GetOrAdd<RectTransform>(group);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        // Under the logo and above the bottom of the screen (BGSafe sits 70 px below the canvas).
        rt.anchoredPosition = new Vector2(-137.5f, -292f);
        rt.sizeDelta = new Vector2(800f, 250f);
        var bg = GetOrAdd<Image>(group);
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        var label = Text(group.transform, "Label", horror, 30f, new Color(0.85f, 0.12f, 0.12f), TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -14f), new Vector2(-24f, -54f));
        label.text = "NEXT NIGHT";

        // The thumbnails are portrait (the level select button's shape), so the landscape slot masks
        // one that covers it rather than letterboxing it.
        GameObject thumbGo = EnsureChild(group.transform, "Thumbnail");
        var thumbRt = GetOrAdd<RectTransform>(thumbGo);
        thumbRt.anchorMin = thumbRt.anchorMax = new Vector2(0f, 0.5f);
        thumbRt.pivot = new Vector2(0f, 0.5f);
        thumbRt.anchoredPosition = new Vector2(24f, -18f);
        thumbRt.sizeDelta = new Vector2(250f, 150f);
        if (thumbGo.TryGetComponent(out Image legacyThumb))
            Object.DestroyImmediate(legacyThumb);
        GetOrAdd<RectMask2D>(thumbGo);

        GameObject pictureGo = EnsureChild(thumbGo.transform, "Picture");
        var pictureRt = GetOrAdd<RectTransform>(pictureGo);
        pictureRt.anchorMin = pictureRt.anchorMax = pictureRt.pivot = new Vector2(0.5f, 0.5f);
        pictureRt.anchoredPosition = Vector2.zero;
        var thumb = GetOrAdd<Image>(pictureGo);
        thumb.preserveAspect = false;
        thumb.raycastTarget = false;
        var fitter = GetOrAdd<AspectRatioFitter>(pictureGo);
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = LevelThumbnailCapture.Aspect;

        var title = Text(group.transform, "Title", horror, 44f, Color.white, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(294f, -50f), new Vector2(-24f, -112f));
        title.enableAutoSizing = true;
        title.fontSizeMin = 26f;
        title.fontSizeMax = 44f;

        var teaser = Text(group.transform, "Teaser", plain, 28f, new Color(0.88f, 0.88f, 0.88f), TextAlignmentOptions.TopLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(294f, 14f), new Vector2(-24f, -110f));
        teaser.fontStyle = FontStyles.Italic;
        teaser.enableAutoSizing = true;
        teaser.fontSizeMin = 18f;
        teaser.fontSizeMax = 28f;

        ui.nextNightGroup = group;
        ui.nextNightThumbnail = thumb;
        ui.nextNightTitle = title;
        ui.nextNightTeaser = teaser;
        EditorUtility.SetDirty(ui);
        log.Add("next-night teaser panel on the win screen");
    }

    static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = EnsureChild(parent, name);
        var rt = GetOrAdd<RectTransform>(go);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0f, 1f);
        // offsetMin is the bottom-left corner and offsetMax the top-right, relative to the anchors.
        bool topBand = Mathf.Approximately(anchorMin.y, 1f);
        rt.offsetMin = topBand ? new Vector2(offsetMin.x, offsetMax.y) : offsetMin;
        rt.offsetMax = topBand ? new Vector2(offsetMax.x, offsetMin.y) : offsetMax;

        var text = GetOrAdd<TextMeshProUGUI>(go);
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    #endregion

    #region Player

    /// <summary>
    /// Mobile had no way to sprint. A Sprint button now exists, so sprint becomes a short burst
    /// with stamina: enough to break away from a chase, not enough to make her harmless.
    /// </summary>
    static void PlayerTuning(List<string> log)
    {
        var fpc = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        fpc.enableSprint = true;
        fpc.unlimitedSprint = false;
        fpc.sprintSpeed = 6f;
        fpc.sprintDuration = 4f;
        fpc.sprintCooldown = 1.5f;
        fpc.enableCrouch = false;
        EditorUtility.SetDirty(fpc);
        PrefabUtility.RecordPrefabInstancePropertyModifications(fpc);
        log.Add("sprint: 6 m/s for 4 s, 1.5 s cooldown; FPC crouch stays off (PlayerCrouch replaces it)");
    }

    #endregion

    #region Helpers

    public static GameObject Find(string path)
    {
        GameObject direct = GameObject.Find(path);
        if (direct != null)
            return direct;

        // GameObject.Find skips inactive objects.
        string[] parts = path.Split('/');
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != parts[0])
                continue;

            Transform t = root.transform;
            for (int i = 1; i < parts.Length && t != null; i++)
                t = t.Find(parts[i]);
            if (t != null)
                return t.gameObject;
        }

        return null;
    }

    public static Transform EnsureRoot(string name)
    {
        GameObject root = Find(name);
        if (root == null)
            root = new GameObject(name);
        return root.transform;
    }

    public static GameObject EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (parent is RectTransform)
            go.AddComponent<RectTransform>();
        go.layer = parent.gameObject.layer;
        return go;
    }

    /// <summary>A hint marker for HintManager, copied from an existing one so it shares the HUD setup.</summary>
    public static GameObject EnsureHud(Transform parent, string name, Vector3 localOffset)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject template = Find(HudTemplatePath);
        if (template == null)
        {
            Debug.LogWarning($"[NightsSceneSetup] HUD template {HudTemplatePath} not found.");
            return null;
        }

        GameObject hud = Object.Instantiate(template, parent);
        hud.name = name;
        hud.transform.localPosition = localOffset;
        hud.transform.localRotation = Quaternion.identity;
        hud.SetActive(false);
        return hud;
    }

    public static void EnsureKeyTag(GameObject go, string keyName)
    {
        var tag = GetOrAdd<SceneObjectTag>(go);
        tag.key = EnsureKey(keyName);
        EditorUtility.SetDirty(tag);
    }

    /// <summary>The nearest point on the Nanny's NavMesh: open floor both she and the player can stand on.</summary>
    public static Vector3 OnNavMesh(Vector3 point)
    {
        var nanny = Object.FindFirstObjectByType<NannyStateManager>(FindObjectsInactive.Include);
        var agent = nanny != null ? nanny.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
        var filter = new UnityEngine.AI.NavMeshQueryFilter
        {
            agentTypeID = agent != null ? agent.agentTypeID : 0,
            areaMask = UnityEngine.AI.NavMesh.AllAreas,
        };
        return UnityEngine.AI.NavMesh.SamplePosition(point, out UnityEngine.AI.NavMeshHit hit, 1f, filter) ? hit.position : SnapToFloor(point);
    }

    public static Vector3 SnapToFloor(Vector3 point)
    {
        if (Physics.Raycast(point + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;
        return point;
    }

    public static Bounds RendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.2f);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    public static Material EnsureMaterial(string name, Color color, Color? emission = null)
    {
        Directory.CreateDirectory(MaterialsFolder);
        string path = $"{MaterialsFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Canvas FindCanvas(string name) =>
        Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == name);

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Cf2Sprites}/{name}.png");

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    #endregion
}
