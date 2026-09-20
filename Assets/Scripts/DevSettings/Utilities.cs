using DG.Tweening;
using UnityEngine;

/// <summary>
/// Minimal helper set required by the ported DevSettings menu. Lives in the
/// global namespace so it is reachable from every script (including
/// <c>PeopleFun.UI.UICollapsibleGroup</c>, which does not import any helper
/// namespace) without an explicit <c>using</c>.
/// </summary>
public static class DevSettingsExtensions
{
    /// <summary>Destroys every child of the given transform.</summary>
    public static void RemoveAllChildren(this Transform transform)
    {
        if (transform == null) return;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>Convenience accessor for a component's <see cref="RectTransform"/>.</summary>
    public static RectTransform RectTransform(this Component component)
    {
        return component != null ? component.transform as RectTransform : null;
    }

    /// <summary>Convenience accessor for a game object's <see cref="RectTransform"/>.</summary>
    public static RectTransform RectTransform(this GameObject go)
    {
        return go != null ? go.transform as RectTransform : null;
    }

    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static T GetOrCreateComponent<T>(this Component component) where T : Component
    {
        if (component == null) return null;
        var existing = component.GetComponent<T>();
        return existing != null ? existing : component.gameObject.AddComponent<T>();
    }
}

/// <summary>
/// Small DOTween helpers used by the DevSettings copy-on-hold feedback.
/// </summary>
public static class AnimationUtilities
{
    /// <summary>
    /// Scales the target up and back down once. Returns the underlying tween so
    /// callers can chain <c>OnComplete</c>.
    /// </summary>
    public static Tween Pulse(GameObject target, float duration, float scale)
    {
        if (target == null) return null;

        var rt = target.transform;
        rt.DOKill();
        rt.localScale = Vector3.one;

        return rt.DOScale(Vector3.one * (1f + scale), duration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo);
    }
}

namespace PeopleFun.Utilities
{
    // Namespace intentionally retained so existing `using PeopleFun.Utilities;`
    // directives in the ported DevSettings files continue to resolve. All shared
    // helpers live in the global namespace above.
    internal static class PeopleFunUtilitiesNamespaceAnchor { }
}
