using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetection : MonoBehaviour
{

    public GameObject behoshBanda;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player" && GameManager.instance.selectedLevel == 6)
        {
            GamePlayManager.instance.babyCryingCradle.Stop();
            ObjectiveController.instance.UpdateTask(1);
            StartCoroutine(DelaySoundStart());
        }

      

        if (other.gameObject.tag == "Player" && GameManager.instance.selectedLevel == 8)
        {
            ObjectiveController.instance.UpdateTask(3);
            StartCoroutine(BabyController.instance.LevelComplete());
        }

        if (other.gameObject.tag == "Neck" && GameManager.instance.selectedLevel == 9)
        {
            print("neck");

            behoshBanda.SetActive(true);

            ObjectiveController.instance.UpdateTask(1);
            StartCoroutine(BabyController.instance.LevelComplete());
        }
    }

    IEnumerator DelaySoundStart()
    {
        GamePlayManager.instance.baby.SetActive(true);
        BabyController.instance.BabyAnim.SetBool("Sit", true);
        yield return new WaitForSeconds(2f);
        BabyController.instance.babyCry.Play();
      
    }
}
