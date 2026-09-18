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
    public float navMeshSampleDistance = 6f;

    [Header("Loop")]
    public bool loop = true;
    [Min(0f)] public float loopDelay = 1.5f;

    [Header("Fade")]
    [Min(0f)] public float fadeInDuration = 0.35f;
    [Min(0f)] public float fadeOutDuration = 0.6f;

    [Header("Footsteps")]
    public float delta = 0.4f;
    public float gap = 0.2f;
    public int maxFootsteps = 80;

    Vector3 lastEmit;
    Vector3 lastDestinationSample;
    int dir = 1;
    bool isTrailing;
    float trailAlpha = 1f;
    Coroutine trailRoutine;
    ParticleSystemRenderer particleRenderer;
    MaterialPropertyBlock particleBlock;
    Color baseTint = Color.white;

    public bool IsTrailing => isTrailing;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (system == null)
            system = GetComponentInChildren<ParticleSystem>(true);
        if (system != null)
            particleRenderer = system.GetComponent<ParticleSystemRenderer>();

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

        if (fade && fadeOutDuration > 0f && system != null && system.particleCount > 0)
            trailRoutine = StartCoroutine(FadeOutAndDisable());
        else
            DisableTrailImmediate();
    }

    IEnumerator RunTrail()
    {
        isTrailing = true;
        ConfigureParticleSystem();

        while (isTrailing && destination != null)
        {
            if (!TrySampleNavMesh(GetPlayerPosition(), out var startNav))
            {
                yield return null;
                continue;
            }

            if (!TrySampleNavMesh(destination.position, out var destNav))
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
            StopParticles(true);
            SetTrailAlpha(fadeInDuration > 0f ? 0f : 1f);

            yield return null;

            float fadeInStart = Time.time;
            while (isTrailing && destination != null && !HasArrived())
            {
                if (fadeInDuration > 0f)
                    SetTrailAlpha(Mathf.Clamp01((Time.time - fadeInStart) / fadeInDuration));

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

            if (!isTrailing || destination == null)
                break;

            if (fadeOutDuration > 0f)
                yield return FadeTrail(trailAlpha, 0f, fadeOutDuration);

            StopParticles(false);
        }

        if (isTrailing && fadeOutDuration > 0f && system != null && system.particleCount > 0)
            yield return FadeTrail(trailAlpha, 0f, fadeOutDuration);

        DisableTrailImmediate();
    }

    IEnumerator FadeOutAndDisable()
    {
        PauseAgent();
        yield return FadeTrail(trailAlpha, 0f, fadeOutDuration);
        DisableTrailImmediate();
    }

    IEnumerator FadeTrail(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTrailAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetTrailAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetTrailAlpha(to);
    }

    Vector3 GetPlayerPosition()
    {
        if (GamePlayManager.Instance != null && GamePlayManager.Instance.player != null)
            return GamePlayManager.Instance.player.transform.position;

        var player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position : transform.position;
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
        if (NavMesh.SamplePosition(worldPosition, out var hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            sampled = hit.position;
            return true;
        }

        sampled = worldPosition;
        return false;
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
            rotation = transform.eulerAngles.y
        };
        system.Emit(ep, 1);
        lastEmit = transform.position;
    }

    void ConfigureParticleSystem()
    {
        if (system == null)
            return;

        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 999f;
        main.maxParticles = Mathf.Max(main.maxParticles, maxFootsteps);
        main.ringBufferMode = ParticleSystemRingBufferMode.Disabled;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = false;
        baseTint = Color.white;
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
        SetTrailAlpha(1f);
    }

    void StopParticles(bool playAfter)
    {
        if (system == null)
            return;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (playAfter)
            system.Play();
    }

    void SetTrailAlpha(float alpha)
    {
        trailAlpha = Mathf.Clamp01(alpha);
        if (particleRenderer == null)
            return;

        if (particleBlock == null)
            particleBlock = new MaterialPropertyBlock();

        particleRenderer.GetPropertyBlock(particleBlock);
        var tint = baseTint;
        tint.a = baseTint.a * trailAlpha;
        tint.r *= trailAlpha;
        tint.g *= trailAlpha;
        tint.b *= trailAlpha;
        particleBlock.SetColor("_TintColor", tint);
        particleBlock.SetColor("_Color", new Color(1f, 1f, 1f, trailAlpha));
        particleRenderer.SetPropertyBlock(particleBlock);
    }
}
