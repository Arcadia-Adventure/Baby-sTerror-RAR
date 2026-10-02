using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;

/// <summary>
/// Runtime ragdoll. Hold() carries the body from its torso with muscle-driven
/// limbs (Baby in Yellow style); EnableRagdoll() is a full limp drop.
/// </summary>
public class RagdollController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody rootBody;
    [SerializeField] Collider[] animatedColliders;
    [SerializeField] Rigidbody hipsBody;
    [SerializeField] Rigidbody holdBody;
    [SerializeField] Rigidbody[] ragdollBodies;
    [SerializeField] Collider[] ragdollColliders;
    [SerializeField] CharacterJoint[] ragdollJoints;
    [SerializeField] bool startAsRagdoll;
    [SerializeField] bool ignoreSelfCollisions = true;

    [Header("Held Muscles")]
    [Tooltip("How hard limbs pull back to the carried pose. Higher = stiffer, more alive.")]
    [SerializeField] float heldMuscleSpring = 30f;
    [Tooltip("Slows limb swing. Higher = heavier, less jiggly.")]
    [SerializeField] float heldMuscleDamper = 4f;
    [Tooltip("Gravity felt by limbs while carried. Below 1 stops limbs hanging like dead weight.")]
    [SerializeField, Range(0f, 1f)] float heldGravityScale = 0.55f;
    [Tooltip("Idle squirming while carried. 0 = perfectly still.")]
    [SerializeField] float squirmStrength = 1.5f;
    [SerializeField] float squirmSpeed = 1.4f;

    [Header("Held Damping")]
    [SerializeField] float heldLinearDamping = 0.5f;
    [SerializeField] float heldAngularDamping = 1.5f;
    [SerializeField] float heldMaxAngularVelocity = 7f;

    [Header("Dropped Muscles")]
    [Tooltip("Small amount of tone after a drop so the body is not a dead noodle.")]
    [SerializeField] float droppedMuscleSpring = 3f;
    [SerializeField] float droppedMuscleDamper = 1f;

    [Header("Joint Stability")]
    [Tooltip("Drives bones by rotation only so bone lengths can never change. Stops any visible stretching.")]
    [SerializeField] bool preserveBoneLengths = true;
    [Tooltip("How far a joint may pull apart before the solver snaps it back. Keep well under the shortest bone.")]
    [SerializeField] float jointProjectionDistance = 0.01f;
    [SerializeField] float jointProjectionAngle = 5f;
    [SerializeField] int ragdollSolverIterations = 20;
    [SerializeField] int ragdollSolverVelocityIterations = 10;
    [Tooltip("Caps how hard overlapping colliders shove each other apart.")]
    [SerializeField] float maxDepenetrationVelocity = 1f;
    [Tooltip("Let the limbs collide with the torso and head instead of passing through them.")]
    [SerializeField] bool collideOwnBody = true;
    [Tooltip("Smooths limbs between physics steps. The Animator wipes Unity's own interpolation, so it is done here.")]
    [SerializeField] bool smoothLimbs = true;

    [Header("Limb Ragdoll")]
    [Tooltip("Arms and legs that go physical while the torso, head and face keep animating. Auto-filled from bone names when empty.")]
    [SerializeField] Rigidbody[] limbBodies;
    [Tooltip("Bone name fragments treated as arms and legs.")]
    [SerializeField] string[] limbNameTokens =
        { "upperarm", "forearm", "hand", "thigh", "calf", "shin", "foot", "toe", "arm", "leg" };

    public bool IsRagdoll { get; private set; }
    public bool IsHeld { get; private set; }
    public bool IsLimbRagdoll { get; private set; }

    public IReadOnlyList<Rigidbody> Bodies => ragdollBodies;
    public IReadOnlyList<CharacterJoint> Joints => ragdollJoints;
    public Rigidbody Hips => hipsBody != null ? hipsBody : (ragdollBodies != null && ragdollBodies.Length > 0 ? ragdollBodies[0] : null);
    public Rigidbody Pin => holdBody != null ? holdBody : Hips;

    bool _rootWasKinematic = true;
    bool _rootDetectedCollisions = true;
    bool _cachedRootState;
    Vector3 _animatedHipsLocalPos;
    bool _hasAnimatedHipsPose;

    Transform _holdPoint;
    Vector3 _holdRootLocalPos;
    Quaternion _holdRootLocalRot = Quaternion.identity;
    Vector3 _pinLocalPos;
    Quaternion _pinLocalRot = Quaternion.identity;

    Rigidbody[] _muscleBodies;
    Rigidbody[] _muscleParents;
    Quaternion[] _muscleRest;
    float _muscleSpring;
    float _muscleDamper;
    float _squirmSeed;

    Collider[] _ignoredPlayerColliders;
    float[] _originalLinearDamping;
    float[] _originalAngularDamping;
    int[] _originalLayers;
    int[] _originalLimbLayers;
    Quaternion[] _limbPrevRot;
    Quaternion[] _limbCurrRot;
    Vector3[] _limbPrevPos;
    Vector3[] _limbCurrPos;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (rootBody == null)
            rootBody = GetComponent<Rigidbody>();
        if (holdBody == null)
            holdBody = FindNamedBody("spine") ?? Hips;

        _squirmSeed = Random.value * 100f;
        BuildMuscles();
        ResolveLimbs();
        StabilizeJoints();
        CacheRootBodyState();
        ApplySelfCollisionIgnore();
        SetRagdoll(startAsRagdoll, Vector3.zero, ForceMode.Impulse, false);
    }

    #region Setup

    Rigidbody FindNamedBody(string namePart)
    {
        if (ragdollBodies == null)
            return null;
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body != null && body.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return body;
        }
        return null;
    }

    void BuildMuscles()
    {
        var bodies = new List<Rigidbody>();
        var parents = new List<Rigidbody>();

        if (ragdollJoints != null)
        {
            for (int i = 0; i < ragdollJoints.Length; i++)
            {
                var joint = ragdollJoints[i];
                if (joint == null || joint.connectedBody == null)
                    continue;
                var body = joint.GetComponent<Rigidbody>();
                if (body == null)
                    continue;
                bodies.Add(body);
                parents.Add(joint.connectedBody);
            }
        }

        _muscleBodies = bodies.ToArray();
        _muscleParents = parents.ToArray();
        _muscleRest = new Quaternion[_muscleBodies.Length];
        for (int i = 0; i < _muscleRest.Length; i++)
            _muscleRest[i] = Quaternion.identity;
    }

    void ResolveLimbs()
    {
        if (limbBodies != null && limbBodies.Length > 0)
        {
            SortByHierarchyDepth(limbBodies);
            return;
        }
        if (ragdollBodies == null)
        {
            limbBodies = System.Array.Empty<Rigidbody>();
            return;
        }

        var found = new List<Rigidbody>();
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body != null && IsLimbName(body.name))
                found.Add(body);
        }

        limbBodies = found.ToArray();
        SortByHierarchyDepth(limbBodies);
    }

    bool IsLimbName(string boneName)
    {
        if (string.IsNullOrEmpty(boneName) || limbNameTokens == null)
            return false;
        string lower = boneName.ToLowerInvariant().Replace(" ", string.Empty);
        for (int i = 0; i < limbNameTokens.Length; i++)
        {
            if (!string.IsNullOrEmpty(limbNameTokens[i]) && lower.Contains(limbNameTokens[i]))
                return true;
        }
        return false;
    }

    /// <summary>Parents must be written before children when copying physics poses back.</summary>
    static void SortByHierarchyDepth(Rigidbody[] bodies)
    {
        System.Array.Sort(bodies, (a, b) => Depth(a).CompareTo(Depth(b)));

        static int Depth(Rigidbody body)
        {
            if (body == null) return int.MaxValue;
            int depth = 0;
            var t = body.transform;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }
    }

    /// <summary>
    /// Tightens joint projection and solver accuracy so limbs cannot pull apart.
    /// Applied at runtime, so ragdolls built with looser settings are fixed too.
    /// </summary>
    void StabilizeJoints()
    {
        if (ragdollJoints == null)
            return;

        for (int i = 0; i < ragdollJoints.Length; i++)
        {
            var joint = ragdollJoints[i];
            if (joint == null) continue;
            joint.enableProjection = true;
            joint.projectionDistance = jointProjectionDistance;
            joint.projectionAngle = jointProjectionAngle;
            joint.enablePreprocessing = true;
        }
    }

    void ApplySolverAccuracy(Rigidbody body)
    {
        body.solverIterations = ragdollSolverIterations;
        body.solverVelocityIterations = ragdollSolverVelocityIterations;
        body.maxDepenetrationVelocity = maxDepenetrationVelocity;
    }

    void ApplySelfCollisionIgnore()
    {
        if (ragdollColliders == null)
            return;

        // The ragdoll skeleton must never fight the character's own gameplay collider.
        if (animatedColliders != null)
        {
            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                if (ragdollColliders[i] == null) continue;
                for (int j = 0; j < animatedColliders.Length; j++)
                {
                    if (animatedColliders[j] != null)
                        Physics.IgnoreCollision(ragdollColliders[i], animatedColliders[j], true);
                }
            }
        }

        // Neighbours always overlap in the bind pose, so they must never collide.
        IgnoreJointNeighbours();

        if (collideOwnBody || !ignoreSelfCollisions)
            return;

        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            for (int j = i + 1; j < ragdollColliders.Length; j++)
            {
                if (ragdollColliders[i] != null && ragdollColliders[j] != null)
                    Physics.IgnoreCollision(ragdollColliders[i], ragdollColliders[j], true);
            }
        }
    }

    /// <summary>
    /// Only bones that sit next to each other in the chain overlap in the bind pose,
    /// so those are the only pairs worth ignoring. Every other pair stays solid,
    /// which is what stops a hand from sinking into the belly.
    /// </summary>
    void IgnoreJointNeighbours()
    {
        if (ragdollJoints == null)
            return;

        for (int i = 0; i < ragdollJoints.Length; i++)
        {
            var joint = ragdollJoints[i];
            if (joint == null || joint.connectedBody == null) continue;

            var bone = joint.GetComponent<Collider>();
            var parent = joint.connectedBody.GetComponent<Collider>();
            if (bone == null) continue;

            if (parent != null)
                Physics.IgnoreCollision(bone, parent, true);

            // A short bone can also reach past its parent into its grandparent.
            var parentJoint = joint.connectedBody.GetComponent<CharacterJoint>();
            if (parentJoint != null && parentJoint.connectedBody != null)
            {
                var grandParent = parentJoint.connectedBody.GetComponent<Collider>();
                if (grandParent != null)
                    Physics.IgnoreCollision(bone, grandParent, true);
            }
        }
    }

    void CacheRootBodyState()
    {
        if (_cachedRootState || rootBody == null)
            return;
        _rootWasKinematic = rootBody.isKinematic;
        _rootDetectedCollisions = rootBody.detectCollisions;
        _cachedRootState = true;
    }

    void CacheAnimatedHipsPose()
    {
        var hips = Hips;
        if (hips == null)
            return;
        _animatedHipsLocalPos = transform.InverseTransformPoint(hips.transform.position);
        _hasAnimatedHipsPose = true;
    }

    /// <summary>Records the animated pose the muscles pull back toward.</summary>
    void CaptureMusclePose()
    {
        if (_muscleBodies == null)
            return;
        for (int i = 0; i < _muscleBodies.Length; i++)
        {
            var body = _muscleBodies[i];
            var parent = _muscleParents[i];
            if (body == null || parent == null) continue;
            _muscleRest[i] = Quaternion.Inverse(parent.transform.rotation) * body.transform.rotation;
        }
    }

    #endregion

    #region Limb ragdoll

    /// <summary>
    /// Makes only the arms and legs physical. The torso, neck, head and face keep
    /// playing their animation, so the baby still looks and emotes as normal while
    /// its limbs dangle and react.
    /// </summary>
    public void EnableLimbRagdoll(Transform carrier = null)
    {
        if (IsHeld)
            Release(Vector3.zero);
        if (IsRagdoll)
            DisableRagdoll(false);
        if (limbBodies == null || limbBodies.Length == 0)
            return;

        if (animator != null)
            animator.enabled = true;

        Physics.SyncTransforms();
        StabilizeJoints();
        CaptureMusclePose();
        SetMuscleTone(heldMuscleSpring, heldMuscleDamper);

        int heldLayer = LayerMask.NameToLayer("HeldItem");
        if (_originalLimbLayers == null || _originalLimbLayers.Length != limbBodies.Length)
            _originalLimbLayers = new int[limbBodies.Length];

        for (int i = 0; i < limbBodies.Length; i++)
        {
            var body = limbBodies[i];
            if (body == null) continue;

            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // Poses are written by hand in LateUpdate, so interpolation would fight it.
            body.interpolation = RigidbodyInterpolation.None;
            body.linearDamping = heldLinearDamping;
            body.angularDamping = heldAngularDamping;
            body.maxAngularVelocity = heldMaxAngularVelocity;
            body.sleepThreshold = 0f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            ApplySolverAccuracy(body);

            _originalLimbLayers[i] = body.gameObject.layer;
            if (heldLayer >= 0 && carrier != null)
                body.gameObject.layer = heldLayer;

            var col = body.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;
        }

        EnableBodyColliders(true);
        ApplyLimbCollisionIgnores(carrier);
        ResetLimbInterpolation();
        IsLimbRagdoll = true;
    }

    /// <summary>
    /// Turns the torso, neck and head colliders on while they stay kinematic and
    /// animated, so the dangling limbs have a solid body to rest against.
    /// </summary>
    void EnableBodyColliders(bool enable)
    {
        if (!collideOwnBody || ragdollBodies == null)
            return;

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body == null || IsLimb(body)) continue;

            body.detectCollisions = enable;
            var col = body.GetComponent<Collider>();
            if (col != null)
                col.enabled = enable;
        }
    }

    bool IsLimb(Rigidbody body)
    {
        if (limbBodies == null) return false;
        for (int i = 0; i < limbBodies.Length; i++)
            if (limbBodies[i] == body) return true;
        return false;
    }

    public void DisableLimbRagdoll()
    {
        if (!IsLimbRagdoll)
            return;

        IgnorePlayerCollisions(null, false);

        for (int i = 0; i < limbBodies.Length; i++)
        {
            var body = limbBodies[i];
            if (body == null) continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
            body.sleepThreshold = 0.005f;

            if (_originalLimbLayers != null && i < _originalLimbLayers.Length)
                body.gameObject.layer = _originalLimbLayers[i];

            var col = body.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }

        EnableBodyColliders(false);
        IsLimbRagdoll = false;
    }

    void ResetLimbInterpolation()
    {
        int count = limbBodies != null ? limbBodies.Length : 0;
        if (_limbPrevRot == null || _limbPrevRot.Length != count)
        {
            _limbPrevRot = new Quaternion[count];
            _limbCurrRot = new Quaternion[count];
            _limbPrevPos = new Vector3[count];
            _limbCurrPos = new Vector3[count];
        }

        for (int i = 0; i < count; i++)
        {
            var body = limbBodies[i];
            if (body == null) continue;
            _limbPrevRot[i] = _limbCurrRot[i] = body.rotation;
            _limbPrevPos[i] = _limbCurrPos[i] = body.position;
        }
    }

    /// <summary>Keeps the last two simulated poses so rendering can blend between them.</summary>
    void RecordLimbPoses()
    {
        if (_limbCurrRot == null || _limbCurrRot.Length != limbBodies.Length)
            ResetLimbInterpolation();

        for (int i = 0; i < limbBodies.Length; i++)
        {
            var body = limbBodies[i];
            if (body == null) continue;
            _limbPrevRot[i] = _limbCurrRot[i];
            _limbPrevPos[i] = _limbCurrPos[i];
            _limbCurrRot[i] = body.rotation;
            _limbCurrPos[i] = body.position;
        }
    }

    void ApplyLimbCollisionIgnores(Transform carrier)
    {
        // Enabling a collider can clear previously ignored pairs, so re-apply them.
        ApplySelfCollisionIgnore();

        if (carrier != null)
            IgnorePlayerCollisions(carrier, true);
    }

    /// <summary>
    /// Runs after the Animator has posed the skeleton: the freshly animated pose
    /// becomes the muscle target, then the simulated pose is written over the limbs.
    /// </summary>
    void LateUpdate()
    {
        if (!IsLimbRagdoll || limbBodies == null)
            return;

        CaptureMusclePose();

        // Physics only ticks at the fixed rate, so blend across the last two steps
        // the way Unity's own interpolation would if the Animator did not wipe it.
        float alpha = smoothLimbs && Time.fixedDeltaTime > 0f
            ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime)
            : 1f;

        for (int i = 0; i < limbBodies.Length; i++)
        {
            var body = limbBodies[i];
            if (body == null || body.isKinematic) continue;

            Quaternion rot = smoothLimbs && _limbCurrRot != null && i < _limbCurrRot.Length
                ? Quaternion.Slerp(_limbPrevRot[i], _limbCurrRot[i], alpha)
                : body.rotation;

            if (preserveBoneLengths)
            {
                // Rotation only: each bone keeps its bind-pose offset from its parent,
                // so the skinned mesh can never stretch no matter what the solver does.
                body.transform.rotation = rot;
            }
            else
            {
                Vector3 pos = smoothLimbs && _limbCurrPos != null && i < _limbCurrPos.Length
                    ? Vector3.Lerp(_limbPrevPos[i], _limbCurrPos[i], alpha)
                    : body.position;
                body.transform.SetPositionAndRotation(pos, rot);
            }
        }
    }

    #endregion

    #region Carry

    /// <summary>
    /// Carries the body from its torso. The torso tracks the hold point exactly,
    /// limbs stay physical. Do not parent the character to the player.
    /// </summary>
    public void Hold(Transform holdPoint, Vector3 localPosition, Quaternion localRotation)
    {
        var pin = Pin;
        if (holdPoint == null || pin == null)
        {
            Debug.LogWarning("RagdollController cannot hold: missing hold point or torso body.", this);
            return;
        }

        if (IsLimbRagdoll)
            DisableLimbRagdoll();

        transform.SetParent(null, true);
        Physics.SyncTransforms();
        CacheAnimatedHipsPose();
        CaptureMusclePose();

        _pinLocalPos = transform.InverseTransformPoint(pin.transform.position);
        _pinLocalRot = Quaternion.Inverse(transform.rotation) * pin.transform.rotation;
        _holdPoint = holdPoint;
        _holdRootLocalPos = localPosition;
        _holdRootLocalRot = localRotation;

        SetRagdoll(true, Vector3.zero, ForceMode.Impulse, false);

        Quaternion rootRot = holdPoint.rotation * localRotation;
        Vector3 rootPos = holdPoint.TransformPoint(localPosition);
        transform.SetPositionAndRotation(rootPos, rootRot);
        TeleportRagdoll(pin, rootPos + rootRot * _pinLocalPos, rootRot * _pinLocalRot);

        // The torso is driven directly instead of hanging off a spring, so it
        // never lags behind the hands.
        pin.isKinematic = true;
        pin.useGravity = false;
        pin.detectCollisions = false;
        var pinCollider = pin.GetComponent<Collider>();
        if (pinCollider != null)
            pinCollider.enabled = false;

        SetMuscleTone(heldMuscleSpring, heldMuscleDamper);
        ApplyHeldDamping(true);
        SetRagdollLayersHeld(true);
        IgnorePlayerCollisions(holdPoint, true);

        IsHeld = true;
    }

    /// <summary>Retunes the carry offset while held, so it can be dialled in during Play Mode.</summary>
    public void SetHoldOffset(Vector3 localPosition, Quaternion localRotation)
    {
        _holdRootLocalPos = localPosition;
        _holdRootLocalRot = localRotation;
    }

    public void Release(Vector3 throwForce)
    {
        if (!IsRagdoll && !IsHeld)
            return;

        var pin = Pin;
        _holdPoint = null;
        IsHeld = false;

        ApplyHeldDamping(false);
        SetRagdollLayersHeld(false);
        IgnorePlayerCollisions(null, false);
        SetMuscleTone(droppedMuscleSpring, droppedMuscleDamper);

        if (pin != null)
        {
            pin.isKinematic = false;
            pin.useGravity = true;
            pin.detectCollisions = true;
            var pinCollider = pin.GetComponent<Collider>();
            if (pinCollider != null)
                pinCollider.enabled = true;
        }

        if (!IsRagdoll)
            SetRagdoll(true, Vector3.zero, ForceMode.Impulse, false);

        if (pin != null && throwForce.sqrMagnitude > 0.0001f)
            pin.AddForce(throwForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (IsHeld)
        {
            if (_holdPoint == null)
            {
                Release(Vector3.zero);
                return;
            }
            DriveHeldTorso();
        }

        if (IsRagdoll || IsLimbRagdoll)
            ApplyMuscles();

        if (IsLimbRagdoll && smoothLimbs)
            RecordLimbPoses();
    }

    void DriveHeldTorso()
    {
        var pin = Pin;
        if (pin == null)
            return;

        Quaternion rootRot = _holdPoint.rotation * _holdRootLocalRot;
        Vector3 rootPos = _holdPoint.TransformPoint(_holdRootLocalPos);

        // The bones are children of this transform, so moving it here would drag the
        // whole ragdoll rigidly at the next physics sync. Only the torso is driven.
        pin.MovePosition(rootPos + rootRot * _pinLocalPos);
        pin.MoveRotation(rootRot * _pinLocalRot);
    }

    void SetMuscleTone(float spring, float damper)
    {
        _muscleSpring = Mathf.Max(0f, spring);
        _muscleDamper = Mathf.Max(0f, damper);
    }

    /// <summary>
    /// Pulls each bone back toward the pose it had when the carry started, so limbs
    /// swing and settle instead of hanging dead.
    /// </summary>
    void ApplyMuscles()
    {
        if (_muscleBodies == null || _muscleSpring <= 0f)
            return;

        bool carried = IsHeld || IsLimbRagdoll;
        float squirm = carried ? squirmStrength : 0f;
        float time = Time.time * squirmSpeed + _squirmSeed;
        float antiGravity = carried ? 1f - heldGravityScale : 0f;

        for (int i = 0; i < _muscleBodies.Length; i++)
        {
            var body = _muscleBodies[i];
            var parent = _muscleParents[i];
            if (body == null || parent == null || body.isKinematic)
                continue;

            Quaternion current = Quaternion.Inverse(parent.rotation) * body.rotation;
            Quaternion delta = _muscleRest[i] * Quaternion.Inverse(current);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (float.IsInfinity(axis.x) || float.IsNaN(axis.x))
                continue;
            if (angle > 180f)
                angle -= 360f;

            Vector3 worldAxis = parent.rotation * axis.normalized;
            Vector3 torque = worldAxis * (angle * Mathf.Deg2Rad * _muscleSpring)
                             - body.angularVelocity * _muscleDamper;

            if (squirm > 0f)
            {
                float phase = time + i * 1.7f;
                torque += new Vector3(
                    Mathf.Sin(phase),
                    Mathf.Sin(phase * 1.3f),
                    Mathf.Cos(phase * 0.8f)) * squirm;
            }

            body.AddTorque(torque, ForceMode.Acceleration);

            if (antiGravity > 0f)
                body.AddForce(-Physics.gravity * antiGravity, ForceMode.Acceleration);
        }
    }

    #endregion

    #region Full ragdoll

    [InspectorButton("Enable Ragdoll")]
    public void EnableRagdoll() => EnableRagdoll(Vector3.zero);

    public void EnableRagdoll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (IsHeld)
            Release(Vector3.zero);
        if (IsLimbRagdoll)
            DisableLimbRagdoll();

        Physics.SyncTransforms();
        CacheAnimatedHipsPose();
        CaptureMusclePose();
        SetMuscleTone(droppedMuscleSpring, droppedMuscleDamper);
        SetRagdoll(true, force, mode, true);
    }

    public void EnableRagdoll(Vector3 force, Vector3 forcePoint, ForceMode mode = ForceMode.Impulse)
    {
        EnableRagdoll(Vector3.zero, mode);
        var target = ClosestBody(forcePoint);
        if (target != null)
            target.AddForceAtPosition(force, forcePoint, mode);
    }

    [InspectorButton("Disable Ragdoll")]
    public void DisableRagdoll() => DisableRagdoll(true);

    public void DisableRagdoll(bool alignRoot)
    {
        if (IsHeld)
            Release(Vector3.zero);

        Vector3 rootPos = transform.position;
        Quaternion rootRot = transform.rotation;
        bool applyAlign = false;

        var hips = Hips;
        if (alignRoot && hips != null)
        {
            rootRot = Quaternion.Euler(0f, hips.rotation.eulerAngles.y, 0f);
            rootPos = hips.position;
            if (_hasAnimatedHipsPose)
                rootPos = hips.position - rootRot * _animatedHipsLocalPos;
            applyAlign = true;
        }

        SetRagdoll(false, Vector3.zero, ForceMode.Impulse, false);

        if (applyAlign)
            transform.SetPositionAndRotation(rootPos, rootRot);
    }

    public void AddForceToAll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (ragdollBodies == null) return;
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            if (ragdollBodies[i] != null && !ragdollBodies[i].isKinematic)
                ragdollBodies[i].AddForce(force, mode);
        }
    }

    public void AddExplosionForce(float force, Vector3 origin, float radius, float upwardsModifier = 0.4f)
    {
        if (ragdollBodies == null) return;
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body != null && !body.isKinematic)
                body.AddExplosionForce(force, origin, radius, upwardsModifier, ForceMode.Impulse);
        }
    }

    Rigidbody ClosestBody(Vector3 worldPoint)
    {
        Rigidbody closest = Hips;
        float best = float.MaxValue;
        if (ragdollBodies == null) return closest;

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body == null) continue;
            float d = (body.worldCenterOfMass - worldPoint).sqrMagnitude;
            if (d < best)
            {
                best = d;
                closest = body;
            }
        }
        return closest;
    }

    void TeleportRagdoll(Rigidbody pin, Vector3 targetPos, Quaternion targetRot)
    {
        if (ragdollBodies == null)
            return;

        Vector3 origin = pin.position;
        Quaternion rotDelta = targetRot * Quaternion.Inverse(pin.rotation);

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body == null) continue;
            body.position = targetPos + rotDelta * (body.position - origin);
            body.rotation = rotDelta * body.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
    }

    #endregion

    #region State helpers

    void ApplyHeldDamping(bool held)
    {
        if (ragdollBodies == null)
            return;

        if (held && _originalLinearDamping == null)
        {
            _originalLinearDamping = new float[ragdollBodies.Length];
            _originalAngularDamping = new float[ragdollBodies.Length];
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body == null) continue;
                _originalLinearDamping[i] = body.linearDamping;
                _originalAngularDamping[i] = body.angularDamping;
            }
        }

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body == null) continue;

            if (held)
            {
                body.linearDamping = heldLinearDamping;
                body.angularDamping = heldAngularDamping;
                body.maxAngularVelocity = heldMaxAngularVelocity;
                body.sleepThreshold = 0f;
            }
            else if (_originalLinearDamping != null && i < _originalLinearDamping.Length)
            {
                body.linearDamping = _originalLinearDamping[i];
                body.angularDamping = _originalAngularDamping[i];
                body.sleepThreshold = 0.005f;
            }
        }
    }

    void SetRagdollLayersHeld(bool held)
    {
        if (ragdollColliders == null)
            return;

        if (held)
        {
            int heldLayer = LayerMask.NameToLayer("HeldItem");
            if (_originalLayers == null || _originalLayers.Length != ragdollColliders.Length)
                _originalLayers = new int[ragdollColliders.Length];
            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                if (ragdollColliders[i] == null) continue;
                _originalLayers[i] = ragdollColliders[i].gameObject.layer;
                if (heldLayer >= 0)
                    ragdollColliders[i].gameObject.layer = heldLayer;
            }
        }
        else if (_originalLayers != null)
        {
            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                if (ragdollColliders[i] == null) continue;
                ragdollColliders[i].gameObject.layer = _originalLayers[i];
            }
        }
    }

    void IgnorePlayerCollisions(Transform holdPoint, bool ignore)
    {
        if (ragdollColliders == null)
            return;

        if (ignore)
            _ignoredPlayerColliders = CollectPlayerColliders(holdPoint);

        if (_ignoredPlayerColliders == null)
            return;

        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            var ragCol = ragdollColliders[i];
            if (ragCol == null) continue;
            for (int j = 0; j < _ignoredPlayerColliders.Length; j++)
            {
                var playerCol = _ignoredPlayerColliders[j];
                if (playerCol != null)
                    Physics.IgnoreCollision(ragCol, playerCol, ignore);
            }
        }

        if (!ignore)
            _ignoredPlayerColliders = null;
    }

    static Collider[] CollectPlayerColliders(Transform holdPoint)
    {
        if (holdPoint == null)
            return System.Array.Empty<Collider>();

        var fpc = holdPoint.GetComponentInParent<FirstPersonController>();
        if (fpc != null)
            return fpc.GetComponentsInChildren<Collider>(true);

        var root = holdPoint.root;
        return root != null ? root.GetComponentsInChildren<Collider>(true) : holdPoint.GetComponentsInParent<Collider>(true);
    }

    void SetRagdoll(bool ragdollOn, Vector3 force, ForceMode mode, bool applyForce)
    {
        Vector3 inheritedVelocity = Vector3.zero;
        Vector3 inheritedAngular = Vector3.zero;
        if (ragdollOn && rootBody != null && !rootBody.isKinematic)
        {
            inheritedVelocity = rootBody.linearVelocity;
            inheritedAngular = rootBody.angularVelocity;
        }

        IsRagdoll = ragdollOn;

        if (animator != null)
            animator.enabled = !ragdollOn;

        if (animatedColliders != null)
        {
            for (int i = 0; i < animatedColliders.Length; i++)
            {
                if (animatedColliders[i] != null)
                    animatedColliders[i].enabled = !ragdollOn;
            }
        }

        if (rootBody != null)
        {
            CacheRootBodyState();
            if (ragdollOn)
            {
                rootBody.linearVelocity = Vector3.zero;
                rootBody.angularVelocity = Vector3.zero;
                rootBody.isKinematic = true;
                rootBody.detectCollisions = false;
            }
            else
            {
                rootBody.isKinematic = _rootWasKinematic;
                rootBody.detectCollisions = _rootDetectedCollisions;
            }
        }

        if (ragdollColliders != null)
        {
            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                if (ragdollColliders[i] != null)
                    ragdollColliders[i].enabled = ragdollOn;
            }
        }

        if (ragdollBodies != null)
        {
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body == null) continue;

                body.detectCollisions = ragdollOn;
                body.isKinematic = !ragdollOn;
                body.useGravity = ragdollOn;
                body.collisionDetectionMode = ragdollOn
                    ? CollisionDetectionMode.Continuous
                    : CollisionDetectionMode.ContinuousSpeculative;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                if (ragdollOn)
                    ApplySolverAccuracy(body);

                if (!ragdollOn)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                else if (inheritedVelocity.sqrMagnitude > 0.0001f || inheritedAngular.sqrMagnitude > 0.0001f)
                {
                    body.linearVelocity = inheritedVelocity;
                    body.angularVelocity = inheritedAngular;
                }
            }
        }

        if (ragdollOn && applyForce && force.sqrMagnitude > 0.0001f)
        {
            var hips = Hips;
            if (hips != null && !hips.isKinematic)
                hips.AddForce(force, mode);
        }
    }

    #endregion

