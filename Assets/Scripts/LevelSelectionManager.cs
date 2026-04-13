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
    
    [Header("Scroll Animation")]
    public float scrollDuration = 0.8f;
    public Ease scrollEase = Ease.OutCubic;
    public float startDelay = 0.3f;
    public GameObject loadingScreen;
    public UnityEvent onPurchaseAllLevels;
    public Image unlockAllLevelsButton;
    public GameObject[] lockSprite;
    public GameObject[] barImg;
    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt("UnlockAllLevels", 1);
        unlockAllLevelsButton.enabled = false;
        UnlockLevelsIfNeeded();
    }
    private void Start()
    {
		ArcadiaSdkManager.Agent.HideBanner();
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            unlockAllLevelsButton.enabled= false;
        }
        ArcadiaSdkManager.Agent.ShowBanner();
        UnlockLevelsIfNeeded();
        MoveContentView();
        AA_AnalyticsManager.Agent.TrackScreenView("level_select");
    }
    void OnDisable()
    {
        int killedTweens = DOTween.KillAll(); // for kill scroll animation tween when user start game during animation
        Debug.Log("killed " + killedTweens + " tweens");
    }
    [InspectorButton("MoveContentView")]
    public void MoveContentView()
    {
        int openLevel = GamePreference.openLevels;
        int totalLevels = lockSprite.Length;
        
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
        
        // Animate scroll with DOTween
        DOTween.To(
            () => scrollView.horizontalNormalizedPosition,
            x => scrollView.horizontalNormalizedPosition = x,
            targetPosition,
            scrollDuration
        )
        .SetDelay(startDelay)
        .SetEase(scrollEase);
    }
    void UnlockLevelsIfNeeded()
    {
        int totalUnlockLevel = GamePreference.openLevels;
        if (PlayerPrefs.GetInt("UnlockAllLevels") == 1)
        {
            onPurchaseAllLevels?.Invoke();
            totalUnlockLevel = lockSprite.Length;
        }
        for (int i = 0; i < totalUnlockLevel; i++)
        {
            lockSprite[i].SetActive(false);
            barImg[i].SetActive(true);
        }
        if (totalUnlockLevel > 0 && totalUnlockLevel <= lockSprite.Length)
        lockSprite[totalUnlockLevel-1].GetComponentInParent<Image>().color = Color.red;
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
