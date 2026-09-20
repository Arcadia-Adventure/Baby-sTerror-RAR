#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RagdollCreatorWindow : EditorWindow
{
    [SerializeField] GameObject root;
    [SerializeField] RagdollBoneSlots slots = new RagdollBoneSlots();
    [SerializeField] float totalMass = 20f;
    [SerializeField] float colliderRadiusScale = 1f;
    [SerializeField] float linearDamping = 0.05f;
    [SerializeField] float angularDamping = 0.4f;
    [SerializeField] bool includeFeet = true;
    [SerializeField] bool includeHands;
    [SerializeField] bool flipForward;
    [SerializeField] bool addController = true;
    [SerializeField] bool autoMassFromSize = true;

    Vector2 _scroll;
    bool _bonesFoldout = true;
    bool _settingsFoldout = true;
    string _status = "Select a character to begin.";
    MessageType _statusType = MessageType.Info;

    static readonly Color BoneColor = new Color(0.25f, 0.85f, 1f, 0.95f);
    static readonly Color CapsuleColor = new Color(0.3f, 1f, 0.45f, 0.35f);

    [MenuItem("Ommy/Ragdoll Creator")]
    [MenuItem("Tools/Ragdoll Creator")]
    public static void Open()
    {
        var window = GetWindow<RagdollCreatorWindow>("Ragdoll Creator");
        window.minSize = new Vector2(420, 560);
        window.TryAssignSelection();
        window.Show();
    }

    [MenuItem("GameObject/Ommy/Ragdoll Creator", false, 25)]
    static void OpenFromHierarchy()
    {
        Open();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        TryAssignSelection();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSelectionChange()
    {
        Repaint();
    }

    void TryAssignSelection()
    {
        if (root != null || Selection.activeGameObject == null)
            return;

        var selected = Selection.activeGameObject;
        if (selected.GetComponentInChildren<Animator>() != null || selected.GetComponentInChildren<SkinnedMeshRenderer>() != null)
        {
            root = selected;
            DetectBones();
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Ragdoll Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select a character, detect bones, then Create Ragdoll. Works with Humanoid avatars, Mixamo, and Toon Baby (TB) skeletons.",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        var newRoot = (GameObject)EditorGUILayout.ObjectField("Character", root, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
        {
            root = newRoot;
            if (root != null)
                DetectBones();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selection", GUILayout.Height(22)))
            {
                if (Selection.activeGameObject != null)
                {
                    root = Selection.activeGameObject;
                    DetectBones();
                }
            }
            if (GUILayout.Button("Detect Bones", GUILayout.Height(22)))
                DetectBones();
        }

        if (root != null && PrefabUtility.IsPartOfPrefabInstance(root))
            EditorGUILayout.HelpBox("Prefab instance — apply the prefab after creating the ragdoll to keep the setup.", MessageType.Info);

        DrawBoneSlots();
        DrawSettings();

        EditorGUILayout.Space(8);
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, _statusType);

        using (new EditorGUI.DisabledScope(root == null))
        {
            GUI.backgroundColor = new Color(0.45f, 0.9f, 0.5f);
            if (GUILayout.Button("Create Ragdoll", GUILayout.Height(32)))
                CreateRagdoll();
            GUI.backgroundColor = Color.white;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Ragdoll", GUILayout.Height(24)))
                    ClearRagdoll();
                if (GUILayout.Button("Select Character", GUILayout.Height(24)) && root != null)
                    Selection.activeGameObject = root;
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    void DrawBoneSlots()
    {
        _bonesFoldout = EditorGUILayout.Foldout(_bonesFoldout, BoneHeader(), true);
        if (!_bonesFoldout)
            return;

        using (new EditorGUI.IndentLevelScope())
        {
            slots.pelvis = BoneField("Pelvis / Hips", slots.pelvis, true);
            slots.spine = BoneField("Spine / Chest", slots.spine, false);
            slots.head = BoneField("Head", slots.head, true);

            EditorGUILayout.Space(4);
            slots.leftHips = BoneField("Left Upper Leg", slots.leftHips, true);
            slots.leftKnee = BoneField("Left Lower Leg", slots.leftKnee, true);
            slots.leftFoot = BoneField("Left Foot", slots.leftFoot, includeFeet);

            EditorGUILayout.Space(4);
            slots.rightHips = BoneField("Right Upper Leg", slots.rightHips, true);
            slots.rightKnee = BoneField("Right Lower Leg", slots.rightKnee, true);
            slots.rightFoot = BoneField("Right Foot", slots.rightFoot, includeFeet);

            EditorGUILayout.Space(4);
            slots.leftArm = BoneField("Left Upper Arm", slots.leftArm, true);
            slots.leftElbow = BoneField("Left Lower Arm", slots.leftElbow, true);
            slots.leftHand = BoneField("Left Hand", slots.leftHand, includeHands);

            EditorGUILayout.Space(4);
            slots.rightArm = BoneField("Right Upper Arm", slots.rightArm, true);
            slots.rightElbow = BoneField("Right Lower Arm", slots.rightElbow, true);
            slots.rightHand = BoneField("Right Hand", slots.rightHand, includeHands);
        }
    }

    string BoneHeader()
    {
        GetRequiredCount(out int assigned, out int required);
        return $"Bones  ({assigned}/{required})";
    }

    Transform BoneField(string label, Transform value, bool required)
    {
        var color = value != null ? GUI.color : (required ? new Color(1f, 0.85f, 0.85f) : GUI.color);
        var prev = GUI.color;
        GUI.color = color;
        var result = (Transform)EditorGUILayout.ObjectField(required ? label + " *" : label, value, typeof(Transform), true);
        GUI.color = prev;
        return result;
    }

    void DrawSettings()
    {
        _settingsFoldout = EditorGUILayout.Foldout(_settingsFoldout, "Settings", true);
        if (!_settingsFoldout)
            return;

        using (new EditorGUI.IndentLevelScope())
        {
            autoMassFromSize = EditorGUILayout.Toggle("Auto Mass From Size", autoMassFromSize);
            using (new EditorGUI.DisabledScope(autoMassFromSize))
                totalMass = EditorGUILayout.Slider("Total Mass", totalMass, 1f, 120f);
            colliderRadiusScale = EditorGUILayout.Slider("Collider Scale", colliderRadiusScale, 0.3f, 2.5f);
            linearDamping = EditorGUILayout.Slider("Linear Damping", linearDamping, 0f, 2f);
            angularDamping = EditorGUILayout.Slider("Angular Damping", angularDamping, 0f, 3f);
            includeFeet = EditorGUILayout.Toggle("Include Feet", includeFeet);
            includeHands = EditorGUILayout.Toggle("Include Hands", includeHands);
            flipForward = EditorGUILayout.Toggle("Flip Forward Axis", flipForward);
            addController = EditorGUILayout.Toggle("Add Ragdoll Controller", addController);
        }
    }

    void DetectBones()
    {
        if (root == null)
        {
            SetStatus("Assign a character first.", MessageType.Warning);
            return;
        }

        slots = RagdollBoneDetector.Detect(root);
        if (autoMassFromSize)
            totalMass = EstimateMass(root);

        GetRequiredCount(out int assigned, out int required);
        if (assigned >= required)
            SetStatus($"Detected {assigned} bones. Ready to create.", MessageType.Info);
        else
            SetStatus($"Detected {assigned}/{required} required bones. Fill the missing slots (marked *) or check the skeleton.", MessageType.Warning);

        SceneView.RepaintAll();
        Repaint();
    }

    static float EstimateMass(GameObject character)
    {
        var renderers = character.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return 20f;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float height = Mathf.Max(0.2f, bounds.size.y);
        return Mathf.Clamp(20f * (height / 1.7f), 1.5f, 90f);
    }

    void GetRequiredCount(out int assigned, out int required)
    {
        assigned = 0;
        required = 0;
        Count(slots.pelvis, true, ref assigned, ref required);
        Count(slots.head, true, ref assigned, ref required);
        Count(slots.leftHips, true, ref assigned, ref required);
        Count(slots.leftKnee, true, ref assigned, ref required);
        Count(slots.leftFoot, includeFeet, ref assigned, ref required);
        Count(slots.rightHips, true, ref assigned, ref required);
        Count(slots.rightKnee, true, ref assigned, ref required);
        Count(slots.rightFoot, includeFeet, ref assigned, ref required);
        Count(slots.leftArm, true, ref assigned, ref required);
        Count(slots.leftElbow, true, ref assigned, ref required);
        Count(slots.leftHand, includeHands, ref assigned, ref required);
        Count(slots.rightArm, true, ref assigned, ref required);
        Count(slots.rightElbow, true, ref assigned, ref required);
        Count(slots.rightHand, includeHands, ref assigned, ref required);
    }

    static void Count(Transform bone, bool isRequired, ref int assigned, ref int required)
    {
        if (!isRequired)
            return;
        required++;
        if (bone != null)
            assigned++;
    }

    void CreateRagdoll()
    {
        if (root == null)
        {
            SetStatus("Assign a character first.", MessageType.Error);
            return;
        }

        string error = RagdollBuilder.Check(slots, includeFeet, includeHands);
        if (!string.IsNullOrEmpty(error))
        {
            SetStatus(error, MessageType.Error);
            return;
        }

        var existing = root.GetComponent<RagdollController>();
        if (existing != null || HasRagdollParts(root))
        {
            if (!EditorUtility.DisplayDialog("Replace Ragdoll",
                    "This character already has a ragdoll. Replace it?", "Replace", "Cancel"))
                return;
            ClearRagdollInternal(existing);
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Ragdoll");

        try
        {
            var result = RagdollBuilder.Build(slots, new RagdollBuildSettings
            {
                TotalMass = totalMass,
                RadiusScale = colliderRadiusScale,
                LinearDamping = linearDamping,
                AngularDamping = angularDamping,
                IncludeFeet = includeFeet,
                IncludeHands = includeHands,
                FlipForward = flipForward
            });

            if (addController)
            {
                var controller = Undo.AddComponent<RagdollController>(root);
                var animator = root.GetComponentInChildren<Animator>();
                var rootBody = root.GetComponent<Rigidbody>();
                var animated = CollectAnimatedColliders(root);
                controller.EditorAssign(animator, rootBody, animated, result.Hips, result.Bodies, result.Colliders, result.Joints);
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(root);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(root.scene);

            SetStatus($"Created ragdoll with {result.Bodies.Length} rigidbodies.", MessageType.Info);
        }
        catch (Exception ex)
        {
            SetStatus("Failed to create ragdoll: " + ex.Message, MessageType.Error);
            Debug.LogException(ex);
        }

        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }

    void ClearRagdoll()
    {
        if (root == null)
            return;

        var controller = root.GetComponent<RagdollController>();
        if (controller == null && !HasRagdollParts(root))
        {
            SetStatus("No ragdoll found on this character.", MessageType.Warning);
            return;
        }

        if (!EditorUtility.DisplayDialog("Clear Ragdoll",
                "Remove ragdoll rigidbodies, colliders, and joints from this character?", "Clear", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clear Ragdoll");
        ClearRagdollInternal(controller);
        Undo.CollapseUndoOperations(group);

        SetStatus("Ragdoll removed.", MessageType.Info);
        SceneView.RepaintAll();
    }

    void ClearRagdollInternal(RagdollController controller)
    {
        var parts = new List<UnityEngine.Object>();
        var rootBody = root != null ? root.GetComponent<Rigidbody>() : null;

        if (controller != null)
            controller.EditorCollectCreatedParts(parts);

        if (controller == null)
        {
            var joints = root.GetComponentsInChildren<CharacterJoint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                var joint = joints[i];
                if (joint == null)
                    continue;
                var go = joint.gameObject;
                parts.Add(joint);
                var col = go.GetComponent<Collider>();
                if (col is CapsuleCollider || col is BoxCollider)
                    parts.Add(col);
                var body = go.GetComponent<Rigidbody>();
                if (body != null && body != rootBody && go != root)
                    parts.Add(body);
            }
        }

        if (controller != null)
            Undo.DestroyObjectImmediate(controller);

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
                Undo.DestroyObjectImmediate(parts[i]);
        }

        if (root != null)
        {
            EditorUtility.SetDirty(root);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(root.scene);
        }
    }

    static Collider[] CollectAnimatedColliders(GameObject character)
    {
        var list = new List<Collider>(character.GetComponents<Collider>());
        var interactable = character.GetComponent<Interactable>();
        if (interactable != null && interactable.collider != null && !list.Contains(interactable.collider))
            list.Add(interactable.collider);
        return list.ToArray();
    }

    static bool HasRagdollParts(GameObject character)
    {
        return character.GetComponentInChildren<CharacterJoint>(true) != null;
    }

    void SetStatus(string message, MessageType type)
    {
        _status = message;
        _statusType = type;
        Repaint();
    }

    void OnSceneGUI(SceneView view)
    {
        if (root == null)
            return;

        DrawBoneLink(slots.pelvis, slots.spine, true);
        DrawBoneLink(slots.spine != null ? slots.spine : slots.pelvis, slots.head, true);
        DrawBoneLink(slots.pelvis, slots.leftHips, true);
        DrawBoneLink(slots.leftHips, slots.leftKnee, true);
        DrawBoneLink(slots.leftKnee, slots.leftFoot, includeFeet);
        DrawBoneLink(slots.pelvis, slots.rightHips, true);
        DrawBoneLink(slots.rightHips, slots.rightKnee, true);
        DrawBoneLink(slots.rightKnee, slots.rightFoot, includeFeet);
        DrawBoneLink(slots.spine != null ? slots.spine : slots.pelvis, slots.leftArm, true);
        DrawBoneLink(slots.leftArm, slots.leftElbow, true);
        DrawBoneLink(slots.leftElbow, slots.leftHand, includeHands);
        DrawBoneLink(slots.spine != null ? slots.spine : slots.pelvis, slots.rightArm, true);
        DrawBoneLink(slots.rightArm, slots.rightElbow, true);
        DrawBoneLink(slots.rightElbow, slots.rightHand, includeHands);

        LabelBone(slots.pelvis, "Pelvis");
        LabelBone(slots.head, "Head");
    }

    static void DrawBoneLink(Transform a, Transform b, bool required)
    {
        if (a == null || b == null)
            return;

        Handles.color = required ? BoneColor : CapsuleColor;
        Handles.DrawLine(a.position, b.position, 3f);
        float radius = Vector3.Distance(a.position, b.position) * 0.12f * 1f;
        Handles.color = CapsuleColor;
        Handles.SphereHandleCap(0, a.position, Quaternion.identity, radius * 0.35f, EventType.Repaint);
        Handles.SphereHandleCap(0, b.position, Quaternion.identity, radius * 0.35f, EventType.Repaint);
    }

    static void LabelBone(Transform bone, string label)
    {
        if (bone == null)
            return;
        Handles.Label(bone.position + Vector3.up * 0.03f, label);
    }
}

[Serializable]
public class RagdollBoneSlots
{
    public Transform pelvis;
    public Transform spine;
    public Transform head;
    public Transform leftHips;
    public Transform leftKnee;
    public Transform leftFoot;
    public Transform rightHips;
    public Transform rightKnee;
    public Transform rightFoot;
    public Transform leftArm;
    public Transform leftElbow;
    public Transform leftHand;
    public Transform rightArm;
    public Transform rightElbow;
    public Transform rightHand;
}

public class RagdollBuildSettings
{
    public float TotalMass = 20f;
    public float RadiusScale = 1f;
    public float LinearDamping = 0.05f;
    public float AngularDamping = 0.4f;
    public bool IncludeFeet;
    public bool IncludeHands;
    public bool FlipForward;
}

public class RagdollBuildResult
{
    public Rigidbody Hips;
    public Rigidbody[] Bodies;
    public Collider[] Colliders;
    public CharacterJoint[] Joints;
}

static class RagdollBoneDetector
{
    enum Slot
    {
        Pelvis, Spine, Head,
        LeftHips, LeftKnee, LeftFoot,
        RightHips, RightKnee, RightFoot,
        LeftArm, LeftElbow, LeftHand,
        RightArm, RightElbow, RightHand
    }

    static readonly Slot[] ScoreOrder =
    {
        Slot.Head, Slot.LeftHand, Slot.RightHand, Slot.LeftFoot, Slot.RightFoot,
        Slot.LeftElbow, Slot.RightElbow, Slot.LeftKnee, Slot.RightKnee,
        Slot.LeftArm, Slot.RightArm, Slot.LeftHips, Slot.RightHips,
        Slot.Spine, Slot.Pelvis
    };

    public static RagdollBoneSlots Detect(GameObject root)
    {
        var slots = new RagdollBoneSlots();
        var animator = root.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid)
            AssignHumanoid(animator, slots);

        var transforms = root.GetComponentsInChildren<Transform>(true);
        var used = new HashSet<Transform>();
        AddUsed(slots, used);

        var best = new Dictionary<Slot, (Transform bone, int score)>();
        for (int i = 0; i < transforms.Length; i++)
        {
            var t = transforms[i];
            if (t == null || used.Contains(t))
                continue;

            string name = Normalize(t.name);
            if (string.IsNullOrEmpty(name) || name == "tb")
                continue;

            foreach (Slot slot in ScoreOrder)
            {
                if (Get(slots, slot) != null)
                    continue;
                int score = Score(name, slot);
                if (score <= 0)
                    continue;
                if (!best.TryGetValue(slot, out var current) || score > current.score)
                    best[slot] = (t, score);
            }
        }

        foreach (Slot slot in ScoreOrder)
        {
            if (Get(slots, slot) != null)
                continue;
            if (best.TryGetValue(slot, out var pick) && pick.bone != null && !used.Contains(pick.bone))
            {
                Set(slots, slot, pick.bone);
                used.Add(pick.bone);
            }
        }

        return slots;
    }

    static void AssignHumanoid(Animator animator, RagdollBoneSlots slots)
    {
        slots.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        slots.spine = animator.GetBoneTransform(HumanBodyBones.Chest)
                      ?? animator.GetBoneTransform(HumanBodyBones.UpperChest)
                      ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        slots.head = animator.GetBoneTransform(HumanBodyBones.Head)
                     ?? animator.GetBoneTransform(HumanBodyBones.Neck);
        slots.leftHips = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        slots.leftKnee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        slots.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        slots.rightHips = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        slots.rightKnee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        slots.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        slots.leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        slots.leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        slots.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        slots.rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        slots.rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        slots.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    static void AddUsed(RagdollBoneSlots slots, HashSet<Transform> used)
    {
        void Add(Transform t)
        {
            if (t != null) used.Add(t);
        }
        Add(slots.pelvis); Add(slots.spine); Add(slots.head);
        Add(slots.leftHips); Add(slots.leftKnee); Add(slots.leftFoot);
        Add(slots.rightHips); Add(slots.rightKnee); Add(slots.rightFoot);
        Add(slots.leftArm); Add(slots.leftElbow); Add(slots.leftHand);
        Add(slots.rightArm); Add(slots.rightElbow); Add(slots.rightHand);
    }

    static Transform Get(RagdollBoneSlots slots, Slot slot)
    {
        switch (slot)
        {
            case Slot.Pelvis: return slots.pelvis;
            case Slot.Spine: return slots.spine;
            case Slot.Head: return slots.head;
            case Slot.LeftHips: return slots.leftHips;
            case Slot.LeftKnee: return slots.leftKnee;
            case Slot.LeftFoot: return slots.leftFoot;
            case Slot.RightHips: return slots.rightHips;
            case Slot.RightKnee: return slots.rightKnee;
            case Slot.RightFoot: return slots.rightFoot;
            case Slot.LeftArm: return slots.leftArm;
            case Slot.LeftElbow: return slots.leftElbow;
            case Slot.LeftHand: return slots.leftHand;
            case Slot.RightArm: return slots.rightArm;
            case Slot.RightElbow: return slots.rightElbow;
            case Slot.RightHand: return slots.rightHand;
            default: return null;
        }
    }

    static void Set(RagdollBoneSlots slots, Slot slot, Transform value)
    {
        switch (slot)
        {
            case Slot.Pelvis: slots.pelvis = value; break;
            case Slot.Spine: slots.spine = value; break;
            case Slot.Head: slots.head = value; break;
            case Slot.LeftHips: slots.leftHips = value; break;
            case Slot.LeftKnee: slots.leftKnee = value; break;
            case Slot.LeftFoot: slots.leftFoot = value; break;
            case Slot.RightHips: slots.rightHips = value; break;
            case Slot.RightKnee: slots.rightKnee = value; break;
            case Slot.RightFoot: slots.rightFoot = value; break;
            case Slot.LeftArm: slots.leftArm = value; break;
            case Slot.LeftElbow: slots.leftElbow = value; break;
            case Slot.LeftHand: slots.leftHand = value; break;
            case Slot.RightArm: slots.rightArm = value; break;
            case Slot.RightElbow: slots.rightElbow = value; break;
            case Slot.RightHand: slots.rightHand = value; break;
        }
    }

    static int Score(string name, Slot slot)
    {
        string[] tokens = Tokens(slot);
        int best = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            int specificity = (tokens.Length - i) * 5;
            if (name == token)
                return 1000 + token.Length + specificity;
            if (name.EndsWith(token))
                best = Mathf.Max(best, 400 + token.Length + specificity);
            else if (name.Contains(token))
                best = Mathf.Max(best, 120 + token.Length + specificity);
        }

        if (best <= 0)
            return 0;

        bool wantsLeft = slot.ToString().StartsWith("Left");
        bool wantsRight = slot.ToString().StartsWith("Right");
        bool hasLeft = HasLeft(name);
        bool hasRight = HasRight(name);

        if (wantsLeft && hasRight) return 0;
        if (wantsRight && hasLeft) return 0;
        if (wantsLeft && !hasLeft) return 0;
        if (wantsRight && !hasRight) return 0;
        if (!wantsLeft && !wantsRight && (hasLeft || hasRight) && slot != Slot.Spine && slot != Slot.Pelvis && slot != Slot.Head)
            return 0;

        return best;
    }

    static bool HasLeft(string name)
    {
        if (name.Contains("right"))
            return false;
        if (name.Contains("left"))
            return true;
        return name.StartsWith("lthigh") || name.StartsWith("lcalf") || name.StartsWith("lfoot")
               || name.StartsWith("lupper") || name.StartsWith("lfore") || name.StartsWith("lhand")
               || name.StartsWith("lupleg") || name.StartsWith("llow") || name.StartsWith("lclav")
               || name.StartsWith("lfinger") || name.StartsWith("larm") || name.StartsWith("lhip")
               || name.StartsWith("lleg") || name.StartsWith("lshin") || name.EndsWith("l");
    }

    static bool HasRight(string name)
    {
        if (name.Contains("left"))
            return false;
        if (name.Contains("right"))
            return true;
        return name.StartsWith("rthigh") || name.StartsWith("rcalf") || name.StartsWith("rfoot")
               || name.StartsWith("rupper") || name.StartsWith("rfore") || name.StartsWith("rhand")
               || name.StartsWith("rupleg") || name.StartsWith("rlow") || name.StartsWith("rclav")
               || name.StartsWith("rfinger") || name.StartsWith("rarm") || name.StartsWith("rhip")
               || name.StartsWith("rleg") || name.StartsWith("rshin") || name.EndsWith("r");
    }

    static string[] Tokens(Slot slot)
    {
        switch (slot)
        {
            case Slot.Pelvis: return new[] { "pelvis", "hips", "hip" };
            case Slot.Spine: return new[] { "spine1", "spine2", "chest", "upperchest", "spine" };
            case Slot.Head: return new[] { "head", "neck" };
            case Slot.LeftHips: return new[] { "leftupperleg", "leftupleg", "leftthigh", "lthigh", "lhip", "lefthip", "uplegl", "thighl" };
            case Slot.LeftKnee: return new[] { "leftlowerleg", "leftleg", "leftcalf", "lcalf", "lleg", "calfl", "legl" };
            case Slot.LeftFoot: return new[] { "leftfoot", "lfoot", "footl" };
            case Slot.RightHips: return new[] { "rightupperleg", "rightupleg", "rightthigh", "rthigh", "rhip", "righthip", "uplegr", "thighr" };
            case Slot.RightKnee: return new[] { "rightlowerleg", "rightleg", "rightcalf", "rcalf", "rleg", "calfr", "legr" };
            case Slot.RightFoot: return new[] { "rightfoot", "rfoot", "footr" };
            case Slot.LeftArm: return new[] { "leftupperarm", "leftarm", "lupperarm", "luparm", "arml", "upperarml" };
            case Slot.LeftElbow: return new[] { "leftlowerarm", "leftforearm", "lforearm", "llowarm", "forearml" };
            case Slot.LeftHand: return new[] { "lefthand", "lhand", "handl" };
            case Slot.RightArm: return new[] { "rightupperarm", "rightarm", "rupperarm", "ruparm", "armr", "upperarmr" };
            case Slot.RightElbow: return new[] { "rightlowerarm", "rightforearm", "rforearm", "rlowarm", "forearmr" };
            case Slot.RightHand: return new[] { "righthand", "rhand", "handr" };
            default: return Array.Empty<string>();
        }
    }

    static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        string s = name.ToLowerInvariant();
        s = s.Replace("mixamorig", string.Empty);
        s = s.Replace("bip001", string.Empty);
        s = s.Replace("bip01", string.Empty);

        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        s = sb.ToString();
        if (s.StartsWith("tb") && s.Length > 2)
            s = s.Substring(2);
        return s;
    }
}

static class RagdollBuilder
{
    class BoneInfo
    {
        public string Name;
        public Transform Anchor;
        public Transform LengthHint;
        public BoneInfo Parent;
        public readonly List<BoneInfo> Children = new List<BoneInfo>();
        public float MinLimit;
        public float MaxLimit;
        public float SwingLimit;
        public Vector3 Axis;
        public Vector3 NormalAxis;
        public float RadiusScale;
        public float Density;
        public float SummedMass;
        public bool UseBox;
        public bool IsHead;
    }

    public static string Check(RagdollBoneSlots slots, bool includeFeet, bool includeHands)
    {
        if (slots.pelvis == null) return "Pelvis is required.";
        if (slots.head == null) return "Head is required.";
        if (slots.leftHips == null || slots.rightHips == null) return "Upper legs are required.";
        if (slots.leftKnee == null || slots.rightKnee == null) return "Lower legs are required.";
        if (slots.leftArm == null || slots.rightArm == null) return "Upper arms are required.";
        if (slots.leftElbow == null || slots.rightElbow == null) return "Lower arms are required.";
        if (includeFeet && (slots.leftFoot == null || slots.rightFoot == null)) return "Feet are required when Include Feet is enabled.";
        if (includeHands && (slots.leftHand == null || slots.rightHand == null)) return "Hands are required when Include Hands is enabled.";
        return null;
    }

    public static RagdollBuildResult Build(RagdollBoneSlots slots, RagdollBuildSettings settings)
    {
        Vector3 right, up, forward;
        CalculateAxes(slots, settings.FlipForward, out right, out up, out forward);

        Vector3 worldRight = slots.pelvis.TransformDirection(right);
        Vector3 worldUp = slots.pelvis.TransformDirection(up);
        Vector3 worldForward = slots.pelvis.TransformDirection(forward);

        var bones = new List<BoneInfo>();
        var pelvis = AddBone(bones, null, "Pelvis", slots.pelvis, worldRight, worldForward, 0, 0, 0, 1f, 2.5f, true, false, null);

        var leftHips = AddBone(bones, pelvis, "Left Hips", slots.leftHips, worldRight, worldForward, -20, 70, 30, 0.3f, 1.5f, false, false, slots.leftKnee);
        var rightHips = AddBone(bones, pelvis, "Right Hips", slots.rightHips, worldRight, worldForward, -20, 70, 30, 0.3f, 1.5f, false, false, slots.rightKnee);
        var leftKnee = AddBone(bones, leftHips, "Left Knee", slots.leftKnee, worldRight, worldForward, -80, 0, 0, 0.25f, 1.5f, false, false, slots.leftFoot);
        var rightKnee = AddBone(bones, rightHips, "Right Knee", slots.rightKnee, worldRight, worldForward, -80, 0, 0, 0.25f, 1.5f, false, false, slots.rightFoot);

        if (settings.IncludeFeet)
        {
            AddBone(bones, leftKnee, "Left Foot", slots.leftFoot, worldRight, worldForward, -20, 20, 15, 0.25f, 0.75f, false, false, null);
            AddBone(bones, rightKnee, "Right Foot", slots.rightFoot, worldRight, worldForward, -20, 20, 15, 0.25f, 0.75f, false, false, null);
        }

        BoneInfo torso = pelvis;
        if (slots.spine != null)
            torso = AddBone(bones, pelvis, "Spine", slots.spine, worldRight, worldForward, -20, 20, 10, 1f, 2.5f, true, false, null);

        var leftArm = AddBone(bones, torso, "Left Arm", slots.leftArm, worldUp, worldForward, -70, 10, 50, 0.25f, 1f, false, false, slots.leftElbow);
        var rightArm = AddBone(bones, torso, "Right Arm", slots.rightArm, worldUp, worldForward, -70, 10, 50, 0.25f, 1f, false, false, slots.rightElbow);
        var leftElbow = AddBone(bones, leftArm, "Left Elbow", slots.leftElbow, worldForward, worldUp, -90, 0, 0, 0.2f, 1f, false, false, slots.leftHand);
        var rightElbow = AddBone(bones, rightArm, "Right Elbow", slots.rightElbow, worldForward, worldUp, -90, 0, 0, 0.2f, 1f, false, false, slots.rightHand);

        if (settings.IncludeHands)
        {
            AddBone(bones, leftElbow, "Left Hand", slots.leftHand, worldForward, worldUp, -40, 40, 20, 0.2f, 0.5f, false, false, null);
            AddBone(bones, rightElbow, "Right Hand", slots.rightHand, worldForward, worldUp, -40, 40, 20, 0.2f, 0.5f, false, false, null);
        }

        AddBone(bones, torso, "Head", slots.head, worldRight, worldForward, -40, 25, 25, 1f, 1f, false, true, null);

        var bodies = new List<Rigidbody>();
        var colliders = new List<Collider>();
        var joints = new List<CharacterJoint>();

        BuildColliders(bones, slots, settings.RadiusScale, colliders);
        BuildBodies(bones, settings, bodies);
        BuildJoints(bones, joints);
        CalculateMass(pelvis, bones, settings.TotalMass);

        for (int i = 0; i < bodies.Count; i++)
        {
            var rb = bodies[i];
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        for (int i = 0; i < colliders.Count; i++)
            colliders[i].enabled = false;

        return new RagdollBuildResult
        {
            Hips = pelvis.Anchor.GetComponent<Rigidbody>(),
            Bodies = bodies.ToArray(),
            Colliders = colliders.ToArray(),
            Joints = joints.ToArray()
        };
    }

    static BoneInfo AddBone(
        List<BoneInfo> bones, BoneInfo parent, string name, Transform anchor,
        Vector3 axis, Vector3 normalAxis, float minLimit, float maxLimit, float swingLimit,
        float radiusScale, float density, bool useBox, bool isHead, Transform lengthHint)
    {
        var bone = new BoneInfo
        {
            Name = name,
            Anchor = anchor,
            Parent = parent,
            Axis = axis,
            NormalAxis = normalAxis,
            MinLimit = minLimit,
            MaxLimit = maxLimit,
            SwingLimit = swingLimit,
            RadiusScale = radiusScale,
            Density = density,
            UseBox = useBox,
            IsHead = isHead,
            LengthHint = lengthHint
        };
        parent?.Children.Add(bone);
        bones.Add(bone);
        return bone;
    }

    static void CalculateAxes(RagdollBoneSlots slots, bool flipForward, out Vector3 right, out Vector3 up, out Vector3 forward)
    {
        up = CalculateDirectionAxis(slots.pelvis.InverseTransformPoint(slots.head.position));
        Vector3 removed, unused;
        DecomposeVector(out unused, out removed, slots.pelvis.InverseTransformPoint(slots.rightArm.position), up);
        right = CalculateDirectionAxis(removed);
        forward = Vector3.Cross(right, up);
        if (flipForward)
            forward = -forward;
    }

    static void DecomposeVector(out Vector3 normalCompo, out Vector3 tangentCompo, Vector3 outwardDir, Vector3 outwardNormal)
    {
        outwardNormal = outwardNormal.normalized;
        normalCompo = outwardNormal * Vector3.Dot(outwardDir, outwardNormal);
        tangentCompo = outwardDir - normalCompo;
    }

    static void BuildColliders(List<BoneInfo> bones, RagdollBoneSlots slots, float radiusScale, List<Collider> colliders)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone.Anchor == null)
                continue;

            if (bone.UseBox)
            {
                var box = CreateBox(bone, slots, radiusScale);
                if (box != null) colliders.Add(box);
                continue;
            }

            var capsule = CreateCapsule(bone, radiusScale);
            if (capsule != null) colliders.Add(capsule);
        }
    }

    static BoxCollider CreateBox(BoneInfo bone, RagdollBoneSlots slots, float radiusScale)
    {
        var box = Undo.AddComponent<BoxCollider>(bone.Anchor.gameObject);
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool initialized = false;

        void Encapsulate(Transform t)
        {
            if (t == null) return;
            Vector3 local = bone.Anchor.InverseTransformPoint(t.position);
            if (!initialized)
            {
                bounds = new Bounds(local, Vector3.zero);
                initialized = true;
            }
            else bounds.Encapsulate(local);
        }

        if (bone.Name == "Pelvis")
        {
            Encapsulate(slots.leftHips);
            Encapsulate(slots.rightHips);
            Encapsulate(slots.spine != null ? slots.spine : slots.head);
        }
        else
        {
            Encapsulate(slots.leftArm);
            Encapsulate(slots.rightArm);
            Encapsulate(slots.head);
            Encapsulate(slots.pelvis);
        }

        if (!initialized)
            bounds = new Bounds(Vector3.zero, Vector3.one * 0.1f);

        Vector3 size = bounds.size;
        size.x = Mathf.Max(size.x, 0.04f) * radiusScale;
        size.y = Mathf.Max(size.y, 0.04f) * radiusScale;
        size.z = Mathf.Max(size.z, 0.04f) * radiusScale;
        box.center = bounds.center;
        box.size = size;
        return box;
    }

    static CapsuleCollider CreateCapsule(BoneInfo bone, float radiusScale)
    {
        Transform end = null;
        if (bone.Children.Count == 1)
            end = bone.Children[0].Anchor;
        else if (bone.LengthHint != null)
            end = bone.LengthHint;

        int direction;
        float distance;
        if (end != null)
        {
            CalculateDirection(bone.Anchor.InverseTransformPoint(end.position), out direction, out distance);
        }
        else if (bone.Parent != null)
        {
            Vector3 mirrored = bone.Anchor.position + (bone.Anchor.position - bone.Parent.Anchor.position);
            CalculateDirection(bone.Anchor.InverseTransformPoint(mirrored), out direction, out distance);
        }
        else
        {
            direction = 1;
            distance = 0.1f;
        }

        if (Mathf.Abs(distance) < 0.01f)
            distance = distance >= 0 ? 0.08f : -0.08f;

        var capsule = Undo.AddComponent<CapsuleCollider>(bone.Anchor.gameObject);
        capsule.direction = direction;
        var center = Vector3.zero;
        center[direction] = distance * 0.5f;
        capsule.center = center;
        capsule.height = Mathf.Abs(distance);
        capsule.radius = Mathf.Max(0.01f, Mathf.Abs(distance) * bone.RadiusScale * radiusScale);

        if (bone.IsHead)
            FitHeadCollider(capsule, bone.Anchor, radiusScale);

        return capsule;
    }

    static void FitHeadCollider(CapsuleCollider capsule, Transform head, float radiusScale)
    {
        var renderers = head.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        var world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            world.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = head.InverseTransformPoint(world.center);
        Vector3 localSize = head.InverseTransformVector(world.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        int dir = 1;
        if (localSize.x > localSize.y && localSize.x > localSize.z) dir = 0;
        else if (localSize.z > localSize.y) dir = 2;

        capsule.direction = dir;
        capsule.center = localCenter;
        capsule.height = Mathf.Max(localSize[dir], 0.05f);
        float radius = 0.5f * Mathf.Max(
            dir == 0 ? localSize.y : localSize.x,
            dir == 2 ? localSize.y : localSize.z);
        capsule.radius = Mathf.Max(0.02f, radius * 0.7f * radiusScale);
    }

    static void BuildBodies(List<BoneInfo> bones, RagdollBuildSettings settings, List<Rigidbody> bodies)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            var rb = bone.Anchor.GetComponent<Rigidbody>();
            if (rb == null)
                rb = Undo.AddComponent<Rigidbody>(bone.Anchor.gameObject);
            rb.mass = bone.Density;
            rb.linearDamping = settings.LinearDamping;
            rb.angularDamping = settings.AngularDamping;
            rb.useGravity = false;
            rb.isKinematic = true;
            bodies.Add(rb);
        }
    }

    static void BuildJoints(List<BoneInfo> bones, List<CharacterJoint> joints)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone.Parent == null)
                continue;

            var joint = Undo.AddComponent<CharacterJoint>(bone.Anchor.gameObject);
            joint.connectedBody = bone.Parent.Anchor.GetComponent<Rigidbody>();
            joint.anchor = Vector3.zero;
            joint.axis = CalculateDirectionAxis(bone.Anchor.InverseTransformDirection(bone.Axis));
            joint.swingAxis = CalculateDirectionAxis(bone.Anchor.InverseTransformDirection(bone.NormalAxis));
            joint.enablePreprocessing = false;
            joint.enableProjection = true;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle = 20f;

            var limit = new SoftJointLimit();
            limit.limit = bone.MinLimit;
            joint.lowTwistLimit = limit;
            limit.limit = bone.MaxLimit;
            joint.highTwistLimit = limit;
            limit.limit = bone.SwingLimit;
            joint.swing1Limit = limit;
            limit.limit = 0f;
            joint.swing2Limit = limit;
            joints.Add(joint);
        }
    }

    static void CalculateMass(BoneInfo rootBone, List<BoneInfo> bones, float totalMass)
    {
        SumMass(rootBone);
        if (rootBone.SummedMass <= 0.0001f)
            return;
        float scale = totalMass / rootBone.SummedMass;
        for (int i = 0; i < bones.Count; i++)
        {
            var rb = bones[i].Anchor.GetComponent<Rigidbody>();
            if (rb != null)
                rb.mass = Mathf.Max(0.01f, rb.mass * scale);
        }
    }

    static void SumMass(BoneInfo bone)
    {
        float mass = bone.Anchor.GetComponent<Rigidbody>().mass;
        for (int i = 0; i < bone.Children.Count; i++)
        {
            SumMass(bone.Children[i]);
            mass += bone.Children[i].SummedMass;
        }
        bone.SummedMass = mass;
    }

    static void CalculateDirection(Vector3 point, out int direction, out float distance)
    {
        direction = 0;
        if (Mathf.Abs(point.y) > Mathf.Abs(point.x))
            direction = 1;
        if (Mathf.Abs(point.z) > Mathf.Abs(point[direction]))
            direction = 2;
        distance = point[direction];
    }

    static Vector3 CalculateDirectionAxis(Vector3 point)
    {
        int direction;
        float distance;
        CalculateDirection(point, out direction, out distance);
        var axis = Vector3.zero;
        axis[direction] = distance >= 0f ? 1f : -1f;
        return axis;
    }
}
#endif
