using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFootSteps : MonoBehaviour
{
    public FirstPersonController fpc;
    public Rigidbody rb;

    public AudioSource footSteps;

    private void Start()
    {
        fpc = GetComponent<FirstPersonController>();
    }

    private void Update()
    {
        if (fpc.playerCanMove)
        {
            if (fpc.isWalking && footSteps.isPlaying == false&& rb.velocity.magnitude>1)
            {
                
                footSteps.volume = Random.Range(0.8f, 1);
                footSteps.pitch = Random.Range(0.8f, 1.1f);

                footSteps.Play();
            }
        }
    }
}
