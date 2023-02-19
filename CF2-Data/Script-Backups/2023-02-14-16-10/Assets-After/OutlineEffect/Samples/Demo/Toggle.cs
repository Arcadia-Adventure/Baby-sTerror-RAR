using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace cakeslice
{
    public class Toggle : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(ControlFreak2.CF2Input.GetKeyDown(KeyCode.K))
            {
                GetComponent<Outline>().enabled = !GetComponent<Outline>().enabled;
            }
        }
    }
}