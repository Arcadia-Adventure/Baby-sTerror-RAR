using System;
using System.Collections;
using Ommy.Audio;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The house's mains power. The lamps are baked into the lightmaps, so switching Light components
/// off would change nothing; instead a power cut swaps every lightmap for a tiny, almost-black one
/// and dims the light probes, leaving the player's flashlight (realtime) as the main light.
///
/// Make the dark lightmap with Baby's Terror > Lighting > Create Power-Cut Lightmap; it's imported
/// as a Lightmap texture so Unity encodes it correctly for each platform.
/// </summary>
public class HouseLights : MonoBehaviour
{
    /// <summary>True unless a level has cut the power. Resets when the gameplay scene unloads.</summary>
    public static bool IsPowerOn { get; private set; } = true;
    public static HouseLights Instance { get; private set; }
    public static event Action<bool> PowerChanged;

    [Tooltip("Tiny dark texture imported as a Lightmap. Used for every lightmap slot while the power is out.")]
    public Texture2D darkLightmap;
    [Tooltip("Light-probe brightness during a power cut, so moving things (the Nanny, the baby) go dark too.")]
    [Range(0f, 1f)] public float darkProbeScale = 0.12f;
    [Tooltip("Reflection brightness during a power cut.")]
    [Range(0f, 1f)] public float darkReflectionIntensity = 0.15f;
    [Tooltip("Realtime or mixed lights that should also go out (baked lamps are covered by the lightmap swap).")]
    public Light[] switchedLights = new Light[0];
    [Tooltip("The moonlight through the windows: dimmed, not switched off, so the windows still glow.")]
    public Light moonLight;
    [Range(0f, 1f)] public float darkMoonScale = 0.45f;
    [Tooltip("Lamp bulbs and shades whose glow should go out with the power.")]
    public Renderer[] emissiveRenderers = new Renderer[0];
    public AudioClip powerOffSFX;
    public AudioClip powerOnSFX;
    [Tooltip("Short buzz or click played on each flicker.")]
    public AudioClip flickerSFX;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    LightmapData[] _normalMaps;
    LightmapData[] _darkMaps;
    SphericalHarmonicsL2[] _normalProbes;
    SphericalHarmonicsL2[] _darkProbes;
    float _normalReflection;
    float _normalMoonIntensity;
    bool[] _lightStates;
    MaterialPropertyBlock _darkBlock;
    Coroutine _flicker;

    void Awake()
    {
        Instance = this;
        IsPowerOn = true;
        Cache();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        // Statics outlive the scene; the next level always starts with the power on.
        if (!IsPowerOn)
            Apply(true);
        Instance = null;
        IsPowerOn = true;
        PowerChanged = null;
    }

    void Cache()
    {
        _normalMaps = LightmapSettings.lightmaps;
        _normalReflection = RenderSettings.reflectionIntensity;
        if (moonLight != null)
            _normalMoonIntensity = moonLight.intensity;

        if (darkLightmap != null && _normalMaps != null)
        {
            _darkMaps = new LightmapData[_normalMaps.Length];
            for (int i = 0; i < _normalMaps.Length; i++)
            {
                _darkMaps[i] = new LightmapData
                {
                    lightmapColor = darkLightmap,
                    // Direction and shadowmask stay, so the moonlight still casts the right shadows.
                    lightmapDir = _normalMaps[i].lightmapDir,
                    shadowMask = _normalMaps[i].shadowMask,
                };
            }
        }
        else if (darkLightmap == null)
        {
            Debug.LogWarning("[HouseLights] No dark lightmap assigned, so a power cut only dims moving objects. " +
                             "Run Baby's Terror > Lighting > Create Power-Cut Lightmap.", this);
        }

        _lightStates = new bool[switchedLights.Length];
        for (int i = 0; i < switchedLights.Length; i++)
            _lightStates[i] = switchedLights[i] != null && switchedLights[i].enabled;

        _darkBlock = new MaterialPropertyBlock();
        _darkBlock.SetColor(EmissionColorId, Color.black);
    }

    /// <summary>Cuts or restores the power.</summary>
    public void SetPower(bool on, bool playSound = true)
    {
        if (_flicker != null)
        {
            StopCoroutine(_flicker);
            _flicker = null;
        }

        if (on == IsPowerOn)
            return;

        Apply(on);
        if (playSound)
            PlaySound(on ? powerOnSFX : powerOffSFX);
    }

    /// <summary>The lights stutter for a while, then settle on or off.</summary>
    public void Flicker(float duration, bool endOn)
    {
        if (_flicker != null)
            StopCoroutine(_flicker);
        _flicker = StartCoroutine(FlickerRoutine(duration, endOn));
    }

    IEnumerator FlickerRoutine(float duration, bool endOn)
    {
        float end = Time.time + duration;
        while (Time.time < end)
        {
            Apply(!IsPowerOn);
            PlaySound(flickerSFX);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.04f, IsPowerOn ? 0.35f : 0.18f));
        }

        _flicker = null;
        if (IsPowerOn != endOn)
        {
            Apply(endOn);
            PlaySound(endOn ? powerOnSFX : powerOffSFX);
        }
    }

    /// <summary>
    /// The probe data isn't readable while the scene is still loading (it reads back empty in
    /// Awake), so it's captured the first time the power actually changes.
    /// </summary>
    bool EnsureProbes()
    {
        LightProbes probes = LightmapSettings.lightProbes;
        if (probes == null || probes.count == 0)
            return false;

        if (_normalProbes == null || _normalProbes.Length != probes.count)
        {
            SphericalHarmonicsL2[] baked = probes.bakedProbes;
            if (baked == null || baked.Length != probes.count)
                return false;

            _normalProbes = baked;
            _darkProbes = new SphericalHarmonicsL2[baked.Length];
            for (int i = 0; i < baked.Length; i++)
                _darkProbes[i] = baked[i] * darkProbeScale;
        }

        return true;
    }

    void Apply(bool on)
    {
        IsPowerOn = on;
        bool probesReady = EnsureProbes();

        if (on)
        {
            if (_normalMaps != null)
                LightmapSettings.lightmaps = _normalMaps;
            if (probesReady)
                LightmapSettings.lightProbes.bakedProbes = _normalProbes;
            RenderSettings.reflectionIntensity = _normalReflection;
        }
        else
        {
            if (_darkMaps != null)
                LightmapSettings.lightmaps = _darkMaps;
            if (probesReady)
                LightmapSettings.lightProbes.bakedProbes = _darkProbes;
            RenderSettings.reflectionIntensity = _normalReflection * darkReflectionIntensity;
        }

        for (int i = 0; i < switchedLights.Length; i++)
        {
            if (switchedLights[i] != null)
                switchedLights[i].enabled = on && _lightStates[i];
        }

        if (moonLight != null)
            moonLight.intensity = on ? _normalMoonIntensity : _normalMoonIntensity * darkMoonScale;

        foreach (Renderer r in emissiveRenderers)
        {
            if (r != null)
                r.SetPropertyBlock(on ? null : _darkBlock);
        }

        PowerChanged?.Invoke(on);
    }

    static void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip);
    }
}
