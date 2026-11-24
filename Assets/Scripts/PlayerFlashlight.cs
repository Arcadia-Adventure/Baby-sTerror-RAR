using System;
using ControlFreak2;
using Ommy.Audio;
using UnityEngine;

/// <summary>
/// The flashlight, once picked up. It's equipment rather than a held item, so the player can still
/// carry the baby or a fuse with it on. Built on the player camera at runtime, so no prefab change
/// is needed. Needs URP additional lights set to Per Pixel for the cone to render.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerFlashlight : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.L;
    [SerializeField] float range = 14f;
    [SerializeField] float spotAngle = 60f;
    [SerializeField] float innerSpotAngle = 30f;
    [SerializeField] float intensity = 6f;
    [SerializeField] Color color = new Color(1f, 0.95f, 0.85f);
    [SerializeField] AudioClip clickSFX;

    public bool HasFlashlight { get; private set; }
    public bool IsOn => _light != null && _light.enabled;
    public event Action<bool> Toggled;

    Light _light;

    void Update()
    {
        if (HasFlashlight && CF2Input.GetKeyDown(toggleKey))
            Toggle();
    }

    /// <summary>Gives the player the flashlight, switched on.</summary>
    public void Equip()
    {
        if (HasFlashlight)
            return;

        HasFlashlight = true;
        BuildLight();
        if (UIManager.Instance != null)
            UIManager.Instance.SetFlashlightButtonVisible(true);
        SetOn(true);
    }

    public void Toggle() => SetOn(!IsOn);

    public void SetOn(bool on)
    {
        if (!HasFlashlight || _light == null || _light.enabled == on)
            return;

        _light.enabled = on;
        if (clickSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSFX);
        if (UIManager.Instance != null)
            UIManager.Instance.SetFlashlightState(on);
        Toggled?.Invoke(on);
    }

    void BuildLight()
    {
        if (_light != null)
            return;

        Camera cam = GetComponent<FirstPersonController>().playerCamera;
        var go = new GameObject("Flashlight");
        go.transform.SetParent(cam.transform, false);
        // Slightly right of and below the eye, like a torch held in the hand.
        go.transform.localPosition = new Vector3(0.15f, -0.12f, 0.1f);

        _light = go.AddComponent<Light>();
        _light.type = LightType.Spot;
        _light.range = range;
        _light.spotAngle = spotAngle;
        _light.innerSpotAngle = innerSpotAngle;
        _light.intensity = intensity;
        _light.color = color;
        // A light made at runtime is always realtime; lightmapBakeType is editor-only API.
        _light.shadows = LightShadows.None;
        _light.renderMode = LightRenderMode.ForcePixel;
        _light.enabled = false;
    }
}