#if UNITY_EDITOR
    public void EditorAssign(
        Animator assignedAnimator,
        Rigidbody assignedRootBody,
        Collider[] assignedAnimatedColliders,
        Rigidbody assignedHips,
        Rigidbody[] bodies,
        Collider[] colliders,
        CharacterJoint[] joints)
    {
        animator = assignedAnimator;
        rootBody = assignedRootBody;
        animatedColliders = assignedAnimatedColliders;
        hipsBody = assignedHips;
        ragdollBodies = bodies;
        ragdollColliders = colliders;
        ragdollJoints = joints;
        holdBody = null;
        if (bodies != null)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null && bodies[i].name.IndexOf("spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    holdBody = bodies[i];
                    break;
                }
            }
        }
        if (holdBody == null)
            holdBody = assignedHips;
    }

    public void EditorCollectCreatedParts(List<Object> parts)
    {
        if (ragdollJoints != null)
        {
            for (int i = 0; i < ragdollJoints.Length; i++)
                if (ragdollJoints[i] != null) parts.Add(ragdollJoints[i]);
        }
        if (ragdollColliders != null)
        {
            for (int i = 0; i < ragdollColliders.Length; i++)
                if (ragdollColliders[i] != null) parts.Add(ragdollColliders[i]);
        }
        if (ragdollBodies != null)
        {
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body != null && body != rootBody)
                    parts.Add(body);
            }
        }
    }
#endif
}
