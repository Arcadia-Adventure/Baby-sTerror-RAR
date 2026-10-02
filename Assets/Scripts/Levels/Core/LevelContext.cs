using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Everything modules and actions can reach while a level plays.</summary>
public class LevelContext
{
    readonly Dictionary<SceneObjectKey, GameObject> _objects = new();

    public LevelContext(LevelDefinition definition, LevelSceneSetup scene, GamePlayManager manager)
    {
        Definition = definition;
        Scene = scene;
        Manager = manager;

        var tags = UnityEngine.Object.FindObjectsByType<SceneObjectTag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SceneObjectTag tag in tags)
        {
            if (tag.key == null)
                continue;

            if (_objects.TryGetValue(tag.key, out GameObject existing) && existing != tag.gameObject)
                Debug.LogWarning($"[LevelContext] Key '{tag.key.name}' is on both {existing.name} and {tag.name}; using {existing.name}.", tag);
            else
                _objects[tag.key] = tag.gameObject;
        }
    }

    public LevelDefinition Definition { get; }
    public LevelSceneSetup Scene { get; }
    public GamePlayManager Manager { get; }

    public PlayerController Player => Manager.player;
    public BabyController Baby => Manager.baby;
    public NannyStateManager Nanny => Manager.nanny;

    public bool LevelEnded { get; internal set; }

    /// <summary>Time.time at which the Nanny's latest jump scare finishes.</summary>
    public float NannyJumpScareEndsAt { get; set; }

    public GameObject Get(SceneObjectKey key)
    {
        if (key == null)
            return null;

        if (_objects.TryGetValue(key, out GameObject target) && target != null)
            return target;

        Debug.LogWarning($"[LevelContext] Nothing in the scene is tagged '{key.name}'. Add a SceneObjectTag with that key.", key);
        return null;
    }

    public T Get<T>(SceneObjectKey key) where T : Component
    {
        GameObject target = Get(key);
        if (target == null)
            return null;

        T component = target.GetComponent<T>();
        if (component == null)
            component = target.GetComponentInChildren<T>(true);
        if (component != null)
            return component;

        // A real null, not Unity's fake-null object, so callers can use ?. safely.
        Debug.LogWarning($"[LevelContext] '{key.name}' ({target.name}) has no {typeof(T).Name}.", target);
        return null;
    }

    /// <summary>Runs the action after the delay; it's dropped if the gameplay scene unloads first.</summary>
    public void Delay(float seconds, Action action) =>
        TweenUtilities.DelayedCall(Mathf.Max(0f, seconds), () => action(), Manager);
}
