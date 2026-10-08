using System;
using UnityEngine;

/// <summary>
/// A trigger volume level assets can react to, e.g. through an On Player Enters Zone event.
/// Give it a SceneObjectTag so the level asset can find it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelTriggerZone : MonoBehaviour
{
    public event Action PlayerEntered;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PlayerEntered?.Invoke();
    }
}
