using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ControlFreak2;
using ControlFreak2.UI;
using Ommy.Prefs;
using Ommy.Audio;
using Ommy.Singleton;

public class UIManager : Singleton<UIManager>
{
    [Header("Level Complete")]
    public GameObject nextButton;
    public GameObject rateusButton;
    public GameObject levelCompletePanel;

    [Header("Next Night Teaser")]
    [Tooltip("Group on the win screen that previews the next night, to pull the player into it.")]
    public GameObject nextNightGroup;
    public Image nextNightThumbnail;
    public TextMeshProUGUI nextNightTitle;
    public TextMeshProUGUI nextNightTeaser;

    [Header("Level Fail")]
    [Tooltip("Optional until the fail screen is built. Reuse ReplayBtn and HomeBtn for its buttons.")]
    public GameObject levelFailPanel;

    [Header("Pause")]
    public GameObject pausePanel;

    [Header("Crosshair")]
    public Sprite knobImage;
    public Sprite doorOpenImage;
    public Sprite doorCloseImage;
    public Sprite pickImage;
    public Sprite dropImage;
    [Tooltip("Crosshair for Interact prompts (phone, collectible...). Falls back to the pick icon.")]
    public Sprite interactImage;
    [Tooltip("Crosshair for Hide prompts. Falls back to the door icon.")]
    public Sprite hideImage;
    public Image crossHairDetection;
    public TextMeshProUGUI detectionTxt;
    public RectTransform rt;

    [Header("Touch Buttons")]
    public TouchButtonSpriteAnimator door;
    public TouchButtonSpriteAnimator pick;
    public TouchButtonSpriteAnimator useDevice;
    [Tooltip("Optional until the button exists in the scene. Presses E.")]
    public TouchButtonSpriteAnimator interact;
    [Tooltip("Optional until the button exists in the scene. Presses C.")]
    public TouchButtonSpriteAnimator crouch;
    [Tooltip("Optional until the button exists in the scene. Presses L.")]
    public TouchButtonSpriteAnimator flashlight;
    [Tooltip("Optional until the button exists in the scene. Holds LeftShift.")]
    public TouchButtonSpriteAnimator sprint;

    [Header("Touch Button Sprites")]
    public Sprite interactSprite;
    public Sprite exitHideSprite;
    public Sprite crouchSprite;
    public Sprite standSprite;
    public Sprite flashlightOnSprite;
    public Sprite flashlightOffSprite;

    [Header("Settings")]
    public FirstPersonController fps;
    public Slider sl;

    CrosshairState currentCrosshairState = CrosshairState.None;

    public CrosshairState CurrentCrosshairState => currentCrosshairState;

    private void Start()
    {
        Time.timeScale = 1f;
        sl.value = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
        fps.mouseSensitivity = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
        crossHairDetection.sprite = knobImage;
        detectionTxt.text = null;

        SetInteractButton(false);
        SetFlashlightButtonVisible(false);
        SetCrouchState(false);
        SetCrouchButtonVisible(true);
        SetSprintButtonVisible(true);
    }

    #region Crosshair

    public void SetCrosshair(CrosshairState state, string detectionText)
    {
        detectionTxt.text = detectionText;
        if (currentCrosshairState == state) return;
        currentCrosshairState = state;
        TweenUtilities.Kill(crossHairDetection);

        switch (state)
        {
            case CrosshairState.None:
                rt.sizeDelta = new Vector2(20, 20);
                crossHairDetection.sprite = knobImage;
                // Keep any text passed in, e.g. "Need Feeder" or "Put the baby down to hide".
                detectionTxt.text = detectionText;
                break;
            case CrosshairState.Interact:
                SetCrosshairSprite(interactImage != null ? interactImage : pickImage);
                break;
            case CrosshairState.Hide:
                SetCrosshairSprite(hideImage != null ? hideImage : doorOpenImage);
                break;
            case CrosshairState.Pick:
                SetCrosshairSprite(pickImage);
                break;
            case CrosshairState.Drop:
                SetCrosshairSprite(dropImage);
                break;
            case CrosshairState.DoorOpen:
                SetCrosshairSprite(doorOpenImage);
                break;
            case CrosshairState.DoorClose:
                SetCrosshairSprite(doorCloseImage);
                break;
        }
    }

    void SetCrosshairSprite(Sprite sprite)
    {
        rt.sizeDelta = new Vector2(50, 50);
        crossHairDetection.sprite = sprite;
        TweenUtilities.Fade(crossHairDetection, 1f, 1f);
    }

