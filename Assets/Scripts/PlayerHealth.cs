using System;
using Ommy.Audio;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Grace period after a hit during which further damage is ignored.")]
    [SerializeField] private float invulnerabilityDuration = 0.8f;

    [Header("Feedback")]
    [SerializeField] private AudioClip hurtSFX;
    [Tooltip("Camera that gets shaken on hit. Falls back to the main camera.")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private int shakeVibrato = 14;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDied;

    /// <summary>Fires with (current, max) whenever health changes, for driving a health bar.</summary>
    public static event Action<float, float> OnHealthChanged;
    public static event Action OnPlayerDied;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float Normalized => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
    public bool IsDead { get; private set; }

    float _invulnerableUntil;
    Transform _lastAttacker;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        if (shakeTarget == null && Camera.main != null)
            shakeTarget = Camera.main.transform;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void OnDestroy()
    {
        // These are static, so a stale subscriber would survive a level reload.
        OnHealthChanged = null;
        OnPlayerDied = null;
    }

    public void TakeDamage(float amount) => TakeDamage(amount, null);

    /// <param name="attacker">The point to look at on whoever dealt the hit (e.g. the Nanny's face), so the death camera can turn to it.</param>
    public void TakeDamage(float amount, Transform attacker)
    {
        if (IsDead || amount <= 0f || Time.time < _invulnerableUntil)
            return;

        if (attacker != null)
            _lastAttacker = attacker;

        _invulnerableUntil = Time.time + invulnerabilityDuration;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        PlayHurtFeedback();
        onDamaged?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        IsDead = false;
        _invulnerableUntil = 0f;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void PlayHurtFeedback()
    {
        if (hurtSFX != null)
            AudioManager.Instance.PlaySFX(hurtSFX);

        if (shakeTarget != null)
            TweenUtilities.ShakeRotation(shakeTarget, shakeDuration, shakeStrength, shakeVibrato);
    }

    void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        // Freeze the player and turn the camera onto the killer so the last thing they see
        // is the Nanny. (This replaces the old unconscious collapse, whose camera animation
        // would fight the look-at.)
        PlayerController player = GamePlayManager.Instance != null ? GamePlayManager.Instance.player : null;
        if (player != null)
        {
            player.StopMovement();
            if (_lastAttacker != null)
                player.ForceLookAt(_lastAttacker);
        }

        AudioManager.Instance.PlaySFX(SFX.Scream);

        onDied?.Invoke();
        OnPlayerDied?.Invoke();

        if (GamePlayManager.Instance != null)
            GamePlayManager.Instance.LevelFailed();
    }
}
