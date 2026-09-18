using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TweenUtilities
{
    public static void Kill(object target)
    {
        if (target is Object unityObject && unityObject == null) return;
        if (target == null) return;
        DOTween.Kill(target);
    }

    public static Tweener Fade(Graphic graphic, float endValue, float duration, TweenCallback onComplete = null)
    {
        if (graphic == null) return null;
        graphic.DOKill();
        return Link(graphic.DOFade(endValue, duration), graphic, onComplete);
    }

    public static Tweener Fade(CanvasGroup canvasGroup, float endValue, float duration, TweenCallback onComplete = null)
    {
        if (canvasGroup == null) return null;
        canvasGroup.DOKill();
        return Link(canvasGroup.DOFade(endValue, duration), canvasGroup, onComplete);
    }

    public static Tweener Rotate(Transform transform, Vector3 endValue, float duration)
    {
        if (transform == null) return null;
        return Link(transform.DORotate(endValue, duration), transform);
    }

    public static Tweener LocalRotate(Transform transform, Vector3 endValue, float duration)
    {
        if (transform == null) return null;
        return Link(transform.DOLocalRotate(endValue, duration), transform);
    }

    public static Tweener LocalMove(Transform transform, Vector3 endValue, float duration)
    {
        if (transform == null) return null;
        return Link(transform.DOLocalMove(endValue, duration), transform);
    }

    public static Sequence LocalJump(Transform transform, Vector3 endValue, float jumpPower, int numJumps, float duration)
    {
        if (transform == null) return null;
        return Link(transform.DOLocalJump(endValue, jumpPower, numJumps, duration), transform);
    }

    public static Tweener PunchRotation(Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
    {
        if (transform == null) return null;
        return Link(transform.DOPunchRotation(punch, duration, vibrato, elasticity), transform);
    }

    public static Tweener ShakeRotation(Transform transform, float duration, float strength, int vibrato)
    {
        if (transform == null) return null;
        return Link(transform.DOShakeRotation(duration, strength, vibrato), transform);
    }

    public static Tweener Rotate(Rigidbody rigidbody, Vector3 endValue, float duration)
    {
        if (rigidbody == null) return null;
        return Link(rigidbody.DORotate(endValue, duration), rigidbody);
    }

    public static Sequence Sequence(Component target)
    {
        var sequence = DOTween.Sequence();
        if (target == null) return sequence;
        sequence.SetTarget(target);
        return Link(sequence, target);
    }

    public static Tween DelayedCall(float delay, TweenCallback callback, Component linkTarget = null)
    {
        var tween = DOVirtual.DelayedCall(delay, callback);
        return linkTarget == null ? tween : Link(tween, linkTarget);
    }

    public static Tweener To(Component target, DOGetter<float> getter, DOSetter<float> setter, float endValue, float duration)
    {
        var tween = DOTween.To(getter, setter, endValue, duration);
        if (target == null) return tween;
        tween.SetTarget(target);
        return Link(tween, target);
    }

    public static Tweener Typewriter(TMP_Text text, string value, float charsPerSecond, TweenCallback onComplete = null)
    {
        if (text == null) return null;
        text.DOKill();
        text.text = value ?? string.Empty;
        text.maxVisibleCharacters = 0;

        if (!text.gameObject.activeInHierarchy || charsPerSecond <= 0f)
        {
            text.maxVisibleCharacters = int.MaxValue;
            onComplete?.Invoke();
            return null;
        }

        text.ForceMeshUpdate();
        int charCount = text.textInfo.characterCount;
        if (charCount <= 0)
        {
            text.maxVisibleCharacters = int.MaxValue;
            onComplete?.Invoke();
            return null;
        }

        float visible = 0f;
        float duration = charCount / charsPerSecond;
        var tween = To(text, () => visible, x =>
        {
            visible = x;
            text.maxVisibleCharacters = Mathf.FloorToInt(x);
        }, charCount, duration);
        tween.SetEase(Ease.Linear);
        if (onComplete != null) tween.OnComplete(onComplete);
        return tween;
    }

    static T Link<T>(T tween, Component target, TweenCallback onComplete = null) where T : Tween
    {
        if (tween == null || target == null) return tween;
        tween.SetLink(target.gameObject);
        if (onComplete != null) tween.OnComplete(onComplete);
        return tween;
    }
}
