using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;

public class CullingManager : MonoBehaviour
{
    public static CullingManager Instance { get; private set; }

    [Header("Settings")]
    public bool exclusiveMode = true; // Only one area enabled at a time
    
    [Header("References")]
    public List<CullingArea> cullingAreas = new List<CullingArea>();
    public CullingArea defaultArea;
    
    private CullingArea currentActiveArea;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (defaultArea != null)
        {
            SetActiveArea(defaultArea);
        }
    }

    [InspectorButton("FindAllCullingAreas")]
    public void FindAllCullingAreas()
    {
        cullingAreas = new List<CullingArea>(FindObjectsOfType<CullingArea>());
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
    /// Finds the matching CullingArea and activates it.
    /// </summary>
    public void OnPlayerTriggerEnter(Collider triggerCollider)
    {
        CullingArea area = FindAreaByTrigger(triggerCollider);
        if (area != null)
        {
            if (exclusiveMode)
            {
                SetActiveArea(area);
            }
            else
            {
                area.OnTriggerEnter();
            }
        }
    }

    /// <summary>
    /// Called when player exits a trigger collider.
    /// Finds the matching CullingArea and handles the exit logic.
    /// </summary>
    public void OnPlayerTriggerExit(Collider triggerCollider)
    {
        if (exclusiveMode) return; // In exclusive mode, we don't disable on exit
        
        CullingArea area = FindAreaByTrigger(triggerCollider);
        if (area != null)
        {
            area.OnTriggerExit();
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

    private CullingArea FindAreaByTrigger(Collider triggerCollider)
    {
        foreach (var area in cullingAreas)
        {
            if (area != null && area.AreaTrigger == triggerCollider)
            {
                return area;
            }
        }
        return null;
    }

    public CullingArea GetCurrentActiveArea()
    {
        return currentActiveArea;
    }
}

