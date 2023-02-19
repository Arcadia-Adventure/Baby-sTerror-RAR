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

    [Header("Pickup Settings")]
    [SerializeField] Transform holdArea;

    public GameObject heldObj;
    private Rigidbody heldObjRB;

    [Header("Physics Parameters")]
    [SerializeField] private float pickupRange = 5.0f;
    [SerializeField] private float pickupForce = 150.0f;

    public GameObject detectObj;
    public DoorController doorController;

    public void DoorOpenCloaeBtn()
    {
        doorController.DoorOpenClose();
        GamePlayManager.instance.doorBell.Stop();
    }
    public void DetectItemsPickUI()
    {
        UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
        UIManager.instance.crossHairDetection.sprite = UIManager.instance.pickImage;
        UIManager.instance.crossHairDetection.DOFade(1, 1);
    }

    public void DetectItemsDropUI()
    {
        UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
        UIManager.instance.crossHairDetection.sprite = UIManager.instance.dropImage;
        UIManager.instance.crossHairDetection.DOFade(1, 1);
    }

    public void Drop()
    {
        if (heldObj != null)
        {
            DetectItemsPickUI();
            DropObject();
            SoundManager.instance.DropItem();
            GamePlayManager.instance.GlowOn();
            print("drop");


            if (GameManager.instance.selectedLevel == 2)
            {
                GamePlayManager.instance.baby.tag = "Untagged";
            }


            if (prefabe)
            {

                if(GameManager.instance.selectedLevel == 1)
                {
                    PrefabeInstantLvl1();
                }

                if (GameManager.instance.selectedLevel == 6)
                {
                    PrefabeInstantLvl6();
                }


                var b = GameObject.FindWithTag("Baby");
                Destroy(b);


                BabyController.instance.BabyAnim.SetBool("Happy", true);
                GamePlayManager.instance.cradleGreenGlow.Stop();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                StartCoroutine(BabyController.instance.LevelComplete());

                print("cradle detection");
            }

            if (feeder)
            {
                var f = GameObject.FindGameObjectWithTag("Feeder");
                Destroy(f);


                GamePlayManager.instance.baby.tag = "Untagged";


                BabyController.instance.babyCry.Stop();
                SoundManager.instance.BabyHappy();
                BabyController.instance.babyBlueGlow.Play();
                ObjectiveController.instance.UpdateTask(2);
                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                StartCoroutine(BabyController.instance.LevelComplete());

                print("feeder detection");
            }

            if (washPoint)
            {
                PrefabeInstantLvl3();

                var b = GameObject.FindWithTag("Baby");
                Destroy(b);

                ObjectiveController.instance.UpdateTask(1);

                BabyController.instance.BabyAnim.SetBool("Fly", false);
                BabyController.instance.BabyAnim.SetBool("Sit", true);

                GamePlayManager.instance.washPointGreenGlow.Stop();

                print("Sit with cry");

                //GamePlayManager.instance.facewashGlow.Play();


            }


            if (faceWash)
            {
                var b = GameObject.FindWithTag("Facewash");
                Destroy(b);

                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();

                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                babyposLvl3.GetComponent<AudioSource>().enabled = false;
                babyposLvl3.tag = "Untagged";

                BabyController.instance.babyDirtyFace.SetActive(false);

                StartCoroutine(BabyController.instance.LevelComplete());
            }

            if (shirt)
            {

                var s = GameObject.FindWithTag("Shirt");
                Destroy(s);

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

                StartCoroutine(BabyController.instance.LevelComplete());
            }


            if (toy)
            {
                var t = GameObject.FindWithTag("Toy");
                Destroy(t);

                GamePlayManager.instance.baby.tag = "Untagged";

                BabyController.instance.babyCry.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);


                StartCoroutine(BabyController.instance.LevelComplete());
            }

            if (fire)
            {
                var pump = GameObject.FindWithTag("Fire Extinguisher");
                Destroy(pump);

                var f = GameObject.FindGameObjectsWithTag("Fire");
                for (int i = 0; i < f.Length; i++)
                {
                    f[i].gameObject.SetActive(false);
                }

                GamePlayManager.instance.baby.tag = "Untagged";

                BabyController.instance.babyCry.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("Sit", false);

                StartCoroutine(BabyController.instance.LevelComplete());
            }

            if (talisman)
            {

                BabyController.instance.babyAngryVoice.Stop();
                BabyController.instance.babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyController.instance.BabyAnim.SetBool("Happy", true);
                BabyController.instance.BabyAnim.SetBool("AngryFly", false);

                BabyController.instance.babyEyesRed.color = Color.white;

                GamePlayManager.instance.baby.tag = "Untagged";

                StartCoroutine(BabyController.instance.LevelComplete());
            }

        }
    }

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



        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickupRange))
        {

            // door button click
            if (ControlFreak2.CF2Input.GetMouseButtonDown(1))
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



                if (hit.transform.tag == "Door" || hit.transform.tag == "Fridge")
                {
                    print("door open and cloase");
                    hit.transform.GetComponent<DoorController>().DoorOpenClose();
                    GamePlayManager.instance.doorBell.Stop();

                    SoundManager.instance.doorOpenClose.Play();
                }
            }




            // set door button UI 
            if (hit.transform.tag == "Door" || hit.transform.tag == "Fridge")
            {
                print("detection");

                UIManager.instance.rt.sizeDelta = new Vector2(50, 50);
                UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorOpenImage;


                if (hit.transform.GetComponent<DoorController>().isDoor == false)
                {
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorOpenImage;
                    UIManager.instance.detectionTxt.text = "Open " + hit.transform.tag;
                    UIManager.instance.crossHairDetection.DOFade(1, 1);

                    UIManager.instance.door.SetSprite(UIManager.instance.doorOpenImage);
                    BtnFade(UIManager.instance.door, true);
                }
                else
                {
                    UIManager.instance.crossHairDetection.sprite = UIManager.instance.doorCloseImage;
                    UIManager.instance.detectionTxt.text = "Close  " + hit.transform.tag;

                    UIManager.instance.door.SetSprite(UIManager.instance.doorCloseImage);
                    BtnFade(UIManager.instance.door, true);
                }
            }
            else
            {
                UIManager.instance.crossHairDetection.sprite = UIManager.instance.knobImage;
                UIManager.instance.rt.sizeDelta = new Vector2(20, 20);
                UIManager.instance.detectionTxt.text = null;
                BtnFade(UIManager.instance.door, false);
            }



            // set pick item UI detection
            string tag = hit.transform.tag;

            if (tag == "Baby" ||
                 tag == "Feeder" ||
                 tag == "Facewash" ||
                 tag == "Shirt" ||
                 tag == "Toy" ||
                 tag == "Fire Extinguisher" ||
                 tag == "Axe" ||
                 tag == "Talisman"
                )
            {
                DetectItemsPickUI();
                BtnFade(UIManager.instance.pick, true);
                UIManager.instance.pick.SetSprite(UIManager.instance.pickImage);
                UIManager.instance.detectionTxt.text = "Pick " + hit.transform.tag;
            }
            else if (tag != "Door")
            {
                if (tag != "Fridge")
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
                }
            }



            // set drop item UI detection
            if (heldObj != null)
            {


                if ((GameManager.instance.selectedLevel == 1) || (GameManager.instance.selectedLevel == 6))
                {
                    if (hit.transform.tag == "Cradle")
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



                if (hit.transform.tag == "Baby" && (GameManager.instance.selectedLevel == 2))
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

                if (hit.transform.tag == "WashPoint" && (GameManager.instance.selectedLevel == 3))
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

                if (hit.transform.tag == "Baby" && (GameManager.instance.selectedLevel == 3))
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

                if (hit.transform.tag == "Baby" && (GameManager.instance.selectedLevel == 4))
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

                if (hit.transform.tag == "Baby" && (GameManager.instance.selectedLevel == 5))
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

                if (hit.transform.tag == "Fire" && (GameManager.instance.selectedLevel == 7))
                {
                    DetectItemsDropUI();
                    BtnFade(UIManager.instance.pick, true);
                    UIManager.instance.pick.SetSprite(UIManager.instance.dropImage);
                    UIManager.instance.detectionTxt.text = "Drop Talisman";

                    fire = true;
                }
                else
                {
                    fire = false;
                }

                if (hit.transform.tag == "Baby" && (GameManager.instance.selectedLevel == 10))
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
    public bool fire;
    public bool talisman;




    public void PrefabeInstantLvl1()
    {
        var babyPos = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyPos.transform.position = GamePlayManager.instance.babyDropSpwanPoint[0].transform.position;

        babyPos.tag = "Untagged";
        babyPos.GetComponent<AudioSource>().enabled = false;
        print(babyPos);
    }

    GameObject babyposLvl3;
    public void PrefabeInstantLvl3()
    {
        babyposLvl3 = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyposLvl3.transform.position = GamePlayManager.instance.babyDropSpwanPoint[1].transform.position;

        babyposLvl3.tag = "Untagged";

        print(babyposLvl3);
    }

    public void PrefabeInstantLvl6()
    {
        var babyPos = Instantiate(GamePlayManager.instance.baby, transform.position, Quaternion.identity);
        babyPos.transform.position = GamePlayManager.instance.babyDropSpwanPoint[2].transform.position;

        babyPos.tag = "Untagged";
        babyPos.GetComponent<AudioSource>().enabled = false;
        print(babyPos);
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

        if (detectObj.GetComponent<Rigidbody>())
        {
            heldObjRB = detectObj.GetComponent<Rigidbody>();
            heldObjRB.useGravity = false;
            heldObjRB.drag = 10;
            heldObjRB.constraints = RigidbodyConstraints.FreezeRotation;

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

                holdArea.localPosition = new Vector3(0.3f, 0.3f, 1);
                holdArea.localEulerAngles = new Vector3(-90, -90, 0);

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
        heldObjRB.drag = 1;
        heldObjRB.constraints = RigidbodyConstraints.None;
        heldObj.transform.parent = null;
        heldObj = null;
        heldObjRB.AddForce(transform.forward * 10, ForceMode.Impulse);
    }
}
