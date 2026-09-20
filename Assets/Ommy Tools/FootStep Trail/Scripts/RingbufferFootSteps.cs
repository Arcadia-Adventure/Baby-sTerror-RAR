using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RingbufferFootSteps : MonoBehaviour
{
    public ParticleSystem system;
    public NavMeshAgent agent;
    public Transform destination;

    [Header("Movement")]
    public float agentSpeed = 3.5f;
    public float arriveDistance = 0.75f;
    public float navMeshSampleDistance = 2f;
    [Tooltip("Max height difference to count as the same floor. Rejects the story above or below.")]
    public float sameFloorMaxY = 1.25f;

    [Header("Loop")]
    public bool loop = true;
    [Min(0f)] public float loopDelay = 1.5f;

    [Header("Fade")]
    [Tooltip("Each footprint fades in over this time after it spawns.")]
    [Min(0f)] public float fadeInDuration = 0.2f;
    [Tooltip("Each footprint starts fading out as soon as it spawns, over this time.")]
    [Min(0f)] public float fadeOutDuration = 0.6f;

    [Header("Footsteps")]
    public float delta = 0.4f;
    public float gap = 0.2f;
    public int maxFootsteps = 80;

    Vector3 lastEmit;
    Vector3 lastDestinationSample;
    int dir = 1;
    bool isTrailing;
    Coroutine trailRoutine;

    public bool IsTrailing => isTrailing;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (system == null)
            system = GetComponentInChildren<ParticleSystem>(true);

        var mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.enabled = false;

        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.autoBraking = true;
            if (agentSpeed > 0f)
                agent.speed = agentSpeed;
        }

        ConfigureParticleSystem();
        StopParticles(false);
    }

    void Start() => StopTrail(false);

    public void StartTrail(Transform dest)
    {
        StopTrail(false);
        destination = dest;
        if (destination == null || agent == null)
            return;

        trailRoutine = StartCoroutine(RunTrail());
    }

    public void StopTrail() => StopTrail(true);

    public void StopTrail(bool fade)
    {
        if (trailRoutine != null)
        {
            StopCoroutine(trailRoutine);
            trailRoutine = null;
        }

        isTrailing = false;

        if (fade && system != null && system.particleCount > 0)
            trailRoutine = StartCoroutine(LetFootstepsFinishThenDisable());
        else
            DisableTrailImmediate();
    }

    IEnumerator RunTrail()
    {
        isTrailing = true;
        ConfigureParticleSystem();
        bool firstPass = true;

        while (isTrailing && destination != null)
        {
            if (!TrySampleNavMesh(GetPlayerFeetPosition(), out var startNav) ||
                !TrySampleNavMesh(destination.position, out var destNav))
            {
                yield return null;
                continue;
            }

            if (Vector3.Distance(startNav, destNav) <= arriveDistance)
            {
                PauseAgent();
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            agent.Warp(startNav);
            agent.isStopped = false;
            lastDestinationSample = destNav;
            agent.SetDestination(destNav);
            lastEmit = startNav;

            if (firstPass)
            {
                StopParticles(true);
                firstPass = false;
            }
            else if (system != null && !system.isPlaying)
            {
                system.Play();
            }

            yield return null;

            while (isTrailing && destination != null && !HasArrived())
            {
                FollowDestination(destination.position);
                EmitFootsteps();
                yield return null;
            }

            if (!isTrailing || destination == null)
                break;

            PauseAgent();
            if (!loop)
                break;

            if (loopDelay > 0f)
                yield return new WaitForSeconds(loopDelay);
        }

        DisableTrailImmediate();
    }

    IEnumerator LetFootstepsFinishThenDisable()
    {
        PauseAgent();
        if (system != null)
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        yield return new WaitForSeconds(GetFootstepLifetime());
        DisableTrailImmediate();
    }

    Vector3 GetPlayerFeetPosition()
    {
        var player = GetPlayerTransform();
        if (player == null)
            return transform.position;

        float feetOffset = 0.9f;
        if (player.TryGetComponent(out CharacterController character))
            feetOffset = character.height * 0.5f - character.center.y + character.skinWidth;
        else if (player.TryGetComponent(out CapsuleCollider capsule))
            feetOffset = capsule.height * 0.5f - capsule.center.y;

        var feet = player.position;
        feet.y -= Mathf.Max(0.1f, feetOffset);
        return feet;
    }

    Transform GetPlayerTransform()
    {
        if (GamePlayManager.Instance != null && GamePlayManager.Instance.player != null)
            return GamePlayManager.Instance.player.transform;

        var player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    bool HasArrived()
    {
        if (agent == null || !agent.isOnNavMesh || agent.pathPending)
            return false;

        float stopDistance = Mathf.Max(arriveDistance, agent.stoppingDistance);
        if (Vector3.Distance(transform.position, lastDestinationSample) <= stopDistance)
            return true;

        return agent.hasPath
            && agent.remainingDistance <= stopDistance
            && agent.velocity.sqrMagnitude < 0.15f;
    }

    void FollowDestination(Vector3 worldPosition)
    {
        if (agent == null || !agent.isOnNavMesh || agent.pathPending)
            return;

        if (!TrySampleNavMesh(worldPosition, out var sampled))
            return;

        if ((sampled - lastDestinationSample).sqrMagnitude < 0.25f)
            return;

        lastDestinationSample = sampled;
        agent.SetDestination(sampled);
    }

    bool TrySampleNavMesh(Vector3 worldPosition, out Vector3 sampled)
    {
        sampled = worldPosition;
        float maxY = Mathf.Max(0.25f, sameFloorMaxY);
        float radius = Mathf.Min(Mathf.Max(0.25f, navMeshSampleDistance), maxY);

        Vector3[] probes =
        {
            worldPosition,
            worldPosition + Vector3.down * 0.4f,
            worldPosition + Vector3.up * 0.2f
        };

        bool found = false;
        float bestYDelta = float.MaxValue;
        float bestHoriz = float.MaxValue;

        for (int i = 0; i < probes.Length; i++)
        {
            if (!NavMesh.SamplePosition(probes[i], out var hit, radius, NavMesh.AllAreas))
                continue;

            float yDelta = Mathf.Abs(hit.position.y - worldPosition.y);
            if (yDelta > maxY)
                continue;

            float horiz = Vector2.Distance(
                new Vector2(hit.position.x, hit.position.z),
                new Vector2(worldPosition.x, worldPosition.z));

            if (yDelta < bestYDelta - 0.05f || (Mathf.Abs(yDelta - bestYDelta) <= 0.05f && horiz < bestHoriz))
            {
                bestYDelta = yDelta;
                bestHoriz = horiz;
                sampled = hit.position;
                found = true;
            }
        }

        return found;
    }

    void EmitFootsteps()
    {
        if (system == null)
            return;

        if (Vector3.Distance(lastEmit, transform.position) <= delta)
            return;

        var pos = system.transform.position + (transform.right * gap * dir);
        dir *= -1;

        var ep = new ParticleSystem.EmitParams
        {
            position = pos,
            rotation = transform.eulerAngles.y,
            startLifetime = GetFootstepLifetime()
        };
        system.Emit(ep, 1);
        lastEmit = transform.position;
    }

    float GetFootstepLifetime()
    {
        float lifetime = fadeInDuration + fadeOutDuration;
        return lifetime > 0.05f ? lifetime : 0.05f;
    }

    void ConfigureParticleSystem()
    {
        if (system == null)
            return;

        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = GetFootstepLifetime();
        main.maxParticles = Mathf.Max(main.maxParticles, maxFootsteps);
        main.ringBufferMode = ParticleSystemRingBufferMode.Disabled;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFootstepGradient());
    }

    Gradient BuildFootstepGradient()
    {
        float lifetime = GetFootstepLifetime();
        float fadeInEnd = fadeInDuration > 0f ? Mathf.Clamp01(fadeInDuration / lifetime) : 0f;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            fadeInEnd > 0f
                ? new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, fadeInEnd),
                    new GradientAlphaKey(0f, 1f)
                }
                : new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
        return gradient;
    }

    void PauseAgent()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void DisableTrailImmediate()
    {
        isTrailing = false;
        trailRoutine = null;
        PauseAgent();
        StopParticles(false);
    }

    void StopParticles(bool playAfter)
    {
        if (system == null)
            return;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (playAfter)
            system.Play();
    }
}
