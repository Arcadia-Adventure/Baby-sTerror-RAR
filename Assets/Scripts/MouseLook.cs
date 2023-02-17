using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{

    public float mouseSensivity = 100f;

    public Transform playerBody;

    float xRotation = 0;

    // Start is called before the first frame update
    void Start()
    {
        ControlFreak2.CFCursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = ControlFreak2.CF2Input.GetAxis("Mouse X") * mouseSensivity * Time.deltaTime;
        float mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y") * mouseSensivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
