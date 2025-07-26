using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ControlFreak2;
public class PickDropController : MonoBehaviour
{
    public static PickDropController instance;

    private void Awake()
    {
        instance = this;
    }

    public LayerMask detectionLayers;

    [Header("Pickup Settings")]
    [SerializeField] Transform holdArea;

    public GameObject heldObj;
    private Rigidbody heldObjRB;

    [Header("Physics Parameters")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private float pickupForce = 150.0f;

    public GameObject detectObj;
    public DoorController doorController;
    public FirstPersonController fpc;

    public void DoorOpenCloseBtn()
    {
        doorController.DoorOpenClose();
        GamePlayManager.instance.doorBell.Stop();
    }

    public void DetectItemsPickUI()
    {
        UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
        UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
        UIManager.instance.crossHairDetection.DOFade(1, 1);

        fpc.enableZoom = true;
        fpc.holdToZoom = true;
        fpc.isZoomed = true;
    }

    public void DetectItemsDropUI()
    {
        UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
        UIManager.instance.crossHairDetection.sprite = UIManager.instance.dropImage;
        UIManager.instance.crossHairDetection.DOFade(1, 1);

      
        fpc.holdToZoom = false;
        fpc.isZoomed = false;
    }

   
    public void Drop()
    {
        if (heldObj != null)
        {
            fpc.holdToZoom = false;
            fpc.isZoomed = false;


            DetectItemsPickUI();
           
            SoundManager.instance.DropItem();
            GamePlayManager.instance.GlowOn();
            //print("drop");

            //DropObject();



            if (doorLock)
            {
                heldObjRB.GetComponent<DOTweenAnimation>().DORestart();

                this.transform.DOShakePosition(0.5f,1, 10, 30);

                SoundManager.instance.drop.Stop();


                print("hitting axe");

            }
            else
            {
                DropObject();
            }

            if (GameManager.instance.selectedLevel == 2)
            {
                GamePlayManager.instance.baby.tag = "Untagged";
            }
           

            if (prefabe)
            {

                Destroy(BabyController.instance.gameObject);

                if (GameManager.instance.selectedLevel == 1)
                {
                    PrefabeInstantLvl1();

                    babyPosLvl1.GetComponent<Animator>().enabled = true;
                    babyPosLvl1.GetComponent<Animator>().SetBool("Happy", true);
                }

                if (GameManager.instance.selectedLevel == 6)
                {
                    PrefabeInstantLvl6();

                    babyPosLvl6.GetComponent<Animator>().enabled = true;
                    babyPosLvl6.GetComponent<Animator>().SetBool("Happy", true);
                }

                if(GameManager.instance.selectedLevel == 10)
                {
                    PrefabeInstantLvl1();

                    babyPosLvl1.GetComponent<Animator>().enabled = true;
                    babyPosLvl1.GetComponent<Animator>().SetBool("Happy", true);


                    ObjectiveController.instance.UpdateTask(3);
                    SoundManager.instance.BabyHappy();

                    GamePlayManager.instance.cradleGreenGlow.Stop();
                    GamePlayManager.instance.baby.tag = "Untagged";

                    StartCoroutine(SoundManager.instance.LevelComplete());
                }

                //BabyController.instance.BabyAnim.SetBool("Happy", true);



                GamePlayManager.instance.cradleGreenGlow.Stop();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                StartCoroutine(SoundManager.instance.LevelComplete());

                //print("cradle detection");

            }


            if (feeder)
            {
                
                Destroy(Items.instance.feeder);


                GamePlayManager.instance.baby.tag = "Untagged";


                BabyController.instance.babyCry.Stop();
                SoundManager.instance.BabyHappy();
                BabyController.instance.babyBlueGlow.Play();
                ObjectiveController.instance.UpdateTask(2);
                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                StartCoroutine(SoundManager.instance.LevelComplete());

                print("feeder detection");
            }
           

            if (washPoint)
            {
              
                Destroy(BabyController.instance.gameObject);

                PrefabeInstantLvl3();

                babyposLvl3.GetComponent<Animator>().enabled = true;
                babyposLvl3.GetComponent<Animator>().SetBool("Fly", false); 
                babyposLvl3.GetComponent<Animator>().SetBool("Sit", true);


                babyposLvl3.GetComponent<AudioSource>().enabled = true;


                /*  BabyController.instance.BabyAnim.SetBool("Fly", false);
                  BabyController.instance.BabyAnim.SetBool("Sit", true);*/

                GamePlayManager.instance.washPointGreenGlow.Stop();
                ObjectiveController.instance.UpdateTask(1);

                print("Sit with cry");

                //GamePlayManager.instance.facewashGlow.Play();
            }
           


            if (faceWash)
            {
              
                Destroy(Items.instance.facewash);

                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();

                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                babyposLvl3.GetComponent<AudioSource>().enabled = false;
                babyposLvl3.tag = "Untagged";

                BabyController.instance.babyDirtyFace.SetActive(false);

                StartCoroutine(SoundManager.instance.LevelComplete());
            }
           

            if (shirt)
            {

                Destroy(Items.instance.dress);

                GamePlayManager.instance.baby.tag = "Untagged";

                BabyController.instance.babyCry.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                BabyController.instance.body.SetActive(false);
                BabyController.instance.diaper.SetActive(false);
                BabyController.instance.clothBody.SetActive(true);
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                StartCoroutine(SoundManager.instance.LevelComplete());
            }
            
          


            if (toy)
            {
                
                Destroy(Items.instance.toy);

                GamePlayManager.instance.baby.tag = "Untagged";

                BabyController.instance.babyCry.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);


                StartCoroutine(SoundManager.instance.LevelComplete());
            }
           






            if (fire) // lvl7
            {

                Destroy(Items.instance.fireCylinder);
                Destroy(Items.instance.fireLvl7);

                GamePlayManager.instance.baby.tag = "Untagged";

                BabyController.instance.babyCry.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                StartCoroutine(SoundManager.instance.LevelComplete());
            }
          

            if (talisman)
            {

                //Destroy(Items.instance.telisman);
                Items.instance.telisman.SetActive(false);

                BabyController.instance.BabyAnim.SetBool("Fly", false);

                //BabyController.instance.BabyAnim.enabled = false;
                BabyController.instance.GetComponent<Rigidbody>().isKinematic = false;
                BabyController.instance.GetComponent<Rigidbody>().useGravity = true;

                Destroy(Items.instance.fireLvl10);

                BabyController.instance.babyAngryVoice.Stop();

                BabyController.instance.babyBlueGlow.Play();

                BabyController.instance.babyEyesRed.color = Color.white;

                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                SoundManager.instance.BabyHappy();

                GamePlayManager.instance.cradleGreenGlow.Play();
                //GamePlayManager.instance.baby.tag = "Untagged";


               
                //StartCoroutine(BabyController.instance.LevelComplete());
            }        
        }
    }

