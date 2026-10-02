using System;
using System.Collections;
using UnityEngine;

public class BabyAnimationController : MonoBehaviour
{
    public Animator babyAnimator;
    public BabyAnimationType initialAndCurrentAnim;
    private Coroutine _onCompleteRoutine;

    public void Start()
    {
        babyAnimator = GetComponent<Animator>();
        if (initialAndCurrentAnim != BabyAnimationType.None)
            SetAnimation(initialAndCurrentAnim);
    }

    /// <summary>
    /// Jumps straight to an animation's first frame and evaluates it immediately,
    /// so the skeleton is already in that pose this frame (no transition blend).
    /// </summary>
    public void SnapToAnimation(BabyAnimationType animationType)
    {
        if (babyAnimator == null)
            babyAnimator = GetComponent<Animator>();
        if (babyAnimator == null || animationType == BabyAnimationType.None)
            return;

        bool wasEnabled = babyAnimator.enabled;
        babyAnimator.enabled = true;

        ResetAllTriggers();

        string stateName = animationType.ToString();
        if (babyAnimator.HasState(0, Animator.StringToHash(stateName)))
            babyAnimator.Play(stateName, 0, 0f);
        else
            babyAnimator.SetTrigger(stateName);

        babyAnimator.Update(0f);
        initialAndCurrentAnim = animationType;
        babyAnimator.enabled = wasEnabled;
    }

    void ResetAllTriggers()
    {
        foreach (BabyAnimationType type in Enum.GetValues(typeof(BabyAnimationType)))
        {
            if (type == BabyAnimationType.None) continue;
            babyAnimator.ResetTrigger(type.ToString());
        }
    }

    public void SetAnimation(BabyAnimationType animationType, Action onComplete = null)
    {
        if (babyAnimator == null)
            babyAnimator = GetComponent<Animator>();
        if (babyAnimator == null)
            return;

        ResetAllTriggers();

        if (animationType != BabyAnimationType.None)
            babyAnimator.SetTrigger(animationType.ToString());

        initialAndCurrentAnim = animationType;

        if (_onCompleteRoutine != null)
            StopCoroutine(_onCompleteRoutine);

        if (onComplete != null)
            _onCompleteRoutine = StartCoroutine(WaitForAnimationComplete(onComplete));
    }

    private IEnumerator WaitForAnimationComplete(Action onComplete)
    {
        // Let the animator start the transition on the next frame
        yield return null;

        // Wait until the transition into the new state finishes
        while (babyAnimator.IsInTransition(0))
            yield return null;

        // Now we're in the target state — wait for it to play through
        float duration = babyAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(duration);

        _onCompleteRoutine = null;
        onComplete.Invoke();
    }
}
