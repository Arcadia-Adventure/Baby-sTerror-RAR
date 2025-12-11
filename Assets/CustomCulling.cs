using System.Collections.Generic;
using Ommy.Attributes;
using UnityEngine;
using UnityEngine.UI;

public class CustomCulling : MonoBehaviour
{
    public bool justDisableMeshRenderers;
    public bool DisableFirstFloorAtStart = true;
    public bool groundFloorTriggered, firstFloorTriggered;
    public Collider groundCheckTrigger;
    public Collider firstCheckTrigger;
    public GameObject groundFloorObjects;
    public GameObject firstFloorObjects;
    public List<MeshRenderer> firstFloorMeshRenderers;
    public List<MeshRenderer> groundFloorMeshRenderers;
    [InspectorButton("GetMeshs")]
    public void GetMeshs()
    {
        if (groundFloorObjects != null)
        {
            groundFloorMeshRenderers = new List<MeshRenderer>(groundFloorObjects.GetComponentsInChildren<MeshRenderer>());
        }
        if (firstFloorObjects != null)
        {
            firstFloorMeshRenderers = new List<MeshRenderer>(firstFloorObjects.GetComponentsInChildren<MeshRenderer>());
        }
    }
    private void Start() 
    {
        if (DisableFirstFloorAtStart)
        {
            SwitchFloor(false);
        }
    }
    [InspectorButton("FirstFloor")]
    public void FirstFloor()
    {
        SwitchFloor(true);
    }
    [InspectorButton("GroundFloor")]
    public void GroundFloor()
    {
        SwitchFloor(false);
    }
    [InspectorButton("EnableAllFloors")]
    public void EnableAllFloors()
    {
        SwitchFloor(false);
    }
    public void SwitchFloor(bool toFirstFloor)
    {

            firstFloorTriggered = toFirstFloor;
            groundFloorTriggered = !toFirstFloor;
            if (justDisableMeshRenderers)
            {
                firstFloorMeshRenderers.ForEach(mr => mr.enabled = toFirstFloor);
                groundFloorMeshRenderers.ForEach(mr => mr.enabled = !toFirstFloor);
                return;
            }
        firstFloorObjects.SetActive(toFirstFloor);
        groundFloorObjects.SetActive(!toFirstFloor);
    }
    public void OnPlayerTrigger(Collider other)
    {
        if(other == firstCheckTrigger)
            SwitchFloor(true);
        else if(other == groundCheckTrigger)
            SwitchFloor(false);
    }
}