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
        public Transform playerSpawnPoint, babySpawnPoint;
        public BabyAnimationType babyAnimationType;
    }

    [Header("Level Setup")]
    public List<LevelConfig> levelConfigs = new();
    public GameObject levelsParent;
    public Transform playerSpawnPointsParent;
    public Transform babySpawnPointsParent;
    public GameObject player;
    public BabyController baby;

    [Header("Environment")]
    public AudioSource RainBG;
    public AudioSource babyCryingCradle;
    public AudioSource doorLock;
    public DoorController babyRoomDoor;
    public DoorController houseExitDoor;
    public DropPoint cradleDropPoint;
    public ParticleSystem axeBlueGlow;
    public GameObject[] flyingFurniture;
    public GameObject[] Cracker;

    int Level => GamePreference.selectedLevel;
    LevelConfig CurrentConfig => levelConfigs[Level - 1];

    #region Editor Setup

    [InspectorButton("SetupLevels")]
    public void SetupLevelsConfig()
    {
        int levelCount = levelsParent.transform.childCount;
        for (int i = 0; i < levelCount; i++)
        {
            LevelConfig config = new()
            {
                levelObject = levelsParent.transform.GetChild(i).gameObject,
                playerSpawnPoint = playerSpawnPointsParent.GetChild(i),
                babySpawnPoint = babySpawnPointsParent.GetChild(i)
            };
            levelConfigs.Add(config);
            config.levelObject.SetActive(false);
        }
    }

    #endregion

    private void OnEnable() => ObjectiveManager.OnTaskReceived += OnTaskReceived;
    private void OnDisable() => ObjectiveManager.OnTaskReceived -= OnTaskReceived;

    private void Start()
    {
        RainBG.volume = 0.2f;
        RainBG.Play();
        AudioManager.Instance.SetBGSetting(false);

        player.transform.SetPositionAndRotation(
            CurrentConfig.playerSpawnPoint.position,
            CurrentConfig.playerSpawnPoint.rotation);

        CurrentConfig.levelObject.SetActive(true);

        var spawnPoint = CurrentConfig.babySpawnPoint;
        baby.SetActiveAndPositionAndRotation(spawnPoint != null, spawnPoint);
        baby.SetAnimation(BabyAnimationType.CrySit);

        SetupLevel();

        ArcadiaSdkManager.Agent.ShowBanner();
        AA_AnalyticsManager.Agent.GameStartAnalytics(Level);
    }

    public void OnInteractableInteract(ItemType itemType)
    {
        if (itemType == ItemType.BabyRoomDoor || Level == 6)
            ObjectiveManager.OnTaskEventReceived(TaskType.CheckBabyRoom);
    }

    void SetupLevel()
    {
        houseExitDoor.isDoorLock = Level != 1;
        babyRoomDoor.isDoorLock = Level == 8;
        baby.babyEyesRed.color = Color.white;

        switch (Level)
        {
            case 1:
                AudioSources.Instance.PlayDoorKnocking(1f, true);
                cradleDropPoint.gameObject.SetActive(true);
                houseExitDoor.PlayDoorBell(true);
                baby.SetAnimation(BabyAnimationType.CryLay);
                break;

            case 2:
                baby.requireItem = ItemType.Feeder;
                baby.SetAnimation(BabyAnimationType.CrySit);
                break;

            case 3:
                baby.requireItem = ItemType.Facewash;
                baby.SetAnimation(BabyAnimationType.CrySit);
                baby.babyDirtyFace.SetActive(true);
                break;

            case 4:
                baby.requireItem = ItemType.Cloth;
                baby.SetAnimation(BabyAnimationType.CrySit);
                break;

            case 5:
                baby.requireItem = ItemType.Toy;
                baby.SetAnimation(BabyAnimationType.CrySit);
                break;

            case 6:
                babyCryingCradle.Play();
                baby.SetActiveAndPositionAndRotation(false, null);
                break;

            case 7:
                baby.SetAnimation(BabyAnimationType.CrySit);
                baby.canPickBaby = false;
                break;

            case 8:
                baby.SetAnimation(BabyAnimationType.CrySit);
                axeBlueGlow.transform.parent.tag = "Untagged";
                axeBlueGlow.Stop();
                Items.instance.fireLvl8.GetComponentInChildren<AudioSource>().Play();
                break;

            case 9:
                SetupPossessedBaby(BabyAnimationType.AngrySit, BabyAnimationType.AngrySit);
                baby.canPickBaby = false;
                break;

            case 10:
                SetupPossessedBaby(BabyAnimationType.Fly, BabyAnimationType.AngrySit);
                SetupFlyingFurniture();
                baby.requireItem = ItemType.Talisman;
                baby.canPickBaby = false;
                UIManager.instance.nextButton.SetActive(false);
                UIManager.instance.rateusButton.SetActive(true);
                break;
        }
    }

    void SetupPossessedBaby(BabyAnimationType animation, BabyAnimationType audio)
    {
        baby.babyEyesRed.color = Color.red;
        baby.SetAnimation(animation);
        baby.PlayAudio(audio);
        baby.rb.isKinematic = true;
        baby.rb.useGravity = false;
    }

    void SetupFlyingFurniture()
    {
        foreach (var furniture in flyingFurniture)
        {
            var rb = furniture.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.AddForce(10, 10, 10);
        }
    }

    public void Crack()
    {
        for (int i = 0; i < Cracker.Length; i++)
        {
            if (!Cracker[i].activeInHierarchy)
            {
                Cracker[i].SetActive(true);
                AudioManager.Instance.PlaySFX(SFX.DoorBreak);

                if (i == Cracker.Length - 1)
                {
                    babyRoomDoor.isDoorLock = false;
                    Destroy(PickDropController.instance.heldPickable.gameObject);
                }
                return;
            }
        }
    }

    public void LevelComplete()
    {
        DOVirtual.DelayedCall(2f, () =>
        {
            UIManager.instance.LvlCompleteON();
            AudioManager.Instance.PlaySFX(SFX.LevelComplete);

            int currentOpen = GamePreference.openLevels;
            if (currentOpen < 9 && Level == currentOpen + 1)
                GamePreference.openLevels = currentOpen + 1;

            ArcadiaSdkManager.Agent.ShowRateUs();
            AA_AnalyticsManager.Agent.GameCompleteAnalytics(Level);
        });
    }

    void OnTaskReceived(TaskType taskType)
    {
        if (taskType == TaskType.CheckBabyRoom)
            SpawnBabyInKitchen();
        if(taskType == TaskType.FollowBabyVoice)
            player.GetComponent<PlayerAnimationController>().SetAnimation(PlayerAnimation.Unconscious);
    }

    void SpawnBabyInKitchen()
    {
        babyCryingCradle.Stop();
        baby.gameObject.SetActive(true);
        baby.SetAnimation(BabyAnimationType.CrySit);
        DOVirtual.DelayedCall(2f, () => baby.PlayAudio(BabyAnimationType.CrySit));
        baby.OnPick -= OnBabyPickedInKitchen;
        baby.OnPick += OnBabyPickedInKitchen;
    }

    void OnBabyPickedInKitchen() => cradleDropPoint.gameObject.SetActive(true);
}
