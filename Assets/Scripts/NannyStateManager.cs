using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NannyStateManager : MonoBehaviour
{
    const float DoorCheckInterval = 0.15f;
    const float DoorCheckRadius = 0.15f;
    const float LockedDoorIgnoreTime = 8f;
    const float MinJumpScareDistance = 0.8f;
    const int DoorBangAttackVariant = 0;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private NannyAnimationController animationController;
    [SerializeField] private NannyAudioController audioController;

    [Header("Movement")]
    [Tooltip("How fast she turns to face the direction she is moving, in degrees per second.")]
    [SerializeField] private float moveTurnSpeed = 720f;

    [Header("Patrol")]
    [Tooltip("Optional. Leave empty and she wanders to random reachable points around her spawn instead.")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float wanderRadius = 12f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float arriveTolerance = 0.8f;
    [Tooltip("Random pause at each patrol point, in seconds.")]
    [SerializeField] private Vector2 patrolPauseRange = new Vector2(1f, 3.5f);

    [Header("Detection")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 110f;
    [Tooltip("She notices the player anywhere inside this radius, even directly behind her.")]
    [SerializeField] private float hearingRadius = 4f;
    [Tooltip("Height the sight check is cast from, in local units.")]
    [SerializeField] private float eyeHeight = 1.6f;
    [Tooltip("Geometry that blocks her line of sight. Hits on the player are ignored automatically.")]
    [SerializeField] private LayerMask sightBlockers = ~0;

    [Header("Noise")]
    [Tooltip("Extra hearing radius while the player carries the baby. Noise carries through walls, " +
             "but only tells her where to look.")]
    [SerializeField] private float babyNoiseBonus = 3f;
    [Tooltip("Extra hearing radius while the player is running.")]
    [SerializeField] private float sprintNoiseBonus = 3f;
    [Tooltip("Player speed above which they count as running. The player walks at 3.")]
    [SerializeField] private float sprintSpeedThreshold = 4f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float searchSpeed = 2f;
    [Tooltip("How long she keeps hunting after losing sight of the player.")]
    [SerializeField] private float loseInterestTime = 6f;
    [Tooltip("Freeze and scream when she first spots the player.")]
    [SerializeField] private bool screamOnDetect = true;
    [Tooltip("Minimum time she stays frozen. She also waits until the scream animation has finished.")]
    [SerializeField] private float screamDuration = 1.2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 34f;
    [SerializeField] private float attackCooldown = 2.5f;
    [Tooltip("Delay from the swing starting to the moment damage lands, so the hit matches the animation.")]
    [SerializeField] private float attackImpactDelay = 0.45f;
    [Tooltip("She misses if the player escaped past this range by the time the hit lands.")]
    [SerializeField] private float attackHitRange = 2.4f;
    [SerializeField] private float attackTurnSpeed = 540f;

    [Header("Hunter")]
    [Tooltip("Hunter mode: seconds between drifts toward the player's area.")]
    [SerializeField] private float huntInterval = 15f;
    [Tooltip("Hunting destinations land this far around the player, so she closes in without homing.")]
    [SerializeField] private float huntRadius = 5f;

    [Header("Glimpse")]
    [Tooltip("Seconds the player has to look at her before she reacts.")]
    [SerializeField] private float glimpseNoticeTime = 0.4f;
    [Tooltip("She also reacts if the player walks this close, looking or not.")]
    [SerializeField] private float glimpseVanishDistance = 3f;
    [Tooltip("Half-angle around the camera's forward that counts as looking at her.")]
    [SerializeField] private float glimpseLookAngle = 25f;
    [SerializeField] private float glimpseTurnSpeed = 60f;

    [Header("Jump Scare")]
    [SerializeField] private float jumpScareDistance = 1.5f;
    [SerializeField] private float jumpScareLookDuration = 0.12f;
    [Tooltip("After a jump scare she can't attack for this long, so the player gets a chance to run.")]
    [SerializeField] private float jumpScareAttackGrace = 1.5f;

    [Header("Player Camera")]
    [Tooltip("What the player's camera turns to when she kills, jump-scares or is banished. " +
             "Drag in her head bone or an empty object on her face.")]
    [SerializeField] private Transform lookAtPoint;

    [Header("Doors")]
    [Tooltip("How far ahead she checks for a closed door in her way.")]
    [SerializeField] private float doorCheckDistance = 1f;
    [Tooltip("World height above her feet that the door check is cast from.")]
    [SerializeField] private float doorCheckHeight = 0.8f;
    [Tooltip("Seconds she pounds on a closed door before bursting through. Locked doors hold.")]
    [SerializeField] private float doorBreakTime = 2.5f;
    [SerializeField] private float doorBangInterval = 0.6f;
    [SerializeField] private LayerMask doorLayers = ~0;

    public NannyState State { get; private set; } = NannyState.Patrol;
    public NannyMode Mode => _mode;
    /// <summary>How long a jump scare holds the player, for anything that should wait for it to land.</summary>
    public float JumpScareDuration => screamDuration;

    /// <summary>The point on her the player's camera aims at to look her in the face.</summary>
    public Transform LookAtPoint => lookAtPoint != null ? lookAtPoint : transform;

    Transform _player;
    PlayerController _playerController;
    PlayerHealth _playerHealth;
    Rigidbody _playerBody;
    PickDropController _pickDrop;
    Collider _bodyCollider;
    Vector3 _spawnPosition;
    Vector3 _lastKnownPlayerPosition;
    int _patrolIndex = -1;
    float _stateTimer;
    float _timeSinceSeenPlayer;
    float _patrolPauseDuration;
    float _screamEndTime;
    float _nextAttackTime;
    bool _attackPending;
    bool _hasPatrolTarget;
    bool _screamedThisHunt;

    NannyMode _mode = NannyMode.Patrol;
    bool _configured;
    bool _crawling;
    bool _harmless;
    bool _vanishing;
    float _glimpseLookTime;
    float _nextHuntTime;

    DoorController _door;
    Vector3 _doorPoint;
    DoorController _ignoredDoor;
    float _ignoreDoorUntil;
    float _nextDoorCheckTime;
    float _nextBangTime;
    NannyState _stateBeforeDoor;
    float _stateTimerBeforeDoor;

    Vector3 EyePosition => transform.position + Vector3.up * eyeHeight * transform.lossyScale.y;
    Vector3 BodyCenter => _bodyCollider != null && _bodyCollider.enabled
        ? _bodyCollider.bounds.center
        : transform.position + Vector3.up;
    // The timer covers the frame before the animator picks up the Scream trigger.
    bool IsScreaming => Time.time < _screamEndTime
        || (animationController != null && animationController.IsScreaming);
    bool IsRoaming => State is NannyState.Idle or NannyState.Patrol or NannyState.Chase or NannyState.Search;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animationController == null) animationController = GetComponent<NannyAnimationController>();
        if (audioController == null) audioController = GetComponent<NannyAudioController>();
        _bodyCollider = GetComponent<Collider>();

        // Facing is driven from the actual velocity so she never slides sideways through turns.
        if (agent != null) agent.updateRotation = false;

        _spawnPosition = transform.position;
    }

    private void Start()
    {
        // Once GamePlayManager has configured her, Activate decides where and how she starts.
        if (!_configured)
            EnterPatrol();
    }

    private void Update()
    {
        if (State == NannyState.Dead)
            return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!TryResolvePlayer())
            return;

        _stateTimer += Time.deltaTime;

        switch (State)
        {
            case NannyState.Idle:
            case NannyState.Patrol:
                TickPatrol();
                break;
            case NannyState.Chase:
                TickChase();
                break;
            case NannyState.Attack:
                TickAttack();
                break;
            case NannyState.Search:
                TickSearch();
                break;
            case NannyState.Glimpse:
                TickGlimpse();
                break;
            case NannyState.BangDoor:
                TickBangDoor();
                break;
            case NannyState.Banished:
                TickBanished();
                break;
        }

        // A tick can kill her or make her vanish.
        if (State == NannyState.Dead || !isActiveAndEnabled)
            return;

        if (IsRoaming && !IsScreaming)
        {
            CheckForBlockingDoor();
            FaceMovementDirection();
        }

        UpdateAnimationAndAudio();
    }

    /// <summary>
    /// GamePlayManager spawns and wires the player during its own Start, so the reference
    /// isn't guaranteed to exist on our first frame.
    /// </summary>
    bool TryResolvePlayer()
    {
        if (_player != null)
            return true;

        PlayerController player = GamePlayManager.Instance != null ? GamePlayManager.Instance.player : null;
        if (player == null)
            return false;

        _player = player.transform;
        _playerController = player;
        _playerHealth = player.GetComponent<PlayerHealth>();
        _playerBody = player.GetComponent<Rigidbody>();
        // Looked up rather than via Instance, which would spawn an empty controller in test scenes.
        _pickDrop = FindFirstObjectByType<PickDropController>();
        return true;
    }

    #region States

    void EnterPatrol()
    {
        State = NannyState.Patrol;
        _stateTimer = 0f;
        _patrolPauseDuration = 0f;
        _hasPatrolTarget = false;
        _screamedThisHunt = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
        }
    }

    void TickPatrol()
    {
        if (CanSeePlayer())
        {
            EnterChase();
            return;
        }

        if (CanHearPlayer())
        {
            _lastKnownPlayerPosition = _player.position;
            EnterSearch();
            return;
        }

        if (TryStartHunt())
            return;

        if (_hasPatrolTarget)
        {
            if (!HasArrived())
                return;

            // Arrived: stand still for a beat before choosing the next point.
            _hasPatrolTarget = false;
            _stateTimer = 0f;
            _patrolPauseDuration = Random.Range(patrolPauseRange.x, patrolPauseRange.y);
            agent.isStopped = true;
            return;
        }

        if (_stateTimer < _patrolPauseDuration)
            return;

        if (TryPickPatrolDestination(out Vector3 destination))
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            agent.SetDestination(destination);
            _hasPatrolTarget = true;
        }

        _stateTimer = 0f;
    }

    /// <summary>Hunter mode: every so often, swap the patrol target for a spot near the player.</summary>
    bool TryStartHunt()
    {
        if (_mode != NannyMode.Hunter || Time.time < _nextHuntTime)
            return false;

        _nextHuntTime = Time.time + huntInterval;
        if (!TryFindPointNear(_player.position, huntRadius, out Vector3 destination))
            return false;

        agent.speed = Mathf.Max(patrolSpeed, searchSpeed);
        agent.isStopped = false;
        agent.SetDestination(destination);
        _hasPatrolTarget = true;
        _stateTimer = 0f;
        return true;
    }

    void EnterChase(bool forceScream = false)
    {
        bool scream = forceScream || (screamOnDetect && !_screamedThisHunt);

        State = NannyState.Chase;
        _stateTimer = 0f;
        _timeSinceSeenPlayer = 0f;
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (scream)
        {
            _screamedThisHunt = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            PlayScream();
        }
    }

    void TickChase()
    {
        bool visible = CanSeePlayer();
        if (visible)
        {
            _lastKnownPlayerPosition = _player.position;
            _timeSinceSeenPlayer = 0f;
        }
        else
        {
            _timeSinceSeenPlayer += Time.deltaTime;
            if (CanHearPlayer())
                _lastKnownPlayerPosition = _player.position;
        }

        if (IsScreaming)
        {
            agent.isStopped = true;
            FacePlayer(attackTurnSpeed);
            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(visible ? _player.position : _lastKnownPlayerPosition);

        if (visible && PlanarDistanceToPlayer() <= attackRange)
        {
            if (PlayerHoldsTalisman())
            {
                EnterBanished();
                return;
            }

            if (CanAttack())
            {
                EnterAttack();
                return;
            }
        }

        if (!visible && _timeSinceSeenPlayer >= loseInterestTime)
            EnterSearch();
    }

    void EnterAttack()
    {
        State = NannyState.Attack;
        _stateTimer = 0f;
        _attackPending = true;
        _nextAttackTime = Time.time + attackCooldown;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        int variant = animationController.AttackVariants[Random.Range(0, animationController.AttackVariants.Length)];

        animationController?.PlayAttack(variant);
        audioController?.Play(NannySound.Attack);
    }

    void TickAttack()
    {
        FacePlayer(attackTurnSpeed);

        if (_attackPending && _stateTimer >= attackImpactDelay)
        {
            _attackPending = false;
            TryLandHit();
        }

        if (_stateTimer < attackCooldown)
            return;

        if (CanSeePlayer())
        {
            _lastKnownPlayerPosition = _player.position;
            EnterChase();
        }
        else
        {
            EnterSearch();
        }
    }

    void TryLandHit()
    {
        if (_harmless || PlayerHoldsTalisman())
            return;

        if (_playerHealth == null || _playerHealth.IsDead)
            return;

        if (PlanarDistanceToPlayer() > attackHitRange)
            return;

        _playerHealth.TakeDamage(attackDamage, LookAtPoint);
    }

    void EnterSearch()
    {
        State = NannyState.Search;
        _stateTimer = 0f;
        agent.speed = searchSpeed;
        agent.isStopped = false;
        agent.SetDestination(_lastKnownPlayerPosition);
    }

    void TickSearch()
    {
        if (CanSeePlayer())
        {
            _lastKnownPlayerPosition = _player.position;
            EnterChase();
            return;
        }

        // Following a noise keeps the search alive for as long as the player keeps making it.
        if (CanHearPlayer())
        {
            _lastKnownPlayerPosition = _player.position;
            agent.SetDestination(_lastKnownPlayerPosition);
            _stateTimer = 0f;
            return;
        }

        if (_stateTimer >= loseInterestTime || HasArrived())
            EnterPatrol();
    }

    void EnterGlimpse()
    {
        State = NannyState.Glimpse;
        _stateTimer = 0f;
        _glimpseLookTime = 0f;
        _vanishing = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void TickGlimpse()
    {
        if (_vanishing)
        {
            if (!IsScreaming)
                Hide();
            return;
        }

        FacePlayer(glimpseTurnSpeed);

        if (IsPlayerLookingAtMe())
            _glimpseLookTime += Time.deltaTime;

        if (_glimpseLookTime >= glimpseNoticeTime || PlanarDistanceToPlayer() <= glimpseVanishDistance)
        {
            _vanishing = true;
            if (screamOnDetect)
                PlayScream();
        }
    }

    void EnterBangDoor(DoorController door, Vector3 contactPoint)
    {
        _stateBeforeDoor = State;
        _stateTimerBeforeDoor = _stateTimer;

        State = NannyState.BangDoor;
        _stateTimer = 0f;
        _door = door;
        _doorPoint = contactPoint;
        _nextBangTime = Time.time;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animationController?.PlayAttack(DoorBangAttackVariant);
    }

    void TickBangDoor()
    {
        // The player opened it for her, or something else did.
        if (_door == null || _door.isDoorOpen)
        {
            ResumeAfterDoor();
            return;
        }

        FaceTowards(_doorPoint, attackTurnSpeed);

        if (Time.time >= _nextBangTime)
        {
            _door.BangOnDoor(audioController != null ? audioController.DoorBangClip : null);
            _nextBangTime = Time.time + doorBangInterval;
        }

        if (_stateTimer < doorBreakTime)
            return;

        if (_door.isDoorLock)
        {
            // Locked doors hold, so she gives up on this one and goes elsewhere.
            _ignoredDoor = _door;
            _ignoreDoorUntil = Time.time + LockedDoorIgnoreTime;
            _door = null;
            EnterPatrol();
            return;
        }

        _door.ForceOpen();
        ResumeAfterDoor();
    }

    void ResumeAfterDoor()
    {
        _door = null;
        State = _stateBeforeDoor;
        _stateTimer = _stateTimerBeforeDoor;
        agent.isStopped = false;
        // The leaf is still swinging, so don't trip over the same door straight away.
        _nextDoorCheckTime = Time.time + 0.5f;
    }

    void EnterBanished()
    {
        State = NannyState.Banished;
        _stateTimer = 0f;
        _attackPending = false;
        _harmless = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        _playerController?.ForceLookAt(LookAtPoint);
        PlayScream();
    }

    void TickBanished()
    {
        FacePlayer(attackTurnSpeed);

        if (IsScreaming || _stateTimer < screamDuration)
            return;

        Die();
        ObjectiveUIController.OnTaskEventReceived(TaskType.BanishNanny);
    }

    void PlayScream()
    {
        _screamEndTime = Time.time + screamDuration;
        animationController?.PlayScream();
        audioController?.Play(NannySound.Scream);
    }

    void Hide()
    {
        _vanishing = false;
        if (audioController != null)
        {
            audioController.SetFootstepsActive(false);
            audioController.Stop();
        }
        gameObject.SetActive(false);
    }

    #endregion

    #region Perception

    bool CanSeePlayer()
    {
        if (_player == null)
            return false;

        if (_playerHealth != null && _playerHealth.IsDead)
            return false;

        Vector3 toPlayer = _player.position - transform.position;
        float distance = toPlayer.magnitude;

        bool withinEarshot = distance <= hearingRadius;
        if (!withinEarshot)
        {
            if (distance > viewDistance)
                return false;

            Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flatDirection.sqrMagnitude < 0.0001f)
                return false;

            if (Vector3.Angle(transform.forward, flatDirection.normalized) > viewAngle * 0.5f)
                return false;
        }

        return HasLineOfSight();
    }

    /// <summary>
    /// Noise reaches her through walls, unlike sight, so it only tells her where to go looking.
    /// </summary>
    bool CanHearPlayer()
    {
        if (_player == null || (_playerHealth != null && _playerHealth.IsDead))
            return false;

        float bonus = NoiseBonus();
        if (bonus <= 0f)
            return false;

        // Full distance, so the player on the floor above isn't heard as if they were next to her.
        return (_player.position - transform.position).magnitude <= hearingRadius + bonus;
    }

    float NoiseBonus()
    {
        float bonus = 0f;
        if (_pickDrop != null && _pickDrop.heldPickable is BabyController)
            bonus = babyNoiseBonus;

        if (PlayerPlanarSpeed() > sprintSpeedThreshold)
            bonus = Mathf.Max(bonus, sprintNoiseBonus);

        return bonus;
    }

    bool HasLineOfSight()
    {
        Vector3 eye = EyePosition;
        // Aim at the player's chest rather than their pivot, which sits on the floor.
        Vector3 target = _player.position + Vector3.up;
        Vector3 direction = target - eye;

        if (!Physics.Raycast(eye, direction.normalized, out RaycastHit hit, direction.magnitude,
                sightBlockers, QueryTriggerInteraction.Ignore))
            return true;

        // The player's own capsule is the expected first hit, so it must not count as cover.
        return hit.transform.root == _player.root;
    }

    bool IsPlayerLookingAtMe()
    {
        Camera cam = _playerController != null ? _playerController.PlayerCamera : null;
        if (cam == null)
            return false;

        Transform view = cam.transform;
        Vector3 toMe = BodyCenter - view.position;
        if (toMe.magnitude > viewDistance)
            return false;

        if (Vector3.Angle(view.forward, toMe) > glimpseLookAngle)
            return false;

        return HasLineOfSight();
    }

    bool PlayerHoldsTalisman()
    {
        PickableItem held = _pickDrop != null ? _pickDrop.heldPickable : null;
        return held != null && held.itemType == ItemType.Talisman;
    }

    float PlayerPlanarSpeed()
    {
        if (_playerBody == null)
            return 0f;

        Vector3 velocity = _playerBody.linearVelocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    float PlanarDistanceToPlayer()
    {
        Vector3 delta = _player.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    /// <summary>
    /// The NavMesh is baked straight through the doorways, so a closed door has to be spotted
    /// physically or she would walk through it.
    /// </summary>
    void CheckForBlockingDoor()
    {
        if (Time.time < _nextDoorCheckTime)
            return;
        _nextDoorCheckTime = Time.time + DoorCheckInterval;

        if (agent.isStopped || !agent.hasPath)
            return;

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector3 origin = transform.position + Vector3.up * doorCheckHeight;
        if (!Physics.SphereCast(origin, DoorCheckRadius, direction.normalized, out RaycastHit hit,
                doorCheckDistance, doorLayers, QueryTriggerInteraction.Ignore))
            return;

        DoorController door = hit.collider.GetComponentInParent<DoorController>();
        if (door == null || door.isDoorOpen)
            return;

        if (door == _ignoredDoor && Time.time < _ignoreDoorUntil)
            return;

        EnterBangDoor(door, hit.point);
    }

    #endregion

    #region Helpers

    bool CanAttack() => !_harmless && Time.time >= _nextAttackTime;

    bool HasArrived()
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= Mathf.Max(arriveTolerance, agent.stoppingDistance);
    }

    bool TryPickPatrolDestination(out Vector3 destination)
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                if (patrolPoints[_patrolIndex] != null)
                {
                    destination = patrolPoints[_patrolIndex].position;
                    return true;
                }
            }
        }

        return TryFindPointNear(_spawnPosition, wanderRadius, out destination);
    }

    bool TryFindPointNear(Vector3 center, float radius, out Vector3 destination)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, agent.areaMask))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    /// <summary>
    /// A clear spot right in front of the player, or right behind them if they are facing a wall,
    /// so the forced look turns them around into her.
    /// </summary>
    bool TryFindJumpScareSpot(Vector3 forward, out Vector3 spot)
    {
        spot = default;
        if (!NavMesh.SamplePosition(_player.position, out NavMeshHit start, 2f, agent.areaMask))
            return false;

        for (int side = 0; side < 2; side++)
        {
            Vector3 direction = side == 0 ? forward : -forward;
            Vector3 target = start.position + direction * jumpScareDistance;
            Vector3 end = NavMesh.Raycast(start.position, target, out NavMeshHit blocked, agent.areaMask)
                ? blocked.position
                : target;

            if ((end - start.position).magnitude >= MinJumpScareDistance)
            {
                spot = end;
                return true;
            }
        }

        return false;
    }

    void PlaceAt(Vector3 position, Quaternion rotation)
    {
        if (agent != null && agent.enabled && NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, agent.areaMask))
            agent.Warp(hit.position);
        else
            transform.position = position;

        Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized);
    }

    void FacePlayer(float turnSpeed) => FaceTowards(_player.position, turnSpeed);

    void FaceTowards(Vector3 point, float turnSpeed)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction.normalized),
            turnSpeed * Time.deltaTime);
    }

    void FaceMovementDirection()
    {
        Vector3 velocity = PlanarVelocity();
        if (velocity.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(velocity.normalized),
            moveTurnSpeed * Time.deltaTime);
    }

    Vector3 PlanarVelocity()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        return velocity;
    }

    void UpdateAnimationAndAudio()
    {
        // Always the real velocity: after isStopped the agent still brakes, and the legs must follow.
        float speed = PlanarVelocity().magnitude;
        animationController?.SetSpeed(speed);

        if (audioController == null)
            return;

        audioController.SetFootstepsActive(speed > 0.2f);

        if (IsScreaming)
            return;

        if (State is NannyState.Chase or NannyState.Attack or NannyState.BangDoor)
            audioController.Play(NannySound.Chasing);
        else
            audioController.Play(NannySound.Breathing);
    }

    #endregion

    #region Public API

    /// <summary>Applies a level's tuning. She stays where she is until Activate.</summary>
    public void Configure(NannyModule setup, Transform[] route)
    {
        _configured = true;
        patrolPoints = route;

        if (setup == null)
            return;

        _mode = setup.mode;
        patrolSpeed = setup.patrolSpeed;
        chaseSpeed = setup.chaseSpeed;
        viewDistance = setup.viewDistance;
        viewAngle = setup.viewAngle;
        hearingRadius = setup.hearingRadius;
        babyNoiseBonus = setup.babyNoiseBonus;
        attackDamage = setup.attackDamage;
        loseInterestTime = setup.loseInterestTime;
        huntInterval = setup.huntInterval;
        doorBreakTime = setup.doorBreakTime;
        _crawling = setup.crawling;
    }

    /// <summary>Brings her into the level at the spawn point and starts her mode's behaviour.</summary>
    public void Activate(Transform spawnPoint)
    {
        if (State == NannyState.Dead)
            return;

        gameObject.SetActive(true);

        if (spawnPoint != null)
            PlaceAt(spawnPoint.position, spawnPoint.rotation);
        else
            Debug.LogWarning("[NannyStateManager] No spawn point for this level, so she starts where she stands.", this);

        _spawnPosition = transform.position;
        _patrolIndex = -1;
        // The first hunt comes sooner, so a Hunter level gets going quickly.
        _nextHuntTime = Time.time + huntInterval * 0.5f;
        SetCrawling(_crawling);

        if (_mode == NannyMode.Glimpse)
            EnterGlimpse();
        else
            EnterPatrol();
    }

    /// <summary>
    /// Appears in the player's face, screams, and forces their view onto her. In Glimpse mode she
    /// vanishes afterwards; otherwise the chase starts from there.
    /// </summary>
    public void JumpScare(PlayerController player)
    {
        if (State == NannyState.Dead || player == null)
            return;

        gameObject.SetActive(true);
        if (agent == null || !agent.enabled || !TryResolvePlayer())
            return;

        Transform view = player.PlayerCamera != null ? player.PlayerCamera.transform : player.transform;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = player.transform.forward;
        forward.Normalize();

        if (!TryFindJumpScareSpot(forward, out Vector3 spot))
            return;

        Vector3 toPlayer = player.transform.position - spot;
        PlaceAt(spot, Quaternion.LookRotation(toPlayer.sqrMagnitude > 0.0001f ? toPlayer : -forward));
        SetCrawling(_crawling);

        _nextAttackTime = Mathf.Max(_nextAttackTime, Time.time + jumpScareAttackGrace);
        player.ForceLookAt(LookAtPoint, jumpScareLookDuration);

        if (_mode == NannyMode.Glimpse)
        {
            EnterGlimpse();
            _vanishing = true;
            PlayScream();
        }
        else
        {
            _lastKnownPlayerPosition = player.transform.position;
            EnterChase(forceScream: true);
        }
    }

    /// <summary>Makes her harmless for good, e.g. once the level is won. A running scare still plays out.</summary>
    public void StandDown()
    {
        _harmless = true;
        _attackPending = false;
    }

    /// <summary>Stops the hunt for good and plays the death animation.</summary>
    public void Die()
    {
        if (State == NannyState.Dead)
            return;

        State = NannyState.Dead;
        _attackPending = false;
        _harmless = true;
        _door = null;

        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.enabled = false;
        }

        // The upright capsule would leave an invisible wall where her body has fallen.
        foreach (Collider body in GetComponents<Collider>())
            body.enabled = false;

        animationController?.SetSpeed(0f);
        animationController?.PlayDeath();

        if (audioController != null)
        {
            audioController.SetFootstepsActive(false);
            audioController.Play(NannySound.Death);
        }
    }

    /// <summary>Sends her hunting toward a position, e.g. from a scripted scare or a noise.</summary>
    public void AlertTo(Vector3 position)
    {
        if (State == NannyState.Dead || _mode == NannyMode.Glimpse)
            return;

        _lastKnownPlayerPosition = position;
        EnterChase();
    }

    public void SetCrawling(bool isCrawling)
    {
        _crawling = isCrawling;
        animationController?.SetCrawling(isCrawling);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight * transform.lossyScale.y;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(eye, viewDistance);
        Quaternion left = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up);
        Gizmos.DrawRay(eye, left * transform.forward * viewDistance);
        Gizmos.DrawRay(eye, right * transform.forward * viewDistance);

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius + Mathf.Max(babyNoiseBonus, sprintNoiseBonus));

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
