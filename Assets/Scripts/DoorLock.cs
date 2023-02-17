using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorLock : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Axe")
        {
            if(GameManager.instance.selectedLevel == 8)
            {
                this.GetComponent<BoxCollider>().enabled = false;
                GamePlayManager.instance.redDoorGlow.Stop();
                GamePlayManager.instance.greenDoorGlow.Play();
                Destroy(other.gameObject);

                ObjectiveController.instance.UpdateTask(2);

                StartCoroutine(GreenGlowOff());
            }
        }
    }


    public IEnumerator GreenGlowOff()
    {
        yield return new WaitForSeconds(2f);
        GamePlayManager.instance.greenDoorGlow.Stop();
    }
}
