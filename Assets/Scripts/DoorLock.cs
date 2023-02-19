using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorLock : MonoBehaviour
{
    public ParticleSystem redDoorGlow;
    public ParticleSystem greenDoorGlow;

    private void Start()
    {
        if(GameManager.instance.selectedLevel == 8)
        {
            this.tag = "DoorBreak";
            redDoorGlow.Play();
        }
    }

}
