using System;
using ControlFreak2;
using UnityEngine;

/// <summary>
/// Crouch toggle. Shrinks the capsule and lowers the camera, slows the player, turns sprint off and
/// makes them harder for the Nanny to see and hear (see NannyStateManager's Stealth settings).
/// The FirstPersonController's own crouch scales the whole player, which would squash whatever is
/// held (the baby included), so it stays off and this replaces it.
/// </summary>
[RequireComponent(typeof(FirstPersonController), typeof(CapsuleCollider))]
public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] float crouchHeight = 0.9f;
    [Tooltip("How far the camera drops when crouched.")]
    [SerializeField] float cameraDrop = 0.55f;
    [SerializeField, Range(0.1f, 1f)] float speedMultiplier = 0.55f;
    [Tooltip("Metres per second the camera moves between standing and crouched height.")]
    [SerializeField] float cameraSpeed = 3f;
    [SerializeField] KeyCode toggleKey = KeyCode.C;

    public bool IsCrouched { get; private set; }
    public event Action<bool> Changed;

    FirstPersonController _fpc;
    CapsuleCollider _capsule;
    Transform _camera;
    float _standHeight;
    Vector3 _standCenter;
    float _cameraStandY;
    float _standWalkSpeed;
    bool _sprintWasEnabled;

    void Awake()
    {
        _fpc = GetComponent<FirstPersonController>();
        _capsule = GetComponent<CapsuleCollider>();
        _camera = _fpc.playerCamera.transform;
        _standHeight = _capsule.height;
        _standCenter = _capsule.center;
        _cameraStandY = _camera.localPosition.y;
    }

    void Update()
    {
        if (CF2Input.GetKeyDown(toggleKey))
            Toggle();

        float targetY = IsCrouched ? _cameraStandY - cameraDrop : _cameraStandY;
        Vector3 local = _camera.localPosition;
        if (!Mathf.Approximately(local.y, targetY))
        {
            local.y = Mathf.MoveTowards(local.y, targetY, cameraSpeed * Time.deltaTime);
            _camera.localPosition = local;
        }
    }

    public void Toggle() => SetCrouched(!IsCrouched);

    /// <returns>False if the change couldn't happen, e.g. no headroom to stand up.</returns>
    public bool SetCrouched(bool crouched)
    {
        if (crouched == IsCrouched)
            return true;

        if (crouched && !_fpc.playerCanMove)
            return false;

        if (!crouched && !HasHeadroom())
            return false;

        Apply(crouched);
        return true;
    }

    /// <summary>Stands up immediately, skipping the headroom check and camera blend, e.g. before a teleport.</summary>
    public void ForceStand()
    {
        if (IsCrouched)
            Apply(false);

        Vector3 local = _camera.localPosition;
        local.y = _cameraStandY;
        _camera.localPosition = local;
    }

    void Apply(bool crouched)
    {
        IsCrouched = crouched;

        if (crouched)
        {
            _standWalkSpeed = _fpc.walkSpeed;
            _sprintWasEnabled = _fpc.enableSprint;
            _fpc.walkSpeed = _standWalkSpeed * speedMultiplier;
            _fpc.enableSprint = false;

            // Keep the feet where they are: shrink from the top.
            _capsule.height = crouchHeight;
            _capsule.center = _standCenter + Vector3.down * ((_standHeight - crouchHeight) * 0.5f);
        }
        else
        {
            _fpc.walkSpeed = _standWalkSpeed;
            _fpc.enableSprint = _sprintWasEnabled;
            _capsule.height = _standHeight;
            _capsule.center = _standCenter;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.SetCrouchState(crouched);
        Changed?.Invoke(crouched);
    }

    bool HasHeadroom()
    {
        float radius = _capsule.radius * 0.9f;
        Vector3 top = transform.TransformPoint(_capsule.center + Vector3.up * (_capsule.height * 0.5f - _capsule.radius));
        float distance = _standHeight - crouchHeight;

        RaycastHit[] hits = Physics.SphereCastAll(top, radius, Vector3.up, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root != transform.root)
                return false;
        }

        return true;
    }
}
