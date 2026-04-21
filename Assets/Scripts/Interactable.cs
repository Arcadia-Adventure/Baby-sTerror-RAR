using UnityEngine;

public class Interactable : MonoBehaviour
{
    public CrosshairState crosshairState;
    public string detectionText;
    public new Collider collider;
    public virtual void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    public virtual void Detected() { }
    public virtual void Undetected() { }
    public virtual void Interacted() { }
}
