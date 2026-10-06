using UnityEngine;
public class UseableItem : PickableItem
{
    public Sprite useSprite;
    void OnEnable()
    {
        OnPick.AddListener(OnPickDevice);
        OnDrop.AddListener(OnDropDevice);
    }
    void OnDisable()
    {
        OnPick.RemoveListener(OnPickDevice);
        OnDrop.RemoveListener(OnDropDevice);
    }
    public virtual void OnPickDevice()
    {
        if(useSprite)UIManager.Instance.useDevice.SetSprite(useSprite);
        UIManager.Instance.SetUseDeviceButtonVisible(true);
    }
    public virtual void OnDropDevice()
    {
        UIManager.Instance.SetUseDeviceButtonVisible(false);
    }
    public virtual void UseDevice()
    {
        Debug.Log("Using Device: " + transform.name);
    }
}