    #endregion

    #region Button Visibility

    public void SetDoorButtonVisible(bool visible) => SetButtonVisible(door, visible);
    public void SetPickButtonVisible(bool visible) => SetButtonVisible(pick, visible);
    public void SetUseDeviceButtonVisible(bool visible) => SetButtonVisible(useDevice, visible);
    public void SetCrouchButtonVisible(bool visible) => SetButtonVisible(crouch, visible);
    public void SetFlashlightButtonVisible(bool visible) => SetButtonVisible(flashlight, visible);
    public void SetSprintButtonVisible(bool visible) => SetButtonVisible(sprint, visible);

    /// <summary>Shows or hides the Interact button, with an optional icon for what it'll do.</summary>
    public void SetInteractButton(bool visible, Sprite sprite = null)
    {
        if (interact == null)
            return;

        if (visible)
        {
            Sprite icon = sprite != null ? sprite : interactSprite;
            if (icon != null)
                interact.SetSprite(icon);
        }

        SetButtonVisible(interact, visible);
    }

    public void SetCrouchState(bool crouched)
    {
        Sprite icon = crouched ? standSprite : crouchSprite;
        if (crouch != null && icon != null)
            crouch.SetSprite(icon);
    }

    public void SetFlashlightState(bool on)
    {
        Sprite icon = on ? flashlightOnSprite : flashlightOffSprite;
        if (flashlight != null && icon != null)
            flashlight.SetSprite(icon);
    }

    void HideAllActionButtons()
    {
        SetDoorButtonVisible(false);
        SetPickButtonVisible(false);
        SetUseDeviceButtonVisible(false);
        SetInteractButton(false);
        SetCrouchButtonVisible(false);
        SetSprintButtonVisible(false);
        SetFlashlightButtonVisible(false);
    }

    void SetButtonVisible(TouchButtonSpriteAnimator button, bool visible)
    {
        // The newer buttons are optional until they're added to the scene.
        if (button == null)
            return;

        var img = button.image;
        float targetAlpha = visible ? 1f : 0f;
        button.transform.parent.GetComponent<TouchButton>().enabled = visible;

        if (Mathf.Approximately(img.color.a, targetAlpha)) return;

        TweenUtilities.Fade(img, targetAlpha, 0.3f);
    }

    #endregion

    #region Game UI Actions

    bool _sceneChangeInProgress;

    public void LevelComplete()
    {
        HideAllActionButtons();
        levelCompletePanel.SetActive(true);
        LevelDefinition next = LevelConfigLoader.Database.GetNext(GamePreference.selectedLevel);
        bool isLastLevel = next == null;
        nextButton.SetActive(!isLastLevel);
        rateusButton.SetActive(isLastLevel);
        ShowNextNightTeaser(next);
        ArcadiaSdkManager.Agent.PrepareRewarded();
        ArcadiaSdkManager.Agent.PrepareInterstitial();
    }

    /// <summary>The cliffhanger: the next night's name, picture and hook, or the ending after the last.</summary>
    void ShowNextNightTeaser(LevelDefinition next)
    {
        if (nextNightGroup == null)
            return;

        LevelDatabase database = LevelConfigLoader.Database;
        string title;
        string teaser;
        Sprite thumbnail = null;

        if (next != null)
        {
            title = $"Night {next.level}: {next.missionName}";
            teaser = next.teaser;
            thumbnail = next.thumbnail;
        }
        else
        {
            title = database.endingTitle;
            teaser = database.endingText;
        }

        bool hasContent = !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(teaser);
        nextNightGroup.SetActive(hasContent);
        if (!hasContent)
            return;

        if (nextNightTitle != null)
            nextNightTitle.text = title;
        if (nextNightTeaser != null)
            nextNightTeaser.text = teaser;
        if (nextNightThumbnail != null)
        {
            nextNightThumbnail.sprite = thumbnail;
            nextNightThumbnail.gameObject.SetActive(thumbnail != null);

            // The picture covers its masked slot, so it needs the sprite's own shape to crop instead of stretch.
            if (thumbnail != null && nextNightThumbnail.TryGetComponent(out AspectRatioFitter fitter))
                fitter.aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
        }

        if (next != null)
            AA_AnalyticsManager.Agent.CustomEvent("teaser_shown", "night_" + next.level);
    }

