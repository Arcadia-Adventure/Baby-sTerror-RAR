using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;

public class CullingArea : MonoBehaviour
{
    [Header("Settings")]
    public bool startEnabled = true;
    public bool justDisableMeshRenderers = true;
    
    [Header("Triggers")]
    public Collider activatorTrigger;
    public Collider deactivatorTrigger;
    
    [Header("References")]
    public GameObject areaRoot;
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    private void Start()
    {
        if (CullingManager.Instance == null)
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

    /// <summary>
    /// Check if the given collider belongs to this culling area.
    /// Returns 1 if activator, -1 if deactivator, 0 if neither.
    /// </summary>
    public int CheckTrigger(Collider triggerCollider)
    {
        if (activatorTrigger != null && triggerCollider == activatorTrigger)
            return 1;
        if (deactivatorTrigger != null && triggerCollider == deactivatorTrigger)
            return -1;
        return 0;
    }
}

