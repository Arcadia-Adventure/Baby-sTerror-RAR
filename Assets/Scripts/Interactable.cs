using UnityEngine;

public class Interactable : MonoBehaviour
{
    public CrosshairState crosshairState;
    public string detectionText;
    public virtual void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    public virtual void Detected()
    {
        Debug.Log("Detected " + transform.name);
    }
    public virtual void Interacted()
    {
        Debug.Log("Interacting with " + transform.name);
    }
}
