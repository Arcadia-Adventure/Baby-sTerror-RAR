using System;
using System.Collections.Generic;
using Ommy.Attributes;
using Ommy.Audio;
using Ommy.Prefs;
using Ommy.Singleton;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class GamePlayManager : Singleton<GamePlayManager>
{
    [Header("House")]
    public PlayerController player;
    public BabyController baby;
    public NannyStateManager nanny;
    public MyAudioSource RainBG;

    [Header("Shared Scene Objects")]
    [Tooltip("Every drop point in the house. They all start off; levels switch on the ones they use.")]
    public DropPoint[] allDropPoints;
    public DropPoint cradleDropPoint;
    public GameObject[] flyingFurniture;
    public FireArea bedroomFireArea;

    #region Legacy (read once by Baby's Terror > Migrate Scenes To Level System)

    [Serializable]
    public class LevelConfig
    {
        public GameObject levelObject;
        public Transform playerSpawnPoint, babySpawnPoint;
        public DropPoint initDropPoint;
        public CullingArea spawnCullingArea;
        public Transform nannySpawnPoint;
        public Transform nannyPatrolRoute;
    }

    [HideInInspector] public List<LevelConfig> levelConfigs = new();
    [HideInInspector] public GameObject levelsParent;
    [HideInInspector] public Transform playerSpawnPointsParent;
    [HideInInspector] public Transform babySpawnPointsParent;
    [HideInInspector, FormerlySerializedAs("babyRoomDoor")] public DoorController upperRoomDoor;
    [HideInInspector] public DoorController houseExitDoor;

    #endregion

    public LevelDefinition CurrentLevel { get; private set; }
    public LevelSceneSetup CurrentSetup { get; private set; }

    int Level => GamePreference.selectedLevel;

    LevelRunner _runner;
    bool _levelEnded;

    #region Editor Setup

    /// <summary>
    /// First pass for level design: gives every level that lacks them a Nanny spawn point and a
    /// four-waypoint route on the NavMesh around the baby, with the spawn kept far from the player.
    /// Drag the generated points into place afterwards.
    /// </summary>
    [InspectorButton("Create Missing Nanny Points")]
    public void SetupNannyPoints()
    {
        const string rootName = "NannyPoints";
        GameObject rootObject = GameObject.Find(rootName);
        if (rootObject == null)
            rootObject = new GameObject(rootName);
        Transform root = rootObject.transform;

        foreach (LevelSceneSetup setup in LevelSceneSetup.FindAll())
        {
            if (setup.nannySpawnPoint != null && setup.nannyPatrolRoute != null)
                continue;

            Transform levelRoot = GetOrCreateChild(root, $"Level {setup.LevelNumber}");
            Vector3 anchor = setup.babySpawnPoint != null ? setup.babySpawnPoint.position
                : setup.playerSpawnPoint != null ? setup.playerSpawnPoint.position
                : root.position;
            Vector3 playerSpawn = setup.playerSpawnPoint != null ? setup.playerSpawnPoint.position : anchor;

            if (setup.nannySpawnPoint == null)
            {
                Vector3 spawn = anchor;
                float bestDistance = -1f;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    if (!TrySampleNavMeshNear(anchor, 15f, out Vector3 candidate))
                        continue;

                    float distance = Vector3.Distance(candidate, playerSpawn);
                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        spawn = candidate;
                    }
                }

                setup.nannySpawnPoint = GetOrCreateChild(levelRoot, "Spawn");
                setup.nannySpawnPoint.position = spawn;
            }

            if (setup.nannyPatrolRoute == null)
            {
                setup.nannyPatrolRoute = GetOrCreateChild(levelRoot, "Route");
                for (int w = setup.nannyPatrolRoute.childCount; w < 4; w++)
                {
                    Transform waypoint = GetOrCreateChild(setup.nannyPatrolRoute, $"Waypoint {w + 1}");
                    waypoint.position = TrySampleNavMeshNear(anchor, 10f, out Vector3 point) ? point : anchor;
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(setup);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        return child;
    }

    static bool TrySampleNavMeshNear(Vector3 center, float radius, out Vector3 point)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
        if (NavMesh.SamplePosition(center + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = center;
        return false;
    }

    #endregion

    private void OnEnable() => ObjectiveUIController.OnTaskReceived += OnTaskReceived;

    private void OnDisable()
    {
        ObjectiveUIController.OnTaskReceived -= OnTaskReceived;
        // Static event, so subscribers would otherwise leak into the next level load.
        OnLevelFailed = null;
    }

    private void Start()
    {
        RainBG.Play();
        AudioManager.Instance.GameEnd();
        AudioManager.Instance.SetBGSetting(false);

        CurrentLevel = LevelConfigLoader.GetLevelData(Level);
        CurrentSetup = LevelSceneSetup.FindFor(CurrentLevel);
        if (CurrentSetup == null)
        {
            Debug.LogError($"[GamePlayManager] No LevelSceneSetup in the scene for level {Level}. " +
                           "Run Baby's Terror > Migrate Scenes To Level System, or add one to the level's object.", this);
            return;
        }

        PlacePlayer(CurrentSetup.playerSpawnPoint);
        CullingManager.Instance.SetActiveArea(CurrentSetup.spawnCullingArea);
        CurrentSetup.gameObject.SetActive(true);

        ResetHouse();
        _runner = new LevelRunner(new LevelContext(CurrentLevel, CurrentSetup, this));
        _runner.Start();

        ArcadiaSdkManager.CurrentAdPlacement = "gameplay_banner";
        ArcadiaSdkManager.Agent.ShowBanner();
        ArcadiaSdkManager.Agent.PrepareInterstitial();
        AnalyticsTracker.OnLevelStart(Level);
        AA_AnalyticsManager.Agent.TrackScreenView("gameplay");
        AA_AnalyticsManager.Agent.GameStartAnalytics(Level);
    }

    void PlacePlayer(Transform spawnPoint)
    {
        player.gameObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        float spawnPitch = spawnPoint.eulerAngles.x;
        if (spawnPitch > 180f) spawnPitch -= 360f;
        player.InitializeCameraPitch(spawnPitch);
    }

    // The same starting point for every level; modules then change only what their level needs.
    void ResetHouse()
    {
        foreach (DropPoint point in allDropPoints)
        {
            if (point != null)
                point.gameObject.SetActive(false);
        }

        if (baby != null)
            baby.babyEyesRed.color = Color.white;

        if (nanny == null)
            nanny = FindFirstObjectByType<NannyStateManager>(FindObjectsInactive.Include);
        // She waits hidden until her module brings her in, so she can't be stumbled on early.
        if (nanny != null)
            nanny.gameObject.SetActive(false);
    }

    public void OnInteractableInteract(ItemType itemType)
    {
        if (itemType == ItemType.UpperRoomDoor)
            ObjectiveUIController.OnTaskEventReceived(TaskType.CheckBabyRoom);
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
        if (_levelEnded) return;
        _levelEnded = true;

        _runner?.End(won: true);

        TweenUtilities.DelayedCall(2f, () =>
        {
            UIManager.Instance.LevelComplete();
            AudioManager.Instance.PlaySFX(SFX.LevelComplete);

            // openLevels counts the levels unlocked after the first one.
            int currentOpen = GamePreference.openLevels;
            if (currentOpen < LevelConfigLoader.LevelCount - 1 && Level == currentOpen + 1)
                GamePreference.openLevels = currentOpen + 1;

            AA_AnalyticsManager.Agent.GameCompleteAnalytics(Level);
            int rateUsLevel = FirebaseManager.GameSettings.rate_us_level;
            if (rateUsLevel > 0 && Level == rateUsLevel)
                ArcadiaSdkManager.Agent.ShowRateUs();
        }, this);
    }

    /// <summary>Fired when the player is killed, so UI and audio can react without a hard reference.</summary>
    public static event Action OnLevelFailed;

    public void LevelFailed()
    {
        if (_levelEnded) return;
        _levelEnded = true;

        _runner?.End(won: false);

        AA_AnalyticsManager.Agent.GameFailAnalytics(Level);
        OnLevelFailed?.Invoke();

        // Delayed so the unconscious collapse animation reads before the panel covers it.
        TweenUtilities.DelayedCall(2.5f, () => UIManager.Instance.LevelFailed(), this);
    }

    void OnTaskReceived(TaskType taskType) => _runner?.TaskCompleted(taskType);
}
