using System;
using System.Collections;
using UnityEngine;

public class NannyAnimationController : MonoBehaviour
{
    public const string SpeedParam = "Speed";
    public const string CrawlParam = "IsCrawling";
    public const string AttackParam = "Attack";
    public const string AttackVariantParam = "AttackVariant";
    public const string ScreamParam = "Scream";
    public const string DieParam = "Die";
    public const string LocomotionRateParam = "LocomotionRate";

    static readonly int LocomotionTag = Animator.StringToHash("Locomotion");
    static readonly int ScreamStateHash = Animator.StringToHash("Scream");
    const float MovingThreshold = 0.05f;

    /// <summary>Attack, Bite, Bite2 and NeckBite, in AttackVariant order.</summary>
    public int[] AttackVariants = { 0, 1, 2, 3 };

    [SerializeField] private Animator nannyAnimator;
    [Tooltip("How quickly the locomotion blend reacts to the agent speeding up or slowing down.")]
    [SerializeField] private float speedDamping = 0.15f;

    [Header("Foot Sliding")]
    [Tooltip("Min/max playback rate for walk and run while stretching the animation to match her real speed.")]
    [SerializeField] private Vector2 locomotionRateRange = new Vector2(0.5f, 3f);
    [Tooltip("Seconds the animation's own ground speed is averaged over. Roughly one footstep.")]
    [SerializeField] private float strideSmoothing = 0.3f;

    private Coroutine _onCompleteRoutine;
    float _moveSpeed;
    float _animGroundSpeed;
    float _locomotionRate = 1f;

    public Animator Animator => nannyAnimator;

    /// <summary>True while the Scream state is playing, including the blend into and out of it.</summary>
    public bool IsScreaming
    {
        get
        {
            if (nannyAnimator == null) return false;
            if (nannyAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == ScreamStateHash) return true;
            return nannyAnimator.IsInTransition(0)
                && nannyAnimator.GetNextAnimatorStateInfo(0).shortNameHash == ScreamStateHash;
        }
    }

    private void Awake()
    {
        if (nannyAnimator == null)
            nannyAnimator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed)
    {
        _moveSpeed = speed;
        if (nannyAnimator == null) return;
        nannyAnimator.SetFloat(SpeedParam, speed, speedDamping, Time.deltaTime);
    }

    /// <summary>
    /// Root motion is only measured here, never applied: the NavMeshAgent owns the transform.
    /// The walk/run playback rate is scaled so the feet cover the same ground the agent does.
    /// </summary>
    private void OnAnimatorMove()
    {
        if (nannyAnimator == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (nannyAnimator.GetCurrentAnimatorStateInfo(0).tagHash != LocomotionTag)
        {
            _animGroundSpeed = 0f;
            ApplyLocomotionRate(1f);
            return;
        }

        Vector3 delta = nannyAnimator.deltaPosition;
        delta.y = 0f;
        // deltaPosition already includes the previous rate, so divide it out to get the 1x speed.
        float sampledSpeed = delta.magnitude / dt / Mathf.Max(_locomotionRate, 0.01f);
        _animGroundSpeed = _animGroundSpeed <= 0f
            ? sampledSpeed
            : Mathf.Lerp(_animGroundSpeed, sampledSpeed, 1f - Mathf.Exp(-dt / Mathf.Max(strideSmoothing, 0.01f)));

        float rate = 1f;
        if (_moveSpeed > MovingThreshold && _animGroundSpeed > 0.01f)
            rate = Mathf.Clamp(_moveSpeed / _animGroundSpeed, locomotionRateRange.x, locomotionRateRange.y);

        ApplyLocomotionRate(rate);
    }

    void ApplyLocomotionRate(float rate)
    {
        _locomotionRate = rate;
        nannyAnimator.SetFloat(LocomotionRateParam, rate);
    }

    public void SetCrawling(bool isCrawling)
    {
        if (nannyAnimator == null) return;
        nannyAnimator.SetBool(CrawlParam, isCrawling);
    }

    public void PlayScream(Action onComplete = null)
    {
        Trigger(ScreamParam, onComplete);
    }

    public void PlayAttack(int variant, Action onComplete = null)
    {
        if (nannyAnimator == null) return;
        // AttackVariants contains the actual Animator parameter values, not array indices.
        nannyAnimator.SetInteger(AttackVariantParam, variant);
        Trigger(AttackParam, onComplete);
    }

    public void PlayDeath(Action onComplete = null)
    {
        Trigger(DieParam, onComplete);
    }

    void Trigger(string parameter, Action onComplete)
    {
        if (nannyAnimator == null)
            return;

        ResetAllTriggers();
        nannyAnimator.SetTrigger(parameter);

        if (_onCompleteRoutine != null)
            StopCoroutine(_onCompleteRoutine);

        if (onComplete != null)
            _onCompleteRoutine = StartCoroutine(WaitForAnimationComplete(onComplete));
    }

    void ResetAllTriggers()
    {
        nannyAnimator.ResetTrigger(AttackParam);
        nannyAnimator.ResetTrigger(ScreamParam);
        nannyAnimator.ResetTrigger(DieParam);
    }

    private IEnumerator WaitForAnimationComplete(Action onComplete)
    {
        // Let the animator start the transition on the next frame
        yield return null;

        // Wait until the transition into the new state finishes
        while (nannyAnimator.IsInTransition(0))
            yield return null;

        // Now we're in the target state — wait for it to play through
        float duration = nannyAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(duration);

        _onCompleteRoutine = null;
        onComplete.Invoke();
    }
}
