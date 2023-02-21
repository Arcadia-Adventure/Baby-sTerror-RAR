using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager instance;

    private void Awake()
    {
        instance = this;
    }


    public Transform[] playerSpawnPoint;
    public GameObject player;

    public GameObject[] levelObjects;

    public AudioSource doorBell;

    public Transform[] BabySpawnPoint;
    public GameObject baby;

    public AudioSource babyCryingCradle;
    public GameObject doorTrigger;

    public GameObject hoshBanda;

    public GameObject[] flyingFurniture;

    public ParticleSystem cradleGreenGlow;
    public ParticleSystem washPointGreenGlow;

    public ParticleSystem feederBlueGlow;
    public ParticleSystem shirtBlueGlow;
    public ParticleSystem axeBlueGlow;
    public ParticleSystem toyBlueGlow;
    public ParticleSystem cylinderBlueGlow;
    public ParticleSystem talismanBlueGlow;
    public ParticleSystem facewashGlow;

    public AudioSource RainBG;

    public GameObject cradleSoundTrigger;

    public DoorController babyRoomDoor;
    public AudioSource doorLock;

    public GameObject[] babyDropSpwanPoint;


    private void Start()
    {
        RainBG.volume = 0.2f;
        RainBG.Play();
        SoundManager.instance.BG.Stop();

        player.transform.position = playerSpawnPoint[GameManager.instance.selectedLevel - 1].position;    
        player.transform.rotation = playerSpawnPoint[GameManager.instance.selectedLevel - 1].rotation;

        baby.transform.position = BabySpawnPoint[GameManager.instance.selectedLevel - 1].position;
        baby.transform.rotation = BabySpawnPoint[GameManager.instance.selectedLevel - 1].rotation;

        levelObjects[GameManager.instance.selectedLevel - 1].SetActive(true);

       

        BabyController.instance.babyCry.Play();

        if (GameManager.instance.selectedLevel == 1)
        {   
            doorBell.Play();
            BabyController.instance.BabyAnim.SetBool("Cry", true);
        }

        if (GameManager.instance.selectedLevel == 2)
        {
            BabyController.instance.BabyAnim.SetBool("Sit", true);
            baby.tag = "Untagged";
        }

        if (GameManager.instance.selectedLevel == 3)
        {
            BabyController.instance.BabyAnim.SetBool("Sit", true);
            BabyController.instance.babyDirtyFace.SetActive(true);
        }

        if (GameManager.instance.selectedLevel == 4)
        {
            BabyController.instance.BabyAnim.SetBool("Sit", true);
            baby.tag = "Untagged";
        }

        if (GameManager.instance.selectedLevel == 5)
        {
            BabyController.instance.BabyAnim.SetBool("Sit", true);
            baby.tag = "Untagged";
        }

        if (GameManager.instance.selectedLevel == 6)
        { 
            babyCryingCradle.Play();
            baby.SetActive(false);
        }

        if (GameManager.instance.selectedLevel == 7)
        {
            BabyController.instance.BabyAnim.SetBool("Sit", true);
            baby.tag = "Untagged";
        }



        if (GameManager.instance.selectedLevel == 8)
        {
            babyRoomDoor.isDoorLock = true;
            BabyController.instance.babyCry.Play();
        }


        if(GameManager.instance.selectedLevel == 9)
        {
            BabyController.instance.babyEyesRed.color = Color.red;

            BabyController.instance.BabyAnim.SetBool("AngrySit", true);

            BabyController.instance.babyAngryVoice.Play();
            BabyController.instance.babyCry.Stop();

            baby.GetComponent<Rigidbody>().isKinematic = true;
            baby.GetComponent<Rigidbody>().useGravity = false;

            baby.tag = "Untagged";

        }
        else
        {
            BabyController.instance.babyEyesRed.color = Color.white;
        }

        if(GameManager.instance.selectedLevel == 10)
        {
            StartCoroutine(HoshBandaOff());

            for (int i = 0; i < flyingFurniture.Length; i++)
            {
                flyingFurniture[i].GetComponent<Rigidbody>().useGravity = false;
                flyingFurniture[i].GetComponent<Rigidbody>().AddForce(10, 10, 10);
            }

            /* foreach (var item in flyingFurniture)
             {
                 item.GetComponent<Rigidbody>().isKinematic = false;
             }*/

            baby.tag = "Untagged";

            BabyController.instance.babyEyesRed.color = Color.red;

            BabyController.instance.BabyAnim.SetBool("Fly", true);

            BabyController.instance.babyAngryVoice.Play();
            BabyController.instance.babyCry.Stop();

            baby.GetComponent<Rigidbody>().isKinematic = true;
            baby.GetComponent<Rigidbody>().useGravity = false;

        }



    }



    public void LevelComplete()
    {
        UIManager.instance.LvlCompleteON();

        int currentPlayerPrefs = PlayerPrefs.GetInt("totalUnlockLevel");

        PlayerPrefs.SetInt("totalUnlockLevel", currentPlayerPrefs+1);
    }


    IEnumerator HoshBandaOff()
    {
        hoshBanda.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        hoshBanda.SetActive(false);
    }

    public void GlowOn()
    {
       feederBlueGlow.Play();
       shirtBlueGlow.Play();
       axeBlueGlow.Play();
        toyBlueGlow.Play();
        cylinderBlueGlow.Play();
        talismanBlueGlow.Play();
        facewashGlow.Play();
    }
         

}
