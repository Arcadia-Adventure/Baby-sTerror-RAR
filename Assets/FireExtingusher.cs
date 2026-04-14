using System.Collections;
using UnityEngine;

public class FireExtingusher : UseableItem
{
    public float extinguishTime = 2f;
    public ParticleSystem sprayVFX;
    public override void UseDevice()
    {
        base.UseDevice();
        if (sprayVFX.isPlaying) sprayVFX.Stop(true);
        else sprayVFX.Play(true);
    }
    public override void OnDropDevice()
    {
        base.OnDropDevice();
        if (sprayVFX.isPlaying) sprayVFX.Stop(true);
    }
    Coroutine extinguishCoroutine;
    void OnTriggerEnter(Collider other)
    {
        if(sprayVFX.isPlaying&&other.CompareTag("Fire"))
        {
            print("Colliding with: "+ other.name);
            if(extinguishCoroutine == null)
                extinguishCoroutine = StartCoroutine(DelayCall(other.gameObject));
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Fire"))
        {
            if(extinguishCoroutine != null)
            {
                StopCoroutine(extinguishCoroutine);
                extinguishCoroutine = null;
            }
        }
    }
    public IEnumerator DelayCall(GameObject fireObj)
    {
        yield return new WaitForSeconds(extinguishTime);
        print("fire extinguished: "+fireObj.name);
        fireObj.GetComponentInParent<FireArea>().RemoveFireObject(fireObj);
        extinguishCoroutine = null;
    }
}
public class UseableItem : PickableItem
{
    public Sprite useSprite;
    void OnEnable()
    {
        OnPick+=OnPickDevice;
        OnDrop+=OnDropDevice;
    }
    void OnDisable()
    {
        OnPick-=OnPickDevice;
        OnDrop-=OnDropDevice;
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
