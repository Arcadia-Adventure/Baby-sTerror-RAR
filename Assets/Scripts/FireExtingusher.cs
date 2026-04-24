using System.Collections;
using UnityEngine;

public class FireExtingusher : UseableItem
{
    public float extinguishTime = 2f;
    public float sprayRange = 3f;
    public float sprayRadius = 0.5f;
    public ParticleSystem sprayVFX;
    public LayerMask fireLayer;

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