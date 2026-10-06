using System.Collections;
using UnityEngine;

public class FireExtingusher : UseableItem
{
    public float extinguishTime = 2f;
    public float sprayRange = 3f;
    public float sprayRadius = 0.5f;
    public ParticleSystem sprayVFX;
    public LayerMask fireLayer;

    [Header("Nanny")]
    [Tooltip("Seconds she keeps running away after the spray stops reaching her.")]
    public float repelHoldTime = 1f;
    [Tooltip("How far off the nozzle's heading she can be and still get sprayed, in degrees either side.")]
    public float sprayAngle = 30f;
    [Tooltip("Geometry that stops the spray reaching her. The HeldItem layer is always skipped.")]
    public LayerMask nannyBlockers = ~0;

    Coroutine extinguishRoutine;
    GameObject fireTarget;

    public override void UseDevice()
    {
        base.UseDevice();
        if (sprayVFX.isPlaying) sprayVFX.Stop(true);
        else sprayVFX.Play(true);
    }

    public override void OnDropDevice()
    {
        base.OnDropDevice();
        if (sprayVFX.isPlaying) sprayVFX.Stop(true);
        StopExtinguish();
    }

    public override void Update()
    {
        base.Update();
        if (!sprayVFX.isPlaying) { StopExtinguish(); return; }

        var nanny = DetectNanny();
        if (nanny != null) nanny.Repel(repelHoldTime);

        var hit = DetectFire();
        if (hit && (extinguishRoutine == null || fireTarget != hit))
        {
            StopExtinguish();
            fireTarget = hit;
            extinguishRoutine = StartCoroutine(Extinguish(hit));
        }
        else if (!hit) StopExtinguish();
    }

    GameObject DetectFire()
    {
        var origin = sprayVFX.transform;
        var hits = Physics.SphereCastAll(origin.position, sprayRadius, origin.forward,
            sprayRange, fireLayer != 0 ? fireLayer : ~0, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
            if (h.collider.CompareTag("Fire")) return h.collider.gameObject;
        return null;
    }

    // Aims at her body rather than casting against her capsule, which only covers her legs.
    NannyStateManager DetectNanny()
    {
        var nanny = GamePlayManager.Instance.nanny;
        if (nanny == null || !nanny.CanBeHurt) return null;

        var origin = sprayVFX.transform;
        Vector3 toNanny = nanny.AimPoint - origin.position;
        float distance = toNanny.magnitude;
        if (distance > sprayRange) return null;
        if (distance < 0.01f) return nanny;

        // Heading only, so the player doesn't have to aim up at her face or down at her legs.
        Vector3 flatForward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
        Vector3 flatToNanny = Vector3.ProjectOnPlane(toNanny, Vector3.up);
        if (flatToNanny.sqrMagnitude > 0.0001f && Vector3.Angle(flatForward, flatToNanny) > sprayAngle) return null;

        int mask = nannyBlockers & ~(1 << LayerMask.NameToLayer("HeldItem"));
        foreach (var hit in Physics.RaycastAll(origin.position, toNanny / distance, distance, mask,
                     QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<NannyStateManager>() == nanny) continue;
            return null;
        }
        return nanny;
    }

    void StopExtinguish()
    {
        if (extinguishRoutine != null) StopCoroutine(extinguishRoutine);
        extinguishRoutine = null;
        fireTarget = null;
    }

    IEnumerator Extinguish(GameObject fireObj)
    {
        yield return new WaitForSeconds(extinguishTime);
        fireObj.GetComponentInParent<FireArea>().RemoveFireObject(fireObj);
        StopExtinguish();
    }
}