    public void LevelFailed()
    {
        SetCrosshair(CrosshairState.None, null);
        HideAllActionButtons();

        if (levelFailPanel == null)
        {
            Debug.LogWarning("[UIManager] Level failed, but no levelFailPanel is assigned yet.");
            return;
        }

        levelFailPanel.SetActive(true);
        ArcadiaSdkManager.Agent.PrepareRewarded();
    }

    public void RateUsClick()
    {
        Application.OpenURL("market://details?id=" + Application.identifier);
        AA_AnalyticsManager.Agent.TrackButtonClick("rate_us");
    }

    #endregion

    #region Pause & Navigation

    public void DoorOpenCloseBtn()
    {
        PickDropController.Instance.DoorOpenCloseBtn();
    }

    public void PauseBtn()
    {
        Time.timeScale = 0;
        pausePanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.PauseAll();
        AudioManager.Instance.SetBGSetting(true);
        AudioManager.Instance.StartGame();
        AA_AnalyticsManager.Agent.TrackButtonClick("pause");
        ArcadiaSdkManager.CurrentAdPlacement = "pause_interstitial";
        ArcadiaSdkManager.Agent.ShowInterstitialAd();
    }

    public void ResumeBtn()
    {
        Time.timeScale = 1;
        pausePanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFX.Click);
        AudioManager.Instance.ResumeAll();
        AudioManager.Instance.GameEnd();
        AudioManager.Instance.SetBGSetting(false);
        AA_AnalyticsManager.Agent.TrackButtonClick("resume");
        ArcadiaSdkManager.Agent.ShowBanner(BannerScreen.Gameplay);
    }

    public void HomeBtn()
    {
        if (_sceneChangeInProgress) return;
        _sceneChangeInProgress = true;
        Time.timeScale = 1;
        KillUiTweens();
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "home_button");
        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void ReplayBtn()
    {
        if (_sceneChangeInProgress) return;
        _sceneChangeInProgress = true;
        Time.timeScale = 1;
        KillUiTweens();
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackLevelRetry(GamePreference.selectedLevel);
        ArcadiaSdkManager.CurrentAdPlacement = "replay_rewarded";
        ShowRewardedThenLoadScene();
    }

    public void NextBtn()
    {
        if (_sceneChangeInProgress) return;
        _sceneChangeInProgress = true;
        Time.timeScale = 1;
        KillUiTweens();
        GamePreference.selectedLevel++;
        AudioManager.Instance.PlaySFX(SFX.Click);
        AA_AnalyticsManager.Agent.TrackButtonClick("next_level");
        ArcadiaSdkManager.CurrentAdPlacement = "next_interstitial";
        ArcadiaSdkManager.Agent.ShowInterstitialAd(LoadCurrentScene, LoadCurrentScene);
    }

    void ShowRewardedThenLoadScene()
    {
        if (ArcadiaSdkManager.Agent != null && !ArcadiaSdkManager.Agent.removeAds)
        {
            ArcadiaSdkManager.Agent.ShowRewardedAd(
                _ => LoadCurrentScene(),
                LoadCurrentScene);
            return;
        }

        LoadCurrentScene();
    }

    void LoadCurrentScene()
    {
        KillUiTweens();
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void SensivitySlider()
    {
        PlayerPrefs.SetFloat(PrefKeys.MouseSensitivity, sl.value);
        fps.mouseSensitivity = PlayerPrefs.GetFloat(PrefKeys.MouseSensitivity);
    }

    #endregion

    #region Lifecycle

    void KillUiTweens()
    {
        TweenUtilities.Kill(crossHairDetection);
        if (door != null) TweenUtilities.Kill(door.image);
        if (pick != null) TweenUtilities.Kill(pick.image);
        if (useDevice != null) TweenUtilities.Kill(useDevice.image);
        if (interact != null) TweenUtilities.Kill(interact.image);
        if (crouch != null) TweenUtilities.Kill(crouch.image);
        if (flashlight != null) TweenUtilities.Kill(flashlight.image);
        if (sprint != null) TweenUtilities.Kill(sprint.image);
    }

    private void OnDisable()
    {
        KillUiTweens();
    }

    private void OnDestroy()
    {
        KillUiTweens();
    }

    private void OnApplicationQuit()
    {
        AA_AnalyticsManager.Agent.TrackLevelAbandon(GamePreference.selectedLevel, "app_quit");
        AA_AnalyticsManager.Agent.TrackSessionEnd("app_quit", AnalyticsTracker.GetCurrentScene());
    }

    #endregion
}
