using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    public float loadingTime;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale=1;
        StartCoroutine("Timer");
    }
    IEnumerator Timer()
    {
        yield return new WaitForSeconds(loadingTime);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
    }
}
