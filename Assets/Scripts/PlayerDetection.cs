using System.Collections;
using System.Collections.Generic;
using Ommy.Prefs;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles player detection triggers with level-specific effects.
/// For simple objective triggers, consider using ObjectiveTrigger instead.
/// </summary>
public class PlayerDetection : MonoBehaviour
{
    public FirstPersonController fpc;

    public GameObject behoshBanda;
    public UnityEvent<Collider> onTriggerEnter,onTriggerExit;

    [Header("Objective Settings (Optional)")]
    [Tooltip("If set, completes this task when triggered. Use 0 to disable.")]
    [Min(0)]
    public int taskIndexOnTrigger = 0;

    [Tooltip("If true, completes the level after task completion")]
    public bool completesLevel = false;

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter.Invoke(other);
        
        // Configurable task completion (no level check needed)
        if (other.CompareTag("Player") && taskIndexOnTrigger > 0)
        {
            //ObjectiveManager.Instance.CompleteTask(taskIndexOnTrigger);
            
            if (completesLevel)
            {
                StartCoroutine(SoundManager.instance.LevelComplete());
            }
        }

        // Level-specific effects (keep for backward compatibility, can be removed once scenes are updated)
        if(other.gameObject.tag == "Player" && GamePreference.selectedLevel == 6)
        {
            GamePlayManager.Instance.babyCryingCradle.Stop();
            StartCoroutine(DelaySoundStart());
        }

        if (other.gameObject.tag == "Neck" && GamePreference.selectedLevel == 9)
        {
            print("neck");
            fpc.GetComponent<Rigidbody>().isKinematic = true;
            this.GetComponent<AudioSource>().enabled = false;
            BabyController.instance.babyAngryVoice.Stop();
            behoshBanda.SetActive(true);
            SoundManager.instance.playerFall.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit.Invoke(other);
    }

    IEnumerator DelaySoundStart()
    {
        GamePlayManager.Instance.baby.gameObject.SetActive(true);
        BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
        yield return new WaitForSeconds(2f);
        BabyController.instance.babyCry.Play();
    }
}
