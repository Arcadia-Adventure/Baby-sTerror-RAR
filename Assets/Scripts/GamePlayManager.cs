using System;
using System.Collections.Generic;
using DG.Tweening;
using Ommy.Attributes;
using Ommy.Audio;
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
    public DropPoint cradleDropPoint;
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
    public BabyController baby;
    public AudioSource babyCryingCradle;
    public GameObject[] flyingFurniture;
    public ParticleSystem axeBlueGlow;
    public AudioSource RainBG;
    public DoorController babyRoomDoor;
    public DoorController houseExitDoor;
    public AudioSource doorLock;
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
                AudioManager.Instance.PlaySFX(SFX.DoorBreak);
                return;
            }
        }
    }


    private void Start()
    {
        RainBG.volume = 0.2f;
        RainBG.Play();
        AudioManager.Instance.SetBGSetting(false);

        player.transform.position = levelConfigs[GamePreference.selectedLevel - 1].playerSpawnPoint.position;    
        player.transform.rotation = levelConfigs[GamePreference.selectedLevel - 1].playerSpawnPoint.rotation;
        levelConfigs[GamePreference.selectedLevel - 1].levelObject.SetActive(true);
        var spawnPoint = levelConfigs[GamePreference.selectedLevel - 1].babySpawnPoint;
        baby.SetActiveAndPositionAndRotation(spawnPoint!=null, spawnPoint);
        BabyController.Instance.SetAnimation(BabyAnimationType.Cry);
        SetupLevel();
        ArcadiaSdkManager.Agent.ShowBanner();
        AA_AnalyticsManager.Agent.GameStartAnalytics(GamePreference.selectedLevel);
    }
    public void OnInteractableInteract(ItemType itemType)
    {
        if(itemType == ItemType.BabyRoomDoor || GamePreference.selectedLevel == 6)
        {
            ObjectiveManager.OnTaskEventReceived(TaskType.CheckBabyRoom);
        }
    }
    public void SetupLevel()
    {
        houseExitDoor.isDoorLock = GamePreference.selectedLevel != 1;
        babyRoomDoor.isDoorLock = GamePreference.selectedLevel == 8;
        if (GamePreference.selectedLevel == 1)
        {
            cradleDropPoint.gameObject.SetActive(true);
            houseExitDoor.PlayDoorBell(true);
            BabyController.Instance.SetAnimation(BabyAnimationType.Cry);
        }
        if (GamePreference.selectedLevel == 2)
        {
            BabyController.Instance.requireItem = ItemType.Feeder;
            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
        }

        if (GamePreference.selectedLevel == 3)
        {
            BabyController.Instance.requireItem = ItemType.Facewash;
            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
            BabyController.Instance.babyDirtyFace.SetActive(true);
        }

        if (GamePreference.selectedLevel == 4)
        {
            BabyController.Instance.requireItem = ItemType.Cloth;
            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
        }

        if (GamePreference.selectedLevel == 5)
        {
            BabyController.Instance.requireItem = ItemType.Toy;
            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
        }

        if (GamePreference.selectedLevel == 6)
        { 
            babyCryingCradle.Play();
            baby.SetActiveAndPositionAndRotation(false, null);
        }

        if (GamePreference.selectedLevel == 7)
        {
            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
            BabyController.Instance.canPickBaby = false;
        }

        if (GamePreference.selectedLevel == 8)
        {
            BabyController.Instance.PlayAudio(BabyAnimationType.Cry, true);

            BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
            axeBlueGlow.transform.parent.tag = "Untagged";

            axeBlueGlow.Stop();

            Items.instance.fireLvl8.GetComponentInChildren<AudioSource>().Play();
        }


        if(GamePreference.selectedLevel == 9)
        {
            BabyController.Instance.babyEyesRed.color = Color.red;

            BabyController.Instance.SetAnimation(BabyAnimationType.AngrySit);

            BabyController.Instance.PlayAudio(BabyAnimationType.AngrySit, true);

            baby.GetComponent<Rigidbody>().isKinematic = true;
            baby.GetComponent<Rigidbody>().useGravity = false;

            baby.tag = "Untagged";

        }
        else
        {
            BabyController.Instance.babyEyesRed.color = Color.white;
        }

        if(GamePreference.selectedLevel == 10)
        {

            Items.instance.fireLvl10.GetComponentInChildren<AudioSource>().Play();

            for (int i = 0; i < flyingFurniture.Length; i++)
            {
                flyingFurniture[i].GetComponent<Rigidbody>().useGravity = false;
                flyingFurniture[i].GetComponent<Rigidbody>().AddForce(10, 10, 10);
            }

            baby.tag = "Untagged";

            BabyController.Instance.babyEyesRed.color = Color.red;

            BabyController.Instance.SetAnimation(BabyAnimationType.Fly);
            

            BabyController.Instance.PlayAudio(BabyAnimationType.AngrySit, true);

            baby.GetComponent<Rigidbody>().isKinematic = true;
            baby.GetComponent<Rigidbody>().useGravity = false;


            UIManager.instance.nextButton.SetActive(false);
            UIManager.instance.rateusButton.SetActive(true);

            BabyController.Instance.MuteAudio(true);

        }
    }
    public void LevelComplete()
    {
        DOVirtual.DelayedCall(2f, () => {
            UIManager.instance.LvlCompleteON();
            AudioManager.Instance.PlaySFX(SFX.LevelComplete);
            int currentPlayerPrefs = GamePreference.openLevels;

            if(currentPlayerPrefs < 9 && GamePreference.selectedLevel == currentPlayerPrefs+1)
            {
                GamePreference.openLevels = currentPlayerPrefs+1;
            }
            ArcadiaSdkManager.Agent.ShowRateUs();
            AA_AnalyticsManager.Agent.GameCompleteAnalytics(GamePreference.selectedLevel);
        });
    }
    public void OnTaskReceived(TaskType taskType)
    {
        if(taskType == TaskType.CheckBabyRoom)
        {
            SpawnBabyInKitchen();
        }
    }
    public void SpawnBabyInKitchen()
    {
        babyCryingCradle.Stop();
        baby.gameObject.SetActive(true);
        BabyController.Instance.SetAnimation(BabyAnimationType.Sit);
        DOVirtual.DelayedCall(2f, () => BabyController.Instance.PlayAudio(BabyAnimationType.Cry, true));
        BabyController.Instance.OnPick+=()=>cradleDropPoint.gameObject.SetActive(true);
    }
}
