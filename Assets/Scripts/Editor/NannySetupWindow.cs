#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ommy.Audio;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public class NannySetupWindow : EditorWindow
{
    const string ClipFolder = "Assets/Models/Scary Zombie Pack";
    const string ControllerPath = ClipFolder + "/NannyAnimator.controller";
    const string ModelPath = ClipFolder + "/tripo_convert_c81a3380-c4c1-4fe3-928d-3dca58f6d3f6.fbx";
    const string PrefabPath = "Assets/GamePrefab/Nanny.prefab";

    const string SpeedParam = "Speed";
    const string CrawlParam = "IsCrawling";
    const string AttackParam = "Attack";
    const string AttackVariantParam = "AttackVariant";
    const string ScreamParam = "Scream";
    const string DieParam = "Die";

    const float WalkThreshold = 1.5f;
    const float RunThreshold = 4f;
    const float CrawlRunThreshold = 2.5f;

    /// <summary>
    /// Every clip is a Mixamo take literally named "mixamo.com", so the importer has to
    /// rename them before the controller can reference anything by name.
    /// </summary>
    class ClipDef
    {
        public readonly string fileName;
        public readonly string clipName;
        public readonly bool loop;

        public ClipDef(string fileName, string clipName, bool loop)
        {
            this.fileName = fileName;
            this.clipName = clipName;
            this.loop = loop;
        }
    }

    static readonly ClipDef[] ClipDefs =
    {
        new ClipDef("zombie idle", "Nanny_Idle", true),
        new ClipDef("zombie walk", "Nanny_Walk", true),
        new ClipDef("zombie run", "Nanny_Run", true),
        new ClipDef("zombie crawl", "Nanny_Crawl", true),
        new ClipDef("running crawl", "Nanny_CrawlRun", true),
        new ClipDef("zombie scream", "Nanny_Scream", false),
        new ClipDef("zombie attack", "Nanny_Attack", false),
        new ClipDef("zombie biting", "Nanny_Bite", false),
        new ClipDef("zombie biting (2)", "Nanny_Bite2", false),
        new ClipDef("zombie neck bite", "Nanny_NeckBite", false),
        new ClipDef("zombie dying", "Nanny_Dying", false),
        new ClipDef("zombie death", "Nanny_Death", false)
    };

    [SerializeField] bool copyAvatarFromModel;

    Vector2 _scroll;
    string _status = "Run step 1 to import the clips and build the animator.";
    MessageType _statusType = MessageType.Info;

    [MenuItem("Ommy/Nanny Setup")]
    [MenuItem("Tools/Nanny Setup")]
    public static void Open()
    {
        var window = GetWindow<NannySetupWindow>("Nanny Setup");
        window.minSize = new Vector2(420, 420);
        window.Show();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Nanny Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Imports the 12 Mixamo clips from \"Scary Zombie Pack\" with usable names and loop flags, " +
            "then rebuilds NannyAnimator.controller in place so the prefab keeps its Animator reference.",
            MessageType.None);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("1. Clips & Animator", EditorStyles.boldLabel);

        copyAvatarFromModel = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Copy avatar from Nanny model",
                "Off: every clip keeps its own generated avatar and retargets through the humanoid rig. " +
                "Turn this on only if the Nanny looks offset or broken while animating."),
            copyAvatarFromModel);

        if (GUILayout.Button("Import Clips & Build Animator", GUILayout.Height(28)))
            RunClipsAndAnimator();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Resizes the NavMeshAgent to match the baked FootStepsAgent surface, fits a capsule collider " +
            "to the model, and adds the audio sources plus the animation and audio controllers.",
            MessageType.None);

        if (GUILayout.Button("Configure Nanny Prefab", GUILayout.Height(28)))
            RunPrefabConfigure();

        EditorGUILayout.Space(10);
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, _statusType);

        EditorGUILayout.EndScrollView();
    }

    void RunClipsAndAnimator()
    {
        var log = new List<string>();
        var clips = ImportClips(log);
        string animatorResult = BuildAnimator(clips, log);

        bool failed = log.Count > 0;
        var report = new StringBuilder();
        report.AppendLine($"Imported {clips.Count}/{ClipDefs.Length} clips. {animatorResult}");
        foreach (string line in log)
            report.AppendLine("- " + line);

        _status = report.ToString().TrimEnd();
        _statusType = failed ? MessageType.Warning : MessageType.Info;
    }

    #region Prefab

    void RunPrefabConfigure()
    {
        var changes = new List<string>();
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        if (root == null)
        {
            _status = $"Could not load {PrefabPath}.";
            _statusType = MessageType.Error;
            return;
        }

        try
        {
            ConfigureAgent(root, changes);
            ConfigureCollider(root, changes);
            NannyAudioController audioController = ConfigureAudio(root, changes);
            NannyAnimationController animationController = ConfigureAnimation(root, changes);
            ConfigureStateManager(root, animationController, audioController, changes);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var report = new StringBuilder();
        report.AppendLine("Prefab configured:");
        foreach (string change in changes)
            report.AppendLine("- " + change);

        _status = report.ToString().TrimEnd();
        _statusType = MessageType.Info;
    }

    static void ConfigureAgent(GameObject root, List<string> changes)
    {
        var agent = GetOrAdd<NavMeshAgent>(root, changes, "NavMeshAgent");

        // The baked surface in GamePlay.unity uses the FootStepsAgent type (radius 0.2,
        // height 1.5). A wider agent cannot path through the house doorways on that mesh.
        agent.radius = 0.2f;
        agent.height = 1.5f;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 1.2f;
        agent.autoBraking = true;
        changes.Add("NavMeshAgent sized to radius 0.2 / height 1.5.");

        var body = GetOrAdd<Rigidbody>(root, changes, "Rigidbody");
        // The agent drives the transform, so physics must not fight it.
        body.isKinematic = true;
        body.useGravity = false;
    }

    static void ConfigureCollider(GameObject root, List<string> changes)
    {
        if (root.GetComponent<Collider>() != null)
            return;

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.direction = 1;

        if (TryGetLocalBounds(root, out Bounds bounds))
        {
            float height = bounds.size.y;
            // Depth rather than width, so a T-posed arm span doesn't inflate the radius.
            float radius = Mathf.Clamp(
                Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f, height * 0.08f, height * 0.25f);

            capsule.height = height;
            capsule.radius = radius;
            capsule.center = new Vector3(0f, bounds.center.y, 0f);
            changes.Add($"CapsuleCollider fitted to the model (height {height:0.00}, radius {radius:0.00}).");
        }
        else
        {
            changes.Add("CapsuleCollider added, but no renderer was found to measure - set its size by hand.");
        }
    }

    static NannyAudioController ConfigureAudio(GameObject root, List<string> changes)
    {
        MyAudioSource voice = GetOrAddAudioChild(root, "VoiceAudio", AudioCategory.Voice, changes);
        MyAudioSource footsteps = GetOrAddAudioChild(root, "FootstepAudio", AudioCategory.SFX, changes);

        var controller = GetOrAdd<NannyAudioController>(root, changes, "NannyAudioController");

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("voiceSource").objectReferenceValue = voice;
        serialized.FindProperty("footStepSource").objectReferenceValue = footsteps;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }

    static MyAudioSource GetOrAddAudioChild(GameObject root, string name, AudioCategory category,
        List<string> changes)
    {
        Transform existing = root.transform.Find(name);
        if (existing != null)
        {
            var found = existing.GetComponent<MyAudioSource>();
            if (found != null)
                return found;

            // The child exists but lost its component, so rebuild it in place.
            if (existing.GetComponent<AudioSource>() == null)
                existing.gameObject.AddComponent<AudioSource>();

            found = existing.gameObject.AddComponent<MyAudioSource>();
            found.category = category;
            changes.Add($"Restored the MyAudioSource on \"{name}\".");
            return found;
        }

        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);

        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 20f;

        var myAudioSource = child.AddComponent<MyAudioSource>();
        myAudioSource.category = category;

        changes.Add($"Added spatial \"{name}\" source ({category}).");
        return myAudioSource;
    }

    static NannyAnimationController ConfigureAnimation(GameObject root, List<string> changes)
    {
        var controller = GetOrAdd<NannyAnimationController>(root, changes, "NannyAnimationController");

        var animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("nannyAnimator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            changes.Add("No Animator on the prefab root - assign one before playing.");
        }

        return controller;
    }

    static void ConfigureStateManager(GameObject root, NannyAnimationController animationController,
        NannyAudioController audioController, List<string> changes)
    {
        var stateManager = GetOrAdd<NannyStateManager>(root, changes, "NannyStateManager");

        var serialized = new SerializedObject(stateManager);
        serialized.FindProperty("agent").objectReferenceValue = root.GetComponent<NavMeshAgent>();
        serialized.FindProperty("animationController").objectReferenceValue = animationController;
        serialized.FindProperty("audioController").objectReferenceValue = audioController;
        serialized.FindProperty("sightBlockers").intValue = SightBlockerMask();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        changes.Add("NannyStateManager references wired.");
    }

    /// <summary>Everything except the layers that should never count as cover.</summary>
    static int SightBlockerMask()
    {
        string[] transparentLayers = { "Player", "HeldItem", "UI", "TransparentFX", "Ignore Raycast" };

        int mask = ~0;
        foreach (string layerName in transparentLayers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask &= ~(1 << layer);
        }

        return mask;
    }

    static bool TryGetLocalBounds(GameObject root, out Bounds localBounds)
    {
        localBounds = default;
        bool found = false;
        Bounds worldBounds = default;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            if (!found)
            {
                worldBounds = renderer.bounds;
                found = true;
            }
            else
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
            return false;

        // Collider sizes are local, but renderer bounds are world, so undo the prefab's scale.
        Vector3 scale = root.transform.lossyScale;
        localBounds = new Bounds(
            root.transform.InverseTransformPoint(worldBounds.center),
            new Vector3(
                worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z))));

        return true;
    }

    static T GetOrAdd<T>(GameObject root, List<string> changes, string label) where T : Component
    {
        var component = root.GetComponent<T>();
        if (component != null)
            return component;

        changes.Add($"Added {label}.");
        return root.AddComponent<T>();
    }

    #endregion

    #region Clip import

    Dictionary<string, AnimationClip> ImportClips(List<string> log)
    {
        Avatar sourceAvatar = null;
        if (copyAvatarFromModel)
        {
            sourceAvatar = AssetDatabase
                .LoadAllAssetRepresentationsAtPath(ModelPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            if (sourceAvatar == null)
                log.Add($"No avatar found on {ModelPath}; clips kept their own avatars.");
        }

        var clips = new Dictionary<string, AnimationClip>();

        try
        {
            for (int i = 0; i < ClipDefs.Length; i++)
            {
                ClipDef def = ClipDefs[i];
                EditorUtility.DisplayProgressBar(
                    "Nanny Setup", $"Importing {def.clipName}", (float)i / ClipDefs.Length);

                ApplyImportSettings(def, sourceAvatar, log);

                AnimationClip clip = FindClip($"{ClipFolder}/{def.fileName}.fbx", def.clipName);
                if (clip == null)
                    log.Add($"Could not find clip \"{def.clipName}\" after reimporting {def.fileName}.fbx.");
                else
                    clips[def.clipName] = clip;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return clips;
    }

    static void ApplyImportSettings(ClipDef def, Avatar sourceAvatar, List<string> log)
    {
        string path = $"{ClipFolder}/{def.fileName}.fbx";
        if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
        {
            log.Add($"Missing FBX: {path}");
            return;
        }

        importer.importAnimation = true;

        if (sourceAvatar != null)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.sourceAvatar = sourceAvatar;
        }

        // defaultClipAnimations always describes the raw take, so this stays correct on
        // re-runs and carries the real firstFrame/lastFrame that we must not guess at.
        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0)
        {
            log.Add($"{def.fileName}.fbx contains no animation take.");
            return;
        }

        ModelImporterClipAnimation clip = defaults[0];
        clip.name = def.clipName;
        clip.loopTime = def.loop;
        clip.keepOriginalPositionY = true;

        importer.clipAnimations = new[] { clip };
        importer.SaveAndReimport();
    }

    static AnimationClip FindClip(string assetPath, string clipName)
    {
        return AssetDatabase
            .LoadAllAssetRepresentationsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == clipName);
    }

    #endregion

    #region Animator

    static string BuildAnimator(Dictionary<string, AnimationClip> clips, List<string> log)
    {
        // Loading in place matters: the Nanny prefab references this controller by GUID,
        // so deleting and recreating the asset would silently unhook the Animator.
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            log.Add($"{ControllerPath} was missing and had to be recreated - reassign it on the Nanny prefab.");
        }

        RebuildParameters(controller);
        AnimatorStateMachine root = PrepareRootStateMachine(controller);

        AnimatorState locomotion = AddBlendTreeState(controller, root, "Locomotion", new Vector3(320, 0),
            (Clip(clips, "Nanny_Idle"), 0f),
            (Clip(clips, "Nanny_Walk"), WalkThreshold),
            (Clip(clips, "Nanny_Run"), RunThreshold));

        AnimatorState crawl = AddBlendTreeState(controller, root, "CrawlLocomotion", new Vector3(320, 130),
            (Clip(clips, "Nanny_Crawl"), 0f),
            (Clip(clips, "Nanny_CrawlRun"), CrawlRunThreshold));

        AnimatorState scream = AddState(root, "Scream", Clip(clips, "Nanny_Scream"), new Vector3(660, -170));
        AnimatorState attack = AddState(root, "Attack", Clip(clips, "Nanny_Attack"), new Vector3(660, -70));
        AnimatorState bite = AddState(root, "Bite", Clip(clips, "Nanny_Bite"), new Vector3(660, 30));
        AnimatorState bite2 = AddState(root, "Bite2", Clip(clips, "Nanny_Bite2"), new Vector3(660, 130));
        AnimatorState neckBite = AddState(root, "NeckBite", Clip(clips, "Nanny_NeckBite"), new Vector3(660, 230));
        AnimatorState dying = AddState(root, "Dying", Clip(clips, "Nanny_Dying"), new Vector3(660, 360));
        AnimatorState death = AddState(root, "Death", Clip(clips, "Nanny_Death"), new Vector3(940, 360));

        root.defaultState = locomotion;

        // Ground <-> crawl swap.
        AddTransition(locomotion, crawl, -1f, 0.25f).AddCondition(AnimatorConditionMode.If, 0, CrawlParam);
        AddTransition(crawl, locomotion, -1f, 0.25f).AddCondition(AnimatorConditionMode.IfNot, 0, CrawlParam);

        // Death is registered first so it always wins over a queued attack or scream.
        AddAnyStateTransition(root, dying, 0.1f).AddCondition(AnimatorConditionMode.If, 0, DieParam);
        AddTransition(dying, death, 0.9f, 0.2f);

        AddAnyStateTransition(root, scream, 0.15f).AddCondition(AnimatorConditionMode.If, 0, ScreamParam);

        AnimatorState[] attackStates = { attack, bite, bite2, neckBite };
        for (int variant = 0; variant < attackStates.Length; variant++)
        {
            AnimatorStateTransition transition = AddAnyStateTransition(root, attackStates[variant], 0.1f);
            transition.AddCondition(AnimatorConditionMode.If, 0, AttackParam);
            transition.AddCondition(AnimatorConditionMode.Equals, variant, AttackVariantParam);
        }

        // Every action returns to whichever locomotion mode the Nanny is currently in.
        foreach (AnimatorState state in new[] { scream, attack, bite, bite2, neckBite })
        {
            AddTransition(state, locomotion, 0.85f, 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0, CrawlParam);
            AddTransition(state, crawl, 0.85f, 0.2f).AddCondition(AnimatorConditionMode.If, 0, CrawlParam);
        }

        PruneOrphanSubAssets(controller);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return clips.Count == ClipDefs.Length
            ? "Animator rebuilt with every clip assigned."
            : "Animator rebuilt, but some states have no clip assigned.";
    }

    static AnimationClip Clip(Dictionary<string, AnimationClip> clips, string name)
    {
        return clips.TryGetValue(name, out AnimationClip clip) ? clip : null;
    }

    static void RebuildParameters(AnimatorController controller)
    {
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);

        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(CrawlParam, AnimatorControllerParameterType.Bool);
        controller.AddParameter(AttackParam, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(AttackVariantParam, AnimatorControllerParameterType.Int);
        controller.AddParameter(ScreamParam, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(DieParam, AnimatorControllerParameterType.Trigger);
    }

    static AnimatorStateMachine PrepareRootStateMachine(AnimatorController controller)
    {
        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].name = "Base Layer";
        layers[0].defaultWeight = 1f;
        controller.layers = layers;

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        root.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
        root.entryTransitions = Array.Empty<AnimatorTransition>();

        foreach (ChildAnimatorStateMachine child in root.stateMachines.ToArray())
            root.RemoveStateMachine(child.stateMachine);
        foreach (ChildAnimatorState child in root.states.ToArray())
            root.RemoveState(child.state);

        return root;
    }

    static AnimatorState AddState(AnimatorStateMachine root, string name, AnimationClip clip, Vector3 position)
    {
        AnimatorState state = root.AddState(name, position);
        state.motion = clip;
        return state;
    }

    static AnimatorState AddBlendTreeState(AnimatorController controller, AnimatorStateMachine root,
        string name, Vector3 position, params (AnimationClip clip, float threshold)[] entries)
    {
        var tree = new BlendTree
        {
            name = name,
            hideFlags = HideFlags.HideInHierarchy,
            blendType = BlendTreeType.Simple1D,
            blendParameter = SpeedParam,
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, controller);

        foreach ((AnimationClip clip, float threshold) in entries)
        {
            if (clip != null)
                tree.AddChild(clip, threshold);
        }

        AnimatorState state = root.AddState(name, position);
        state.motion = tree;
        return state;
    }

    static AnimatorStateTransition AddAnyStateTransition(AnimatorStateMachine root, AnimatorState to, float duration)
    {
        AnimatorStateTransition transition = root.AddAnyStateTransition(to);
        transition.conditions = Array.Empty<AnimatorCondition>();
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        return transition;
    }

    static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.conditions = Array.Empty<AnimatorCondition>();
        transition.hasExitTime = exitTime >= 0f;
        if (exitTime >= 0f)
            transition.exitTime = exitTime;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        return transition;
    }

    /// <summary>
    /// Removing states leaves their blend trees and transitions behind as sub-assets,
    /// which would pile up every time this tool re-runs.
    /// </summary>
    static void PruneOrphanSubAssets(AnimatorController controller)
    {
        var reachable = new HashSet<UnityEngine.Object>();
        foreach (AnimatorControllerLayer layer in controller.layers)
            CollectStateMachine(layer.stateMachine, reachable);

        bool removedAny = false;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
        {
            if (asset == null || asset == controller || reachable.Contains(asset))
                continue;

            bool isGraphObject = asset is AnimatorStateMachine
                || asset is AnimatorState
                || asset is AnimatorTransitionBase
                || asset is BlendTree;

            if (!isGraphObject)
                continue;

            DestroyImmediate(asset, true);
            removedAny = true;
        }

        if (removedAny)
            AssetDatabase.SaveAssets();
    }

    static void CollectStateMachine(AnimatorStateMachine stateMachine, HashSet<UnityEngine.Object> reachable)
    {
        if (stateMachine == null || !reachable.Add(stateMachine))
            return;

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            reachable.Add(transition);
        foreach (AnimatorTransition transition in stateMachine.entryTransitions)
            reachable.Add(transition);

        foreach (ChildAnimatorState child in stateMachine.states)
        {
            reachable.Add(child.state);
            foreach (AnimatorStateTransition transition in child.state.transitions)
                reachable.Add(transition);
            CollectMotion(child.state.motion, reachable);
        }

        foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
        {
            foreach (AnimatorTransition transition in stateMachine.GetStateMachineTransitions(child.stateMachine))
                reachable.Add(transition);
            CollectStateMachine(child.stateMachine, reachable);
        }
    }

    static void CollectMotion(Motion motion, HashSet<UnityEngine.Object> reachable)
    {
        if (motion is not BlendTree tree || !reachable.Add(tree))
            return;

        foreach (ChildMotion child in tree.children)
            CollectMotion(child.motion, reachable);
    }

    #endregion
}
#endif
