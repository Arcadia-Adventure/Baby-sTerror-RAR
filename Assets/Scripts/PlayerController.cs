using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Rigidbody rb;

    [Header("Forced Look")]
    [Tooltip("Time to swing the camera onto a target, e.g. the Nanny during an attack.")]
    [SerializeField] private float lookDuration = 0.35f;

    Coroutine _lookRoutine;
    bool _lookActive;
    bool _cameraMoveBeforeLook;

    PlayerCrouch _crouch;
    PlayerFlashlight _flashlight;
    bool _moveBeforeHide;
    bool _cameraMoveBeforeHide;

    public Camera PlayerCamera => firstPersonController != null ? firstPersonController.playerCamera : null;
    public FirstPersonController Fpc => firstPersonController;
    public PlayerHealth Health => playerHealth;

    /// <summary>Added on demand, so the player prefab needs no new components.</summary>
    public PlayerCrouch Crouch => _crouch != null ? _crouch : _crouch = GetOrAdd<PlayerCrouch>();
    public PlayerFlashlight Flashlight => _flashlight != null ? _flashlight : _flashlight = GetOrAdd<PlayerFlashlight>();

    public bool IsCrouched => _crouch != null && _crouch.IsCrouched;
    public bool IsFlashlightOn => _flashlight != null && _flashlight.IsOn;

    /// <summary>The hiding spot the player is in, or null.</summary>
    public HideSpot CurrentHideSpot { get; private set; }
    public bool IsHidden => CurrentHideSpot != null;

    void Awake()
    {
        // Created up front so the crouch key works from the first frame.
        _crouch = GetOrAdd<PlayerCrouch>();
        _flashlight = GetOrAdd<PlayerFlashlight>();
    }

    T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    public void InitializeCameraPitch(float pitch) => firstPersonController.InitializePitch(pitch);

    /// <summary>Distance from the player's pivot down to their feet.</summary>
    public float FeetOffset
    {
        get
        {
            var capsule = GetComponent<CapsuleCollider>();
            return capsule != null ? capsule.height * 0.5f - capsule.center.y : 0.75f;
        }
    }

    /// <summary>Moves the player so the camera sits at the given point, looking along its forward.</summary>
    public void TeleportCameraTo(Transform view)
    {
        Transform cam = PlayerCamera.transform;
        float yaw = view.eulerAngles.y;
        float pitch = NormalizeAngle(view.eulerAngles.x);
        pitch = Mathf.Clamp(pitch, -firstPersonController.maxLookAngle, firstPersonController.maxLookAngle);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.localEulerAngles = new Vector3(pitch, 0f, 0f);
        firstPersonController.InitializePitch(pitch);

        Vector3 cameraOffset = cam.position - transform.position;
        transform.position = view.position - cameraOffset;
        if (rb != null)
            rb.position = transform.position;
    }

    /// <summary>Stands the player on a floor point, facing along its forward.</summary>
    public void TeleportFeetTo(Transform point)
    {
        Vector3 forward = Vector3.ProjectOnPlane(point.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized);

        transform.position = point.position + Vector3.up * (FeetOffset + 0.05f);
        if (rb != null)
            rb.position = transform.position;

        PlayerCamera.transform.localEulerAngles = Vector3.zero;
        firstPersonController.InitializePitch(0f);
    }

    public void EnterHide(HideSpot spot)
    {
        if (spot == null || IsHidden)
            return;

        Crouch.ForceStand();
        ReleaseForcedLook();

        CurrentHideSpot = spot;
        _moveBeforeHide = firstPersonController.playerCanMove;
        _cameraMoveBeforeHide = firstPersonController.cameraCanMove;

        firstPersonController.playerCanMove = false;
        firstPersonController.cameraCanMove = spot.allowLook;
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void ExitHide()
    {
        if (!IsHidden)
            return;

        CurrentHideSpot = null;
        if (playerHealth != null && playerHealth.IsDead)
            return;

        firstPersonController.playerCanMove = _moveBeforeHide;
        firstPersonController.cameraCanMove = _cameraMoveBeforeHide;
        if (rb != null)
            rb.isKinematic = false;
    }

    public void SetAnimation(PlayerAnimation animation)
    {
        playerAnimationController.SetAnimation(animation);
    }
    public void SetAnimation(int animation)
    {
        playerAnimationController.SetAnimation(animation);
    }

    /// <summary>Freezes walking and look input and kills momentum, e.g. when the player dies.</summary>
    public void StopMovement()
    {
        if (firstPersonController != null)
        {
            firstPersonController.playerCanMove = false;
            firstPersonController.cameraCanMove = false;
        }

        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
    /// <summary>Swings the camera onto the target's position and keeps it there while it moves, e.g. the Nanny's face.</summary>
    public void ForceLookAt(Transform target, float duration = -1f)
    {
        if (duration < 0f)
            duration = lookDuration;

        // Remember the pre-look setting once, even if a second attack retriggers mid-swing.
        if (!_lookActive)
            _cameraMoveBeforeLook = firstPersonController.cameraCanMove;

        _lookActive = true;
        // Stop the FPC from overwriting camera rotation while we drive it.
        firstPersonController.cameraCanMove = false;

        if (_lookRoutine != null)
            StopCoroutine(_lookRoutine);
        _lookRoutine = StartCoroutine(LookAtRoutine(target, duration));
    }

    /// <summary>Cancels a forced look immediately, optionally handing camera control back.</summary>
    public void ReleaseForcedLook(bool restoreCameraControl = true)
    {
        if (_lookRoutine != null)
        {
            StopCoroutine(_lookRoutine);
            _lookRoutine = null;
        }

        if (_lookActive && restoreCameraControl && firstPersonController != null)
            firstPersonController.cameraCanMove = _cameraMoveBeforeLook;

        _lookActive = false;
    }

    IEnumerator LookAtRoutine(Transform target, float duration)
    {
        FirstPersonController fpc = firstPersonController;
        Transform body = fpc.transform;
        Transform cam = fpc.playerCamera.transform;

        float startYaw = body.localEulerAngles.y;
        float startPitch = NormalizeAngle(cam.localEulerAngles.x);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            ComputeLookAngles(target.position, body, cam, fpc.maxLookAngle,
                out float targetYaw, out float targetPitch);

            body.localEulerAngles = new Vector3(0f, Mathf.LerpAngle(startYaw, targetYaw, t), 0f);
            cam.localEulerAngles = new Vector3(Mathf.LerpAngle(startPitch, targetPitch, t), 0f, 0f);
            yield return null;
        }

        if (target != null)
        {
            ComputeLookAngles(target.position, body, cam, fpc.maxLookAngle,
                out float finalYaw, out float finalPitch);
            body.localEulerAngles = new Vector3(0f, finalYaw, 0f);
            cam.localEulerAngles = new Vector3(finalPitch, 0f, 0f);
            // Keep the FPC's internal pitch in sync so control hand-off doesn't snap the view.
            fpc.InitializePitch(finalPitch);
        }

        _lookRoutine = null;

        // Don't wrestle control back from a death / unconscious cinematic.
        bool dead = playerHealth.IsDead;
        if (_lookActive && _cameraMoveBeforeLook && !dead)
            fpc.cameraCanMove = true;

        _lookActive = false;
    }

    /// <summary>Yaw for the body and pitch for the camera so the view points at a world point.</summary>
    static void ComputeLookAngles(Vector3 worldPoint, Transform body, Transform cam, float maxPitch,
        out float yaw, out float pitch)
    {
        Vector3 flat = worldPoint - body.position;
        flat.y = 0f;
        yaw = flat.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flat).eulerAngles.y
            : body.localEulerAngles.y;

        Vector3 toPoint = worldPoint - cam.position;
        float horizontal = new Vector2(toPoint.x, toPoint.z).magnitude;
        // FPC convention: positive pitch tilts the view down, so negate the elevation angle.
        pitch = -Mathf.Atan2(toPoint.y, Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