    RaycastHit hit;
    public string objTag;
    private void Update()
    {
        if (ControlFreak2.CF2Input.GetKeyDown(KeyCode.P))
        {
            if (heldObj != null)
            {
                Drop();
            }
            else
            {
                PickupObject();
            }
        }



        //RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickupRange, detectionLayers))
        {
            objTag = hit.transform.tag;
            // door button click
            if (ControlFreak2.CF2Input.GetMouseButtonDown(1))
            {

                //print(hit.transform.name);
               

                if (objTag == "Door" || objTag == "Fridge")
                {
                    //print("door open and close");
                    hit.transform.GetComponent<DoorController>().DoorOpenClose();
                    GamePlayManager.instance.doorBell.Stop();



                    if ((GameManager.instance.selectedLevel == 8) && hit.transform.GetComponent<DoorController>().isDoorLock == true)
                    {
                        ObjectiveController.instance.UpdateTask(1);

                        GamePlayManager.instance.axeBlueGlow.transform.parent.tag = "Axe";

                    }

                }
            }




            // set door button UI 
            if (objTag=="Door" || objTag=="Fridge")
            {
              

                UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorOpenImage;


                if (hit.transform.GetComponent<DoorController>().isDoor == false)
                {
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorOpenImage;
                    UIManager.instance.detectionTxt.text = "Open " + objTag;
                    UIManager.instance.crossHairDetection.DOFade(1, 1);

                    UIManager.instance.door.SetSprite(UIManager.instance.doorOpenImage);
                    BtnFade(UIManager.instance.door, true);


                    if ((GameManager.instance.selectedLevel == 8) && hit.transform.GetComponent<DoorController>().isDoorLock == true)
                    {
                        UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorOpenImage;
                        UIManager.instance.detectionTxt.text = "Door Lock";
                        UIManager.instance.crossHairDetection.DOFade(1, 1);

                        UIManager.instance.door.SetSprite(UIManager.instance.doorOpenImage);
                        BtnFade(UIManager.instance.door, true);
                       
                        GamePlayManager.instance.axeBlueGlow.Play();
                    }
                }
                else
                {
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorCloseImage;
                    UIManager.instance.detectionTxt.text = "Close  " + objTag;

                    UIManager.instance.door.SetSprite(UIManager.instance.doorCloseImage);
                    BtnFade(UIManager.instance.door, true);
                }
            }
           /* else
            {
                UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;
                UIManager.instance.rt.sizeDelta = new Vector2(20, 20);
                UIManager.instance.detectionTxt.text = null;
                BtnFade(UIManager.instance.door, false);
            }*/



            // set pick item UI detection
           // string objTag = objTag;

            if (objTag == "Baby" ||
                 objTag == "Feeder" ||
                 objTag == "Facewash" ||
                 objTag == "Shirt" ||
                 objTag == "Toy" ||
                 objTag == "Fire Extinguisher" ||
                 objTag == "Axe" ||
                 objTag == "Talisman"
                )
            {
                DetectItemsPickUI();
                BtnFade(UIManager.instance.pick, true);
                UIManager.instance.pick.SetSprite(UIManager.instance.pickImage);
                UIManager.instance.detectionTxt.text = "Pick " + objTag;

             

            }
            else if (objTag != "Door")
            {
                if (objTag != "Fridge")
                {
                    if (heldObj != null)
                    {
                        UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    }
                    else
                    {
                        BtnFade(UIManager.instance.pick, false);
                    }
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;
                    UIManager.instance.rt.sizeDelta = new Vector2(20, 20);
                    UIManager.instance.detectionTxt.text = null;



                    //fpc.enableZoom = false;
                    fpc.holdToZoom = false;
                    fpc.isZoomed = false;
                }
            }



            // set drop item UI detection
            if (heldObj != null)
            {


                if ((GameManager.instance.selectedLevel == 1) || (GameManager.instance.selectedLevel == 6) || (GameManager.instance.selectedLevel == 10))
                {
                    if (objTag == "Cradle")
                    {
                        DetectItemsDropUI();
                        BtnFade(UIManager.instance.pick, true);
                        UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                        UIManager.instance.detectionTxt.text = "Drop Baby";

                        prefabe = true;
                    }
                    else
                    {
                        prefabe = false;
                    }
                }



                if (objTag == "Baby" && (GameManager.instance.selectedLevel == 2))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Feeder";

                    feeder = true;
                }
                else
                {
                    feeder = false;
                }

                if (objTag == "WashPoint" && (GameManager.instance.selectedLevel == 3))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Baby";

                    washPoint = true;
                }
                else
                {
                    washPoint = false;
                }

                if (objTag == "Baby" && (GameManager.instance.selectedLevel == 3))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Facewash";

                    faceWash = true;
                }
                else
                {
                    faceWash = false;
                }

