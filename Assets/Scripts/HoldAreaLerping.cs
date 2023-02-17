using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldAreaLerping : MonoBehaviour
{
    public float lerpSpeed;
    public Transform holdPoint;
    public Vector3 velocity = Vector3.zero;
    public Transform cam;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //transform.position = Vector3.SmoothDamp(transform.position, holdPoint.position,ref velocity ,lerpSpeed);
        transform.position = Vector3.Lerp(transform.position, holdPoint.position, 1 - Mathf.Exp(-20 * Time.deltaTime));
        transform.rotation = holdPoint.rotation;

    }
}
