using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnCollistionTriggerEvent : MonoBehaviour
{
    public UnityEvent onCollisionEnter;
    public UnityEvent onCollisionExit;
    public UnityEvent onCollisionStay;
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;
    public UnityEvent onTriggerStay;

    private void OnCollisionEnter(Collision collision)
    {
        onCollisionEnter?.Invoke();
    }

    private void OnCollisionExit(Collision collision)
    {
        onCollisionExit?.Invoke();
    }

    private void OnCollisionStay(Collision collision)
    {
        onCollisionStay?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        onTriggerStay?.Invoke();
    }
}
