using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;

/// <summary>
/// Runtime control for a ragdoll built by the Ragdoll Creator.
/// Bone rigidbodies stay kinematic while the Animator is in control.
/// </summary>
public class RagdollController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody rootBody;
    [SerializeField] Collider[] animatedColliders;
    [SerializeField] Rigidbody hipsBody;
    [SerializeField] Rigidbody[] ragdollBodies;
    [SerializeField] Collider[] ragdollColliders;
    [SerializeField] CharacterJoint[] ragdollJoints;
    [SerializeField] bool startAsRagdoll;
    [SerializeField] bool ignoreSelfCollisions = true;

    public bool IsRagdoll { get; private set; }

    public IReadOnlyList<Rigidbody> Bodies => ragdollBodies;
    public IReadOnlyList<CharacterJoint> Joints => ragdollJoints;
    public Rigidbody Hips => hipsBody != null ? hipsBody : (ragdollBodies != null && ragdollBodies.Length > 0 ? ragdollBodies[0] : null);

    bool _rootWasKinematic = true;
    bool _rootDetectedCollisions = true;
    bool _cachedRootState;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (rootBody == null)
            rootBody = GetComponent<Rigidbody>();

        CacheRootBodyState();
        ApplySelfCollisionIgnore();
        SetRagdoll(startAsRagdoll, Vector3.zero, ForceMode.Impulse, false);
    }

    void ApplySelfCollisionIgnore()
    {
        if (!ignoreSelfCollisions || ragdollColliders == null)
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

    void CacheRootBodyState()
    {
        if (_cachedRootState || rootBody == null)
            return;
        _rootWasKinematic = rootBody.isKinematic;
        _rootDetectedCollisions = rootBody.detectCollisions;
        _cachedRootState = true;
    }

    [InspectorButton("Enable Ragdoll")]
    public void EnableRagdoll() => EnableRagdoll(Vector3.zero);

    public void EnableRagdoll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        SetRagdoll(true, force, mode, true);
    }

    public void EnableRagdoll(Vector3 force, Vector3 forcePoint, ForceMode mode = ForceMode.Impulse)
    {
        SetRagdoll(true, Vector3.zero, mode, true);
        var target = ClosestBody(forcePoint);
        if (target != null)
            target.AddForceAtPosition(force, forcePoint, mode);
    }

    [InspectorButton("Disable Ragdoll")]
    public void DisableRagdoll()
    {
        SetRagdoll(false, Vector3.zero, ForceMode.Impulse, true);
    }

    public void AddForceToAll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (ragdollBodies == null) return;
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            if (ragdollBodies[i] != null)
                ragdollBodies[i].AddForce(force, mode);
        }
    }

    public void AddExplosionForce(float force, Vector3 origin, float radius, float upwardsModifier = 0.4f)
    {
        if (ragdollBodies == null) return;
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            var body = ragdollBodies[i];
            if (body != null)
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
            if (hips != null)
                hips.AddForce(force, mode);
        }
    }

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
