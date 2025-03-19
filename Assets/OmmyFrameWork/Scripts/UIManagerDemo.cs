using System.Collections;
using System.Collections.Generic;
using Ommy.Attributes;
using Ommy.FadeSystem;
using UnityEngine;

public class UIManagerDemo : MonoBehaviour
{
    public GameObject[] panels;
    public void ActivePanel(int index)
    {
        foreach (var item in panels)
        {
            item.SetActive(false);
        }
        panels[index].SetActive(true);
    }
    [InspectorButton]
    public void MyFunction()
    {
        Debug.Log("MyFunction call");
    }
}
