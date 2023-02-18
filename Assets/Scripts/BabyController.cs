using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BabyController : MonoBehaviour
{
    public static BabyController instance;

    private void Awake()
    {
        instance = this;
    }

    public Animator BabyAnim;

    public AudioSource babyCry;

    public Transform facewashPoint;
    public Rigidbody rb;

    public GameObject diaper;
    public GameObject clothBody;
    public GameObject body;

    public Material babyEyesRed;
    public Transform babyNeck;
    public AudioSource babyAngryVoice;

    public ParticleSystem babyBlueGlow;


    private void Start()
    {
        if (GameManager.instance.selectedLevel > 4)
        {
            body.SetActive(false);
            diaper.SetActive(false);
            clothBody.SetActive(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Cradle")
        {
            if (GameManager.instance.selectedLevel == 1 || GameManager.instance.selectedLevel == 6)
            {

              
                BabyAnim.SetBool("Happy", true);
                GamePlayManager.instance.cradleGreenGlow.Stop();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                StartCoroutine("LevelComplete");
            }   
        }

        if (collision.gameObject.tag == "Feeder")
        {
            if (GameManager.instance.selectedLevel == 2)
            {
                babyCry.Stop();
                SoundManager.instance.BabyHappy();
                babyBlueGlow.Play();
                ObjectiveController.instance.UpdateTask(2);
                BabyAnim.SetBool("Happy", true);
                Destroy(collision.gameObject);

                StartCoroutine("LevelComplete");

                print("feeder collison");
            }
        }

        if (collision.gameObject.tag == "WashPoint")
        {
            if (GameManager.instance.selectedLevel == 3)
            {

                ObjectiveController.instance.UpdateTask(1);

                BabyAnim.SetBool("Fly", false);
                BabyAnim.SetBool("Cry", false);
                BabyAnim.SetBool("Sit", true);

                GamePlayManager.instance.washPointGreenGlow.Stop();

                print("Sit");

                rb.isKinematic = true;

                GamePlayManager.instance.facewashGlow.Play();

                transform.position = facewashPoint.position;
                transform.rotation = facewashPoint.rotation;
            }

        }

        if (collision.gameObject.tag == "Facewash")
        {
            if (GameManager.instance.selectedLevel == 3)
            {
              
                babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();

                ObjectiveController.instance.UpdateTask(2);

                BabyAnim.SetBool("Sit", false);
                BabyAnim.SetBool("Happy", true);

                Destroy(collision.gameObject);

                StartCoroutine("LevelComplete");
            }
        }

        if (collision.gameObject.tag == "Shirt")
        {
            if (GameManager.instance.selectedLevel == 4)
            {
                babyCry.Stop();
                babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                body.SetActive(false);
                diaper.SetActive(false);
                clothBody.SetActive(true);
                ObjectiveController.instance.UpdateTask(2);
                Destroy(collision.gameObject);

                BabyAnim.SetBool("Happy", true);

                StartCoroutine("LevelComplete");
            }
        }

        if (collision.gameObject.tag == "Toy")
        {
            if (GameManager.instance.selectedLevel == 5)
            {
                babyCry.Stop();
                babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);
                BabyAnim.SetBool("Happy", true);
                Destroy(collision.gameObject);

                StartCoroutine("LevelComplete");
            }
        }

        if (collision.gameObject.tag == "Talisman")
        {
            if (GameManager.instance.selectedLevel == 10)
            {
                babyCry.Stop();
                babyBlueGlow.Play();
                SoundManager.instance.BabyHappy();
                ObjectiveController.instance.UpdateTask(2);

                BabyAnim.SetBool("Happy",true);

                StartCoroutine("LevelComplete");
               
            }
        }
    }

    public IEnumerator LevelComplete()
    {
        yield return new WaitForSeconds(1.8f);
        GamePlayManager.instance.LevelComplete();

        GamePlayManager.instance.RainBG.Stop();
        SoundManager.instance.LevelCompleteSound();
        StartCoroutine(UIBGSound());
    }

    public IEnumerator UIBGSound()
    {
        yield return new WaitForSeconds(2f);
        SoundManager.instance.BGUISound();
        print("BGUISOUND");
    }

}
