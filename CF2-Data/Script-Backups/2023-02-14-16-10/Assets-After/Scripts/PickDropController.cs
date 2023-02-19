using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickDropController : MonoBehaviour
{
    public static PickDropController instance;

    private void Awake()
    {
        instance = this;
    }


    [Header("Pickup Settings")]
    [SerializeField] Transform holdArea;

    public GameObject heldObj;
    private Rigidbody heldObjRB;

    [Header("Physics Parameters")]
    [SerializeField] private float pickupRange = 5.0f;
    [SerializeField] private float pickupForce = 150.0f;

    public GameObject pickObj;

    private void Update()
    {

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickupRange))
        {
            if (ControlFreak2.CF2Input.GetMouseButtonDown(0))
            {
                print(hit.transform.name);

                if (hit.transform.tag == "DoorBreak")
                {
                    if (GameManager.instance.selectedLevel == 8)
                    {
                        SoundManager.instance.DoorLock();

                        ObjectiveController.instance.UpdateTask(1);

                        GamePlayManager.instance.axeBlueGlow.Play();
                    }
                }

                if (hit.transform.tag == "Door")
                {
                   
                    if(hit.transform.eulerAngles.y == -90)
                    {
                        hit.transform.Rotate(0, 0, 0);

                        SoundManager.instance.DoorOpenClose();
                    }
                    else
                    {
                        hit.transform.Rotate(0, -90, 0);

                        SoundManager.instance.DoorOpenClose();
                    }

                    print("door");

                    GamePlayManager.instance.doorBell.Stop();
                }

                if (hit.transform.tag == "Fridge")
                {
                    if(hit.transform.eulerAngles.y == 0)
                    {
                        hit.transform.Rotate(-90, -90, -180);

                        SoundManager.instance.FridgeOpenDoor();

                        print("fridge open");
                    }
                    else
                    {
                        hit.transform.Rotate(-90, 0, -180);

                        SoundManager.instance.FridgeCloseDoor();
                    }
                }
            }


            //PickupObject
            if (heldObj == null)
            {
                pickObj = hit.transform.gameObject;

            }
            else
            {
                //DropObject
                //DropObject();
            }
        }


        if (heldObj != null)
        {
            //MoveObject
            MoveObject();
        }
    }


    void MoveObject()
    {
        if (Vector3.Distance(heldObj.transform.position, holdArea.position) > 0.1f)
        {
            Vector3 moveDirection = (holdArea.position - heldObj.transform.position);
            heldObjRB.AddForce(moveDirection * pickupForce);
        }
    }



    public void PickupObject()
    {
        SoundManager.instance.PickItem();
      
        if (pickObj.GetComponent<Rigidbody>())
        {
           
            heldObjRB = pickObj.GetComponent<Rigidbody>();
            heldObjRB.useGravity = false;
            heldObjRB.drag = 10;
            heldObjRB.constraints = RigidbodyConstraints.FreezeRotation;

            if(pickObj.transform.tag == "Baby")
            {
                
                BabyController.instance.transform.parent = holdArea;
            }
            else
            {
                heldObjRB.transform.parent = holdArea;
            }
            heldObj = pickObj;


            if (pickObj.tag == "Baby")
            {
                if (GameManager.instance.selectedLevel == 1)
                {
                    GamePlayManager.instance.cradleGreenGlow.Play();
                    ObjectiveController.instance.UpdateTask(1);
                    BabyController.instance.babyCry.Stop();
                }
                if (GameManager.instance.selectedLevel == 3)
                {
                 
                    BabyController.instance.babyCry.Stop();
                    GamePlayManager.instance.washPointGreenGlow.Play();
                }
                if(GameManager.instance.selectedLevel == 6)
                {
                    BabyController.instance.babyCry.Stop();
                    GamePlayManager.instance.cradleGreenGlow.Play();
                    GamePlayManager.instance.cradleSoundTrigger.SetActive(false);
                }
                if (GameManager.instance.selectedLevel == 8)
                {
                    BabyController.instance.babyCry.Stop();
                  
                    GamePlayManager.instance.doorTrigger.SetActive(true);
                }


                BabyController.instance.BabyAnim.SetBool("Fly", true);
                
               

                holdArea.localPosition = new Vector3(0.5f, -0.3f, 1);
                holdArea.localEulerAngles = new Vector3(0,200,0);          

                holdArea.GetChild(0).localPosition = new Vector3(0, 0, 0);
                holdArea.GetChild(0).localEulerAngles = new Vector3(0, 0, 0);

            }


            if (pickObj.tag == "Feeder" && GameManager.instance.selectedLevel == 2)
            {
                GamePlayManager.instance.feederBlueGlow.Stop();
               

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.3f, 0, 0.8f);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }


            if (pickObj.tag == "Facewash" && GameManager.instance.selectedLevel == 3)
            {
                GamePlayManager.instance.facewashGlow.Stop();

                holdArea.localPosition = new Vector3(0.3f, 0, 0.8f);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (pickObj.tag == "Shirt" && GameManager.instance.selectedLevel == 4)
            {
                GamePlayManager.instance.shirtBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.3f, 0, 1f);
                holdArea.localEulerAngles = new Vector3(0, -90, 60);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (pickObj.tag == "Toy" && GameManager.instance.selectedLevel == 5)
            {
                GamePlayManager.instance.toyBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.5f, 0, 1.5f);
                holdArea.localEulerAngles = new Vector3(0, -90, 40);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if(pickObj.tag == "Cylinder" && GameManager.instance.selectedLevel == 7)
            {
                GamePlayManager.instance.cylinderBlueGlow.Stop();
                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.5f, -0.4f, 1f);
                holdArea.localEulerAngles = new Vector3(-90, 0, -120);

                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (pickObj.tag == "Axe" && GameManager.instance.selectedLevel == 8)
            {
                GamePlayManager.instance.axeBlueGlow.Stop();

                holdArea.localPosition = new Vector3(0.3f, 0.3f, 1);
                holdArea.localEulerAngles = new Vector3(-90, -90, 0);

                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (pickObj.tag == "Talisman" && GameManager.instance.selectedLevel == 10)
            {
                GamePlayManager.instance.talismanBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);
                holdArea.localPosition = new Vector3(0.3f, 0, 0.4f);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }
        }
    }

    public void DropObject()
    {
        heldObjRB.useGravity = true;
        heldObjRB.drag = 1;
        heldObjRB.constraints = RigidbodyConstraints.None;

        heldObj.transform.parent = null;
        heldObj = null;

        heldObjRB.AddForce(transform.forward * 10, ForceMode.Impulse);



        
    }
}