                if (objTag == "Baby" && (GameManager.instance.selectedLevel == 4))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Shirt";

                    shirt = true;
                }
                else
                {
                    shirt = false;
                }

                if (objTag == "Baby" && (GameManager.instance.selectedLevel == 5))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Toy";

                    toy = true;
                }
                else
                {
                    toy = false;
                }

                if (objTag == "Fire" && (GameManager.instance.selectedLevel == 7))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Fire Extinguisher";

                    fire = true;
                }
                else
                {
                    fire = false;
                }


                if (objTag == "Door" && (GameManager.instance.selectedLevel == 8))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Door Break";

                    doorLock = true;

                    if (GamePlayManager.instance.babyRoomDoor.isDoorLock == false)
                    {
                        UIManager.instance.detectionTxt.text = "Door Close";

                        BtnFade(UIManager.instance.pick, false);
                    }

                }
                else
                {
                    doorLock = false;
                }



                if (objTag == "Baby" && (GameManager.instance.selectedLevel == 10))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Talisman";

                    talisman = true;
                }
                else
                {
                    talisman = false;
                }


            }



            //PickupObject
            if (heldObj == null)
            {
                detectObj = hit.transform.gameObject;
            }
        }
        else
        {
            UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;
            UIManager.instance.rt.sizeDelta = new Vector2(20, 20);
            UIManager.instance.detectionTxt.text = null;
            BtnFade(UIManager.instance.door, false);

            if (heldObj == null)
            {
                BtnFade(UIManager.instance.pick, false); // drop image

            }

            fpc.holdToZoom = false;
            fpc.isZoomed = false;

        }

        if (heldObj != null)
        {
            //MoveObject
            //MoveObject();
        }
    }
    private void FixedUpdate()
    {
        if (heldObj != null)
        {
            //MoveObject
            MoveObject();
        }
    }



    public bool prefabe;
    public bool feeder;
    public bool washPoint;
    public bool faceWash;
    public bool shirt;
    public bool toy;
    public bool doorLock;
    public bool fire;
    public bool talisman;



    GameObject babyPosLvl1;
    public void PrefabeInstantLvl1()
    {
        babyPosLvl1 = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyPosLvl1.transform.position = GamePlayManager.instance.babyDropSpwanPoint[0].transform.position;

        babyPosLvl1.tag = "Untagged";
        babyPosLvl1.GetComponent<AudioSource>().enabled = false;
        //print(babyPosLvl1);
    }

    GameObject babyposLvl3;
    public void PrefabeInstantLvl3()
    {
        babyposLvl3 = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyposLvl3.transform.position = GamePlayManager.instance.babyDropSpwanPoint[1].transform.position;

        babyposLvl3.tag = "Untagged";

        //print(babyposLvl3);
    }

    GameObject babyPosLvl6;
    public void PrefabeInstantLvl6()
    {
        babyPosLvl6 = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyPosLvl6.transform.position = GamePlayManager.instance.babyDropSpwanPoint[2].transform.position;

        babyPosLvl6.tag = "Untagged";
        babyPosLvl6.GetComponent<AudioSource>().enabled = false;
        //print(babyPosLvl6);
    }



    public void BtnFade(TouchButtonSpriteAnimator t, bool isFade)
    {
        if (isFade)
        {
            t.image.DOFade(1, 1);
            t.transform.parent.GetComponent<TouchButton>().enabled = true;
        }
        else
        {
            t.image.DOFade(0, 1);
            t.transform.parent.GetComponent<TouchButton>().enabled = false;
        }
    }
    Vector3 moveDirection;
    void MoveObject()
    {
            moveDirection = (holdArea.position - heldObj.transform.position);
            heldObjRB.AddForce(moveDirection * pickupForce,ForceMode.Force);
    }

    public void PickupObject()
    {
        SoundManager.instance.PickItem();

        if (detectObj.GetComponent<Rigidbody>())
        {
            heldObjRB = detectObj.GetComponent<Rigidbody>();
            
            heldObjRB.linearDamping = 10;
            heldObjRB.constraints = RigidbodyConstraints.FreezeRotation;
            heldObjRB.useGravity = true;
            heldObjRB.useGravity = false;

            if (detectObj.transform.tag == "Baby")
            {

                BabyController.instance.transform.parent = holdArea;
            }
            else
            {
                heldObjRB.transform.parent = holdArea;
            }

            heldObj = detectObj;


            if (detectObj.tag == "Baby")
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

                    BabyController.instance.BabyAnim.SetBool("Sit", false);

                }
                if (GameManager.instance.selectedLevel == 6)
                {
                    BabyController.instance.babyCry.Stop();
                    GamePlayManager.instance.cradleGreenGlow.Play();
                    GamePlayManager.instance.cradleSoundTrigger.SetActive(false);

                    BabyController.instance.BabyAnim.SetBool("Sit", false);
                }
                if (GameManager.instance.selectedLevel == 8)
                {
                    BabyController.instance.babyCry.Stop();

                    GamePlayManager.instance.doorTrigger.SetActive(true);

                    BabyController.instance.BabyAnim.SetBool("Sit", false);
                }

                if (GameManager.instance.selectedLevel == 10)
                {  
                    BabyController.instance.BabyAnim.SetBool("Happy", false);
                }

                BabyController.instance.BabyAnim.SetBool("Fly", true);


                holdArea.localPosition = new Vector3(0.5f, -0.3f, 1);
                holdArea.localEulerAngles = new Vector3(0, 200, 0);

                holdArea.GetChild(0).localPosition = new Vector3(0, 0, 0);
                holdArea.GetChild(0).localEulerAngles = new Vector3(0, 0, 0);
            }


            if (detectObj.tag == "Feeder" && GameManager.instance.selectedLevel == 2)
            {

               

                GamePlayManager.instance.feederBlueGlow.Stop();

                GamePlayManager.instance.baby.tag = "Baby";

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.3f, 0, 0.8f);
                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (detectObj.tag == "Facewash" && GameManager.instance.selectedLevel == 3)
            {
                GamePlayManager.instance.facewashGlow.Stop();

                holdArea.localPosition = new Vector3(0.3f, 0, 0.8f);
                heldObj.transform.localEulerAngles = Vector3.zero;

                var w = GameObject.FindGameObjectWithTag("WashPoint");
                w.tag = "Untagged";

                babyposLvl3.tag = "Baby";
            }

            if (detectObj.tag == "Shirt" && GameManager.instance.selectedLevel == 4)
            {
                GamePlayManager.instance.shirtBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.3f, 0, 1f);
                holdArea.localEulerAngles = new Vector3(0, -90, 60);
                heldObj.transform.localEulerAngles = Vector3.zero;

                GamePlayManager.instance.baby.tag = "Baby";
            }

            if (detectObj.tag == "Toy" && GameManager.instance.selectedLevel == 5)
            {
                GamePlayManager.instance.toyBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.5f, 0, 1.5f);
                holdArea.localEulerAngles = new Vector3(0, -90, 40);
                heldObj.transform.localEulerAngles = Vector3.zero;

                GamePlayManager.instance.baby.tag = "Baby";
            }

            if (detectObj.tag == "Fire Extinguisher" && GameManager.instance.selectedLevel == 7)
            {
                GamePlayManager.instance.cylinderBlueGlow.Stop();
                ObjectiveController.instance.UpdateTask(1);

                holdArea.localPosition = new Vector3(0.5f, -0.4f, 1f);
                holdArea.localEulerAngles = new Vector3(-90, 0, -120);

                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (detectObj.tag == "Axe" && GameManager.instance.selectedLevel == 8)
            {
                GamePlayManager.instance.axeBlueGlow.Stop();

                holdArea.localPosition = new Vector3(0.4f, -0.8f, 1f);
                holdArea.localEulerAngles = new Vector3(0, 0, 0);

                heldObj.transform.localEulerAngles = Vector3.zero;
            }

            if (detectObj.tag == "Talisman" && GameManager.instance.selectedLevel == 10)
            {
                GamePlayManager.instance.talismanBlueGlow.Stop();

                ObjectiveController.instance.UpdateTask(1);
                holdArea.localPosition = new Vector3(0.3f, 0, 0.4f);
                heldObj.transform.localEulerAngles = Vector3.zero;

                GamePlayManager.instance.baby.tag = "Baby";
            }

            handObjTag = detectObj.tag;
            heldObj.tag = "Untagged";
        }
    }

    public string handObjTag;

    public void DropObject()
    {
        heldObj.tag = handObjTag;
        heldObjRB.useGravity = true;
        heldObjRB.linearDamping = 1;
        heldObjRB.constraints = RigidbodyConstraints.None;
        heldObj.transform.parent = null;
        heldObj = null;
        heldObjRB.AddForce(transform.forward * 1, ForceMode.Impulse);
        heldObjRB.AddForce(transform.up * 2, ForceMode.Impulse);
    }
}
