using Ommy.Audio;
using UnityEngine;

public class PlayerFootSteps : MonoBehaviour
{
    public FirstPersonController fpc;
    public Rigidbody rb;
    [Header("Footstep Audio Settings")]
    public float[] VolumeRange = {0.8f, 1f};
    public MyAudioSource footSteps;
    [Tooltip("Footstep volume multiplier while crouched.")]
    [Range(0f, 1f)] public float crouchVolumeMultiplier = 0.35f;

    PlayerController _player;

    private void Start()
    {
        fpc = GetComponent<FirstPersonController>();
        _player = GetComponent<PlayerController>();
        if (footSteps == null)
            footSteps = GetComponentInChildren<MyAudioSource>(true);
    }

    private void Update()
    {
        if (footSteps == null || !fpc.playerCanMove)
            return;

        if (fpc.isWalking && footSteps.isPlaying == false && rb.linearVelocity.magnitude > 1)
        {
            bool crouched = _player != null && _player.IsCrouched;
            float volume = Random.Range(VolumeRange[0], VolumeRange[1]);
            footSteps.volume = crouched ? volume * crouchVolumeMultiplier : volume;
            footSteps.pitch = crouched ? Random.Range(0.7f, 0.85f) : Random.Range(0.8f, 1.1f);
            footSteps.Play();
        }
    }
}
