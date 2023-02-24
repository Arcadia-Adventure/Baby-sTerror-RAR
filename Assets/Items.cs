using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Items : MonoBehaviour
{
    public static Items instance;
    private void Awake()
    {
        instance = this;
    }
    public GameObject facewash, telisman, fire, feeder, toy, fireCylinder, dress, fireLvl10;
}
