using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;

public class CullingArea : MonoBehaviour
{
    [Header("Settings")]
    public bool startEnabled = true;
    public bool justDisableMeshRenderers = true;
    
    [Header("References")]
    public Collider areaTrigger;
    public GameObject areaRoot;
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    
    private bool isEnabled = true;
    
    public Collider AreaTrigger => areaTrigger;
    public bool IsEnabled => isEnabled;

    private void Awake()
    {
        if (areaTrigger == null)
        {
            areaTrigger = GetComponent<Collider>();
        }
    }

    private void Start()
    {
        SetEnabled(startEnabled);
    }

    [InspectorButton("GetMeshRenderers")]
    public void GetMeshRenderers()
    {
        if (areaRoot != null)
        {
            meshRenderers = new List<MeshRenderer>(areaRoot.GetComponentsInChildren<MeshRenderer>(true));
        }
        else
        {
            meshRenderers = new List<MeshRenderer>(GetComponentsInChildren<MeshRenderer>(true));
        }
    }

    [InspectorButton("EnableArea")]
    public void EnableArea()
    {
        SetEnabled(true);
    }

    [InspectorButton("DisableArea")]
    public void DisableArea()
    {
        SetEnabled(false);
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        
        if (justDisableMeshRenderers)
        {
            foreach (var mr in meshRenderers)
            {
                if (mr != null)
                {
                    mr.enabled = enabled;
                }
            }
        }
        else if (areaRoot != null)
        {
            areaRoot.SetActive(enabled);
        }
    }

    public void OnTriggerEnter()
    {
        SetEnabled(true);
    }

    public void OnTriggerExit()
    {
        SetEnabled(false);
    }
}

