using ControlFreak2;
using UnityEngine;

/// <summary>
/// Detects a quick, still touch on a CF2 trackpad as a tap. Swipes still drive the trackpad as normal.
/// </summary>
[RequireComponent(typeof(TouchTrackPad))]
public class TrackPadTap : MonoBehaviour
{
    [SerializeField] float maxTapDuration = 0.3f;
    [SerializeField] float maxTapMovementCm = 0.3f;

    TouchTrackPad trackPad;
    float pressTime;
    float movedPx;

    public bool JustTapped { get; private set; }

    void Awake() => trackPad = GetComponent<TouchTrackPad>();

    void Update()
    {
        JustTapped = false;

        if (trackPad.JustPressed())
        {
            pressTime = Time.unscaledTime;
            movedPx = 0f;
        }
        else if (trackPad.Pressed())
        {
            movedPx += trackPad.GetSwipeDelta().magnitude;
        }
        else if (trackPad.JustReleased())
        {
            JustTapped = Time.unscaledTime - pressTime <= maxTapDuration
                && movedPx <= maxTapMovementCm * CFScreen.dpcm;
        }
    }
}
