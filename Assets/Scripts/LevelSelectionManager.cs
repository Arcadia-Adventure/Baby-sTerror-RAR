using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Ommy.Singleton;
using Ommy.Prefs;
using DG.Tweening;
using Ommy.Attributes;
using Ommy.Audio;

public class LevelSelectionManager : Singleton<LevelSelectionManager>
{
    public ScrollRect scrollView;

    [Header("Level Buttons")]
    [Tooltip("Inactive button, copied once for every level in the LevelDatabase.")]
    public LevelButton buttonTemplate;
    [Tooltip("Tint for the newest unlocked level.")]
    public Color newestLevelColor = Color.red;

    [Header("Scroll Animation")]
    public float scrollDuration = 0.8f;
    public Ease scrollEase = Ease.OutCubic;
    public float startDelay = 0.3f;
    public GameObject loadingScreen;
    public UnityEvent onPurchaseAllLevels;
    public Image unlockAllLevelsButton;

    // Legacy: read once by Baby's Terror > Migrate Scenes To Level System.
    [HideInInspector] public GameObject[] lockSprite;
    [HideInInspector] public GameObject[] barImg;

    readonly List<LevelButton> _buttons = new();

    // Level 1 is always open; GamePreference.openLevels counts the levels unlocked after it.
    static int LockableLevels => Mathf.Max(0, LevelConfigLoader.LevelCount - 1);

    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt(PrefKeys.UnlockAllLevels, 1);
        unlockAllLevelsButton.enabled = false;
        UnlockLevelsIfNeeded();
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(PrefKeys.UnlockAllLevels) == 1)
        {
            unlockAllLevelsButton.enabled = false;
        }
        ArcadiaSdkManager.Agent.ShowBanner(BannerScreen.LevelSelect);
        BuildButtons();
        UnlockLevelsIfNeeded();
        MoveContentView();
        AA_AnalyticsManager.Agent.TrackScreenView("level_select");
    }

    void OnDisable()
    {
        TweenUtilities.Kill(scrollView);
    }

    void BuildButtons()
    {
        if (buttonTemplate == null)
        {
            Debug.LogError("[LevelSelectionManager] No button template. Run Baby's Terror > Migrate Scenes To Level System.", this);
            return;
        }

        buttonTemplate.gameObject.SetActive(false);
        Transform parent = buttonTemplate.transform.parent;
        foreach (LevelDefinition definition in LevelConfigLoader.Database.Levels)
        {
            if (definition == null)
                continue;

            LevelButton button = Instantiate(buttonTemplate, parent);
            button.Bind(definition, LevelSelectBtn);
            button.gameObject.SetActive(true);
            _buttons.Add(button);
        }
    }

    [InspectorButton("MoveContentView")]
    public void MoveContentView()
    {
        int openLevel = GamePreference.openLevels;
        int totalLevels = LockableLevels;

        // Safety check
        if (totalLevels <= 1)
        {
            scrollView.horizontalNormalizedPosition = 0f;
            return;
        }

        // Last unlocked level index (0-based)
        int lastUnlockedIndex = Mathf.Clamp(openLevel - 1, 0, totalLevels - 1);

        // For horizontal scroll: 0 = left, 1 = right
        float targetPosition = (float)lastUnlockedIndex / (totalLevels - 1);
        targetPosition = Mathf.Clamp01(targetPosition);

        // Start from left (0) and animate to target position
        scrollView.horizontalNormalizedPosition = 0f;

        TweenUtilities.To(
            scrollView,
            () => scrollView.horizontalNormalizedPosition,
            x => { if (scrollView != null) scrollView.horizontalNormalizedPosition = x; },
            targetPosition,
            scrollDuration
        )
        .SetDelay(startDelay)
        .SetEase(scrollEase);
    }

    void UnlockLevelsIfNeeded()
    {
        int totalUnlockLevel = GamePreference.openLevels;
        if (PlayerPrefs.GetInt(PrefKeys.UnlockAllLevels) == 1)
        {
            onPurchaseAllLevels?.Invoke();
            totalUnlockLevel = LockableLevels;
        }

        // Button i is level i + 1, so it's open once i levels after the first are unlocked.
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].SetLocked(i > totalUnlockLevel);

        if (totalUnlockLevel > 0 && totalUnlockLevel < _buttons.Count)
            _buttons[totalUnlockLevel].Highlight(newestLevelColor);
    }


    public void BackBtn()
    {
        SceneManager.LoadScene("MainMenu");
        AudioManager.Instance.PlaySFX(SFX.Click);
    }


    public void LevelSelectBtn(int selectedLevel)
    {
        AA_AnalyticsManager.Agent.TrackLevelSelected(selectedLevel, GamePreference.openLevels);
        loadingScreen.SetActive(true);
        GamePreference.selectedLevel = selectedLevel;
        SceneManager.LoadSceneAsync("GamePlay");
        AudioManager.Instance.PlaySFX(SFX.Click);    
    }
}
