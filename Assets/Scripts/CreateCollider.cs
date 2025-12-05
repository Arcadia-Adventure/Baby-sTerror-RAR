using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CreateUpdate();
    }
    public MeshRenderer[] meshObjects;
    // Update is called once per frame
    void CreateUpdate()
    {
        meshObjects = FindObjectsOfType<MeshRenderer>();
        foreach (var item in meshObjects)
        {
            var col=item.gameObject.AddComponent<MeshCollider>();
            //col.convex = true;
        }
    }
}
