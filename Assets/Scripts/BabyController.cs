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

    public GameObject babyDirtyFace;


    private void Start()
    {
        if (GameManager.instance.selectedLevel > 4)
        {
            body.SetActive(false);
            diaper.SetActive(false);
            clothBody.SetActive(true);
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
