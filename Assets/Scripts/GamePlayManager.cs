using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ommy.Attributes;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;

public class GamePlayManager : Singleton<GamePlayManager>
{
    [Serializable]
    public class LevelConfig
    {
        public GameObject levelObject;
        public Transform playerSpawnPoint,babySpawnPoint;
        public BabyAnimationType babyAnimationType;
    }
    public GameObject levelsParent;
    public Transform playerSpawnPointsParent;
    public Transform babySpawnPointsParent;
    [InspectorButton("SetupLevels")]
    public void SetupLevelsConfig()
    {
        int levelCount = levelsParent.transform.childCount;
        for (int i = 0; i < levelCount; i++)
        {
            LevelConfig config = new ();
            config.levelObject = levelsParent.transform.GetChild(i).gameObject;
            config.playerSpawnPoint = playerSpawnPointsParent.GetChild(i);
            config.babySpawnPoint = babySpawnPointsParent.GetChild(i);
            levelConfigs.Add(config);
            config.levelObject.SetActive(false);
        }
    }
    public List<LevelConfig> levelConfigs = new List<LevelConfig>();
    public GameObject player;
    public AudioSource doorBell;
    public BabyController baby;

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
    public DoorController houseExitDoor;
    public AudioSource doorLock;

    public GameObject[] babyDropSpwanPoint;


    public GameObject[] Cracker;

    [Header("Door Break Objective Settings")]
    [Tooltip("Task index to complete when door is fully broken. Set to 0 to disable.")]
    [Min(0)]
    public int doorBreakTaskIndex = 0;
   

    public void Crack()
    {
        for (int i = 0; i < Cracker.Length; i++)
        {
            if (i == 2)
            {
                babyRoomDoor.isDoorLock = false;
                
               
                var g = PickDropController.instance.heldPickable.gameObject;
                Destroy(g);
                
                // Complete task if configured
                if (doorBreakTaskIndex > 0)
                {
                   // ObjectiveManager.Instance.CompleteTask(doorBreakTaskIndex);
                }

            }
            if (!Cracker[i].activeInHierarchy)
            {
                Cracker[i].SetActive(true);
                SoundManager.instance.doorBreak.Play();
                return;
            }
        }
    }


    private void Start()
    {
        RainBG.volume = 0.2f;
        RainBG.Play();
        SoundManager.instance.BG.Stop();

        player.transform.position = levelConfigs[GamePreference.selectedLevel - 1].playerSpawnPoint.position;    
        player.transform.rotation = levelConfigs[GamePreference.selectedLevel - 1].playerSpawnPoint.rotation;
        levelConfigs[GamePreference.selectedLevel - 1].levelObject.SetActive(true);
        var spawnPoint = levelConfigs[GamePreference.selectedLevel - 1].babySpawnPoint;
        baby.SetActiveAndPositionAndRotation(spawnPoint!=null, spawnPoint);
        BabyController.instance.babyCry.Play();
        SetupLevel();
        ArcadiaSdkManager.Agent.ShowBanner();
        AA_AnalyticsManager.Agent.GameStartAnalytics(GamePreference.selectedLevel);
    }

    public void SetupLevel()
    {
        houseExitDoor.isDoorLock = GamePreference.selectedLevel != 1;
        if (GamePreference.selectedLevel == 1)
        {
            doorBell.Play();
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Cry);
        }
        if (GamePreference.selectedLevel == 2)
        {
            BabyController.instance.requireItem = ItemType.Feeder;
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
        }

        if (GamePreference.selectedLevel == 3)
        {
            BabyController.instance.requireItem = ItemType.Facewash;
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
            BabyController.instance.babyDirtyFace.SetActive(true);
        }

        if (GamePreference.selectedLevel == 4)
        {
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
            baby.tag = "Untagged";
        }

        if (GamePreference.selectedLevel == 5)
        {
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
            baby.tag = "Untagged";
        }

        if (GamePreference.selectedLevel == 6)
        { 
            babyCryingCradle.Play();
            baby.SetActiveAndPositionAndRotation(false, null);
        }

        if (GamePreference.selectedLevel == 7)
        {
            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
            baby.tag = "Untagged";

            Items.instance.fireLvl7.GetComponentInChildren<AudioSource>().Play();

        }



        if (GamePreference.selectedLevel == 8)
        {
            babyRoomDoor.isDoorLock = true;
            BabyController.instance.babyCry.Play();

            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Sit);
            axeBlueGlow.transform.parent.tag = "Untagged";

            axeBlueGlow.Stop();

            Items.instance.fireLvl8.GetComponentInChildren<AudioSource>().Play();
        }
        else
        {
            babyRoomDoor.isDoorLock = false;
        }


        if(GamePreference.selectedLevel == 9)
        {
            BabyController.instance.babyEyesRed.color = Color.red;

            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.AngrySit);

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

        if(GamePreference.selectedLevel == 10)
        {

            Items.instance.fireLvl10.GetComponentInChildren<AudioSource>().Play();

            StartCoroutine(HoshBandaOff());

            SoundManager.instance.playerStandup.Play();

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

            BabyController.instance.babyAnimationController.SetAnimation(BabyAnimationType.Fly);
            

            BabyController.instance.babyAngryVoice.Play();
            BabyController.instance.babyCry.Stop();

            baby.GetComponent<Rigidbody>().isKinematic = true;
            baby.GetComponent<Rigidbody>().useGravity = false;


            UIManager.instance.nextButton.SetActive(false);
            UIManager.instance.rateusButton.SetActive(true);

            baby.GetComponent<AudioSource>().mute = enabled;

        }
    }
    public void LevelComplete()
    {
        DOVirtual.DelayedCall(2f, () => {
            UIManager.instance.LvlCompleteON();
            int currentPlayerPrefs = GamePreference.openLevels;

            if(currentPlayerPrefs < 9 && GamePreference.selectedLevel == currentPlayerPrefs+1)
            {
                GamePreference.openLevels = currentPlayerPrefs+1;
            }
            ArcadiaSdkManager.Agent.ShowRateUs();
            AA_AnalyticsManager.Agent.GameCompleteAnalytics(GamePreference.selectedLevel);
        });
    }


    IEnumerator HoshBandaOff()
    {
        hoshBanda.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        hoshBanda.SetActive(false);
    }
}
