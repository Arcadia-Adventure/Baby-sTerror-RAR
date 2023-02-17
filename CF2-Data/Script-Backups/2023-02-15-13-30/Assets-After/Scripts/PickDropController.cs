using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    public DoorController doorController;

    public void DoorOpenCloaeBtn()
    {
        doorController.DoorOpenClose();

        GamePlayManager.instance.doorBell.Stop();
    }

    private void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickupRange))
        {

            if (hit.transform.tag == "Door")
            {
                print("dsafasdf");
                //ONOFF Button UI
                UIManager.instance.doorOpenCloseBtn.SetActive(true);
                UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorImage;
                UIManager.instance.door.DOFade(1, 1);

                if (hit.transform.GetComponent<DoorController>().isDoor == false)
                {
                    UIManager.instance.detectionTxt.text = "Open Door";
                  
                    UIManager.instance.crossHairDetection.DOFade(1, 1);
                }
                else
                {
                    UIManager.instance.detectionTxt.text = "Close Door";
                    //UIManager.instance.crossHairDetection.DOFade(0, 1);
                   
                }

            }
            else
            {
                //UIManager.instance.doorOpenCloseBtn.SetActive(false);
                UIManager.instance.door.DOFade(0, 1);/*.OnComplete(() => 
                {
                    UIManager.instance.doorOpenCloseBtn.SetActive(false);
                });*/

                UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;

                UIManager.instance.rt.sizeDelta = new Vector2(20, 20);

                UIManager.instance.detectionTxt.text = null;
            }

            
            if (ControlFreak2.CF2Input.GetMouseButtonDown(1) )
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

                    hit.transform.GetComponent<DoorController>().DoorOpenClose();

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
              
               
                /*if (hit.transform.tag == "Fridge")
                {
                    UIManager.instance.doorOpenCloseBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorImage;
                    UIManager.instance.detectionTxt.text = "Open Fridge";
                }*/
                 if (hit.transform.tag == "Baby")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);

                  
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;

                    UIManager.instance.crossHairDetection.DOFade(1, 1);
                    UIManager.instance.pick.DOFade(1, 1);

                    UIManager.instance.detectionTxt.text = "Pick Baby";
                }
                else if (hit.transform.tag == "Feeder")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Feeder";
                }
                else if (hit.transform.tag == "Facewash")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Facewash";
                }
                else if (hit.transform.tag == "Shirt")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Shirt";
                }
                else if (hit.transform.tag == "Toy")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Toy";
                }
                else if (hit.transform.tag == "Cylinder")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Fire Extinguish";
                }
                else if (hit.transform.tag == "Axe")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Axe";

                }
                else if (hit.transform.tag == "Talisman")
                {
                    UIManager.instance.pickBtn.SetActive(true);

                    UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
                    UIManager.instance.detectionTxt.text = "Pick Talisman";
                }
/*                else if(false)
                {
                   
                    UIManager.instance.pick.DOFade(0, 1);
                    //UIManager.instance.pickBtn.SetActive(false);

                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;

                    UIManager.instance.rt.sizeDelta = new Vector2(20, 20);


                   
                    UIManager.instance.dropBtn.SetActive(false);
                   
                    UIManager.instance.detectionTxt.text = null;
                }*/

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
