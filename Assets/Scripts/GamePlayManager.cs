using System;
using System.Collections.Generic;
using DG.Tweening;
using Ommy.Attributes;
using Ommy.Audio;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("babyRoomDoor")]
    public DoorController upperRoomDoor;
    public DoorController houseExitDoor;
    public DropPoint cradleDropPoint;
    public DropPoint[] allDropPoints;
    public GameObject[] flyingFurniture;

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

        float spawnPitch = CurrentConfig.playerSpawnPoint.eulerAngles.x;
        if (spawnPitch > 180f) spawnPitch -= 360f;
        player.InitializeCameraPitch(spawnPitch);

        CullingManager.Instance.SetActiveArea(CurrentConfig.spawnCullingArea);

        CurrentConfig.levelObject.SetActive(true);

        var levelData = LevelConfigLoader.GetLevelData(Level);
        ApplyLevelData(levelData);

        if (baby.gameObject.activeSelf)
        {
            var spawnPoint = CurrentConfig.babySpawnPoint;
            if (CurrentConfig.initDropPoint != null)
                CurrentConfig.initDropPoint.DropOnPoint(baby, jumpDuration: 0f, rotationDuration: 0f);
            else
                baby.SetActiveAndPositionAndRotation(spawnPoint != null, spawnPoint);

            var babyAnim = LevelConfigLoader.ParseBabyAnimation(levelData.baby.initialAnimation);
            var overrideSound = !string.IsNullOrEmpty(levelData.baby.overrideSound)
                ? LevelConfigLoader.ParseBabyAnimation(levelData.baby.overrideSound)
                : BabyAnimationType.None;
            baby.SetAnimation(babyAnim, overrideSound: overrideSound);
        }

        ArcadiaSdkManager.CurrentAdPlacement = "gameplay_banner";
        ArcadiaSdkManager.Agent.ShowBanner();
        AnalyticsTracker.OnLevelStart(Level);
        AA_AnalyticsManager.Agent.TrackScreenView("gameplay");
        AA_AnalyticsManager.Agent.GameStartAnalytics(Level);
    }

    public void OnInteractableInteract(ItemType itemType)
    {
        if (itemType == ItemType.UpperRoomDoor)
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
        upperRoomDoor.SetLocked(doors.upperRoomLocked);

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
        ApplyDropPoints(features.activeDropPoints);

        if (features.fireActive && bedroomFireArea != null)
            bedroomFireArea.ActivateFire();

        if (features.flyingFurniture)
            SetupFlyingFurniture();

        var playerAnim = LevelConfigLoader.ParsePlayerAnimation(features.playerStartAnimation);
        if (playerAnim != PlayerAnimation.None)
            player.SetAnimation(playerAnim);
    }

    void ApplyDropPoints(string[] activeDropPointNames)
    {
        foreach (var dp in allDropPoints)
            dp.gameObject.SetActive(false);

        if (activeDropPointNames == null || activeDropPointNames.Length == 0)
            return;

        var activeSet = new HashSet<string>(activeDropPointNames);

        foreach (var dp in allDropPoints)
        {
            if (activeSet.Contains(dp.referenceName))
                dp.gameObject.SetActive(true);
        }
    }

    public void SetupFlyingFurniture(bool isFly = true)
    {
        foreach (var furniture in flyingFurniture)
        {
            var rb = furniture.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = !isFly;
            if (isFly) rb.AddForce(10, 10, 10);
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
        if (taskType == TaskType.FollowBabyVoice)
            player.SetAnimation(PlayerAnimation.Unconscious);
    }
}
