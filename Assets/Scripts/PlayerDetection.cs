using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDetection : MonoBehaviour
{
    public FirstPersonController fpc;

    public GameObject behoshBanda;
    public UnityEvent<Collider> onTriggerEnter,onTriggerExit;

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter.Invoke(other);
        if(other.gameObject.tag == "Player" && GameManager.Instance.selectedLevel == 6)
        {
            GamePlayManager.instance.babyCryingCradle.Stop();
            ObjectiveController.Instance.UpdateTask(1);
            StartCoroutine(DelaySoundStart());
        }


        if (other.gameObject.tag == "Player" && GameManager.Instance.selectedLevel == 8)
        {
            ObjectiveController.Instance.UpdateTask(3);
            StartCoroutine(SoundManager.instance.LevelComplete());
        }

        if (other.gameObject.tag == "Neck" && GameManager.Instance.selectedLevel == 9)
        {
            print("neck");

            //fpc.isWalking = false;

            fpc.GetComponent<Rigidbody>().isKinematic = true;

            this.GetComponent<AudioSource>().enabled = false;

            BabyController.instance.babyAngryVoice.Stop();

            behoshBanda.SetActive(true);

            SoundManager.instance.playerFall.Play();

            ObjectiveController.Instance.UpdateTask(1);
            StartCoroutine(SoundManager.instance.LevelComplete());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit.Invoke(other);
    }

    IEnumerator DelaySoundStart()
    {
        GamePlayManager.instance.baby.SetActive(true);
        BabyController.instance.BabyAnim.SetBool("Sit", true);
        yield return new WaitForSeconds(2f);
        BabyController.instance.babyCry.Play();
      
    }
}
