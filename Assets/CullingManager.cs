using System.Collections.Generic;
using Ommy.Attributes;
using Ommy.Singleton;
using UnityEngine;

public class CullingManager : Singleton<CullingManager>
{
    [Header("References")]
    public List<CullingArea> cullingAreas = new List<CullingArea>();
    public CullingArea defaultArea;
    private CullingArea currentActiveArea;

    private void Start()
    {
        if (currentActiveArea == null && defaultArea != null)
        {
            SetActiveArea(defaultArea);
        }
    }

    [InspectorButton("FindAllCullingAreas")]
    public void FindAllCullingAreas()
    {
        cullingAreas = new List<CullingArea>(FindObjectsByType<CullingArea>(FindObjectsSortMode.None));
    }

    public void RegisterArea(CullingArea area)
    {
        if (!cullingAreas.Contains(area))
        {
            cullingAreas.Add(area);
        }
    }

    public void UnregisterArea(CullingArea area)
    {
        cullingAreas.Remove(area);
    }

    /// <summary>
    /// Called when player enters a trigger collider.
    /// Checks all CullingAreas for activator/deactivator triggers.
    /// </summary>
    public void OnPlayerTriggerEnter(Collider triggerCollider)
    {
        foreach (var area in cullingAreas)
        {
            if (area == null) continue;
            
            int triggerType = area.CheckTrigger(triggerCollider);
            
            if (triggerType == 1) // Activator
            {
                area.SetEnabled(true);
            }
            else if (triggerType == -1) // Deactivator
            {
                area.SetEnabled(false);
            }
        }
    }

    /// <summary>
    /// Sets the active area, disabling all others in exclusive mode.
    /// </summary>
    public void SetActiveArea(CullingArea area)
    {
        if (area == null) return;
        
        currentActiveArea = area;
        
        foreach (var cullingArea in cullingAreas)
        {
            if (cullingArea != null)
            {
                cullingArea.SetEnabled(cullingArea == area);
            }
        }
    }

    /// <summary>
    /// Sets the active area by index.
    /// </summary>
    public void SetActiveArea(int index)
    {
        if (index >= 0 && index < cullingAreas.Count)
        {
            SetActiveArea(cullingAreas[index]);
        }
    }

    /// <summary>
    /// Enables all culling areas.
    /// </summary>
    [InspectorButton("EnableAllAreas")]
    public void EnableAllAreas()
    {
        foreach (var area in cullingAreas)
        {
            if (area != null)
            {
                area.SetEnabled(true);
            }
        }
    }

    /// <summary>
    /// Disables all culling areas.
    /// </summary>
    [InspectorButton("DisableAllAreas")]
    public void DisableAllAreas()
    {
        foreach (var area in cullingAreas)
        {
            if (area != null)
            {
                area.SetEnabled(false);
            }
        }
    }

    public CullingArea GetCurrentActiveArea()
    {
        return currentActiveArea;
    }
}

