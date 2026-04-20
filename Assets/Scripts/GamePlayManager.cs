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
        public DropPoint initDropPoint;
        public CullingArea spawnCullingArea;
    }

    [Header("Level Setup")]
    public List<LevelConfig> levelConfigs = new();
    public GameObject levelsParent;
    public Transform playerSpawnPointsParent;
    public Transform babySpawnPointsParent;
    public PlayerController player;
    public BabyController baby;

    [Header("Environment")]
    public MyAudioSource RainBG;
    public MyAudioSource babyCryingCradle;
    public DoorController babyRoomDoor;
    public DoorController houseExitDoor;
    public DropPoint cradleDropPoint;
    public ParticleSystem axeBlueGlow;
    public GameObject[] flyingFurniture;
    public GameObject[] Cracker;

    [Header("Scene References")]
    public FireArea bedroomFireArea;

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

    private void OnEnable() => ObjectiveUIController.OnTaskReceived += OnTaskReceived;
    private void OnDisable() => ObjectiveUIController.OnTaskReceived -= OnTaskReceived;

    private void Start()
    {
        RainBG.Play();
        AudioManager.Instance.GameEnd();
        AudioManager.Instance.SetBGSetting(false);

        player.gameObject.transform.SetPositionAndRotation(
            CurrentConfig.playerSpawnPoint.position,
            CurrentConfig.playerSpawnPoint.rotation);

        CullingManager.Instance.SetActiveArea(CurrentConfig.spawnCullingArea);

        CurrentConfig.levelObject.SetActive(true);

        var levelData = LevelConfigLoader.GetLevelData(Level);
        if (levelData != null)
            ApplyLevelData(levelData);

        if (baby.gameObject.activeSelf)
        {
            var spawnPoint = CurrentConfig.babySpawnPoint;
            if (CurrentConfig.initDropPoint != null)
            {
                CurrentConfig.initDropPoint.DropOnPoint(baby);
            }
            else
            {
                baby.SetActiveAndPositionAndRotation(spawnPoint != null, spawnPoint);
            }

            var babyAnim = levelData != null
                ? LevelConfigLoader.ParseBabyAnimation(levelData.baby.initialAnimation)
                : BabyAnimationType.CrySit;
            baby.SetAnimation(babyAnim);
        }

        ArcadiaSdkManager.CurrentAdPlacement = "gameplay_banner";
        ArcadiaSdkManager.Agent.ShowBanner();
        AnalyticsTracker.OnLevelStart(Level);
        AA_AnalyticsManager.Agent.TrackScreenView("gameplay");
        AA_AnalyticsManager.Agent.GameStartAnalytics(Level);
    }

    public void OnInteractableInteract(ItemType itemType)
    {
        if (itemType == ItemType.BabyRoomDoor || Level == 6)
            ObjectiveUIController.OnTaskEventReceived(TaskType.CheckBabyRoom);
    }

    void ApplyLevelData(LevelData data)
    {
        ApplyDoorSetup(data.doors);
        ApplyBabySetup(data.baby);
        ApplyFeatures(data.features);
    }

    void ApplyDoorSetup(DoorSetup doors)
    {
        houseExitDoor.SetLocked(doors.houseExitLocked);
        babyRoomDoor.SetLocked(doors.babyRoomLocked);

        if (doors.doorKnocking != null && doors.doorKnocking.enabled)
            houseExitDoor.PlayDoorKnocking(doors.doorKnocking.initialDelay, doors.doorKnocking.interval);

        if (doors.doorBell)
            houseExitDoor.PlayDoorBell(true);
    }

    void ApplyBabySetup(BabySetup setup)
    {
        baby.babyEyesRed.color = Color.white;
        baby.requireItem = LevelConfigLoader.ParseItemType(setup.requireItem);
        baby.canPickBaby = setup.canPickBaby;

        if (setup.dirtyFace)
            baby.babyDirtyFace.SetActive(true);

        if (!setup.active)
            baby.SetActiveAndPositionAndRotation(false, null);

        if (setup.possessed)
        {
            baby.babyEyesRed.color = Color.red;
            baby.rb.isKinematic = true;
            baby.rb.useGravity = false;
        }
    }

    void ApplyFeatures(FeatureFlags features)
    {
        if (features.cradleActive)
            cradleDropPoint.gameObject.SetActive(true);

        if (features.babyCryingCradle)
            babyCryingCradle.Play();

        if (features.fireActive && bedroomFireArea != null)
            bedroomFireArea.ActivateFire();

        if (features.flyingFurniture)
            SetupFlyingFurniture();

        var playerAnim = LevelConfigLoader.ParsePlayerAnimation(features.playerStartAnimation);
        if (playerAnim != PlayerAnimation.None)
            player.SetAnimation(playerAnim);

        if (!features.showNextButton)
            UIManager.Instance.nextButton.SetActive(false);

        if (features.showRateUsButton)
            UIManager.Instance.rateusButton.SetActive(true);
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

    public void LevelComplete()
    {
        DOVirtual.DelayedCall(2f, () =>
        {
            UIManager.Instance.LvlCompleteON();
            AudioManager.Instance.PlaySFX(SFX.LevelComplete);

            int currentOpen = GamePreference.openLevels;
            if (currentOpen < 9 && Level == currentOpen + 1)
                GamePreference.openLevels = currentOpen + 1;

            AA_AnalyticsManager.Agent.GameCompleteAnalytics(Level);
            ArcadiaSdkManager.Agent.ShowRateUs();
        });
    }

    void OnTaskReceived(TaskType taskType)
    {
        if (taskType == TaskType.CheckBabyRoom)
            SpawnBabyInKitchen();
        if (taskType == TaskType.FollowBabyVoice)
            player.SetAnimation(PlayerAnimation.Unconscious);
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
