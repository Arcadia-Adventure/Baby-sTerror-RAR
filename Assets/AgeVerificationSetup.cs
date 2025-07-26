using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sample script demonstrating how to set up the age verification system in your game.
/// Add this script to a GameObject in your first scene (before the main menu).
/// </summary>
public class AgeVerificationSetup : MonoBehaviour
{
    [Header("Age Verification Setup")]
    [Tooltip("Automatically create UserAgeService if not found")]
    public bool autoCreateAgeService = true;
    
    [Tooltip("Scene to load after age verification is complete")]
    public string nextSceneName = "MainMenu";
    
    [Header("Optional UI References")]
    [Tooltip("Optional: Pre-designed age verification canvas")]
    public Canvas customAgeCanvas;
    
    [Tooltip("Optional: Custom age slider")]
    public Slider customAgeSlider;
    
    [Tooltip("Optional: Custom confirm button")]
    public Button customConfirmButton;
    
    void Start()
    {
        SetupAgeVerification();
    }
    
    private void SetupAgeVerification()
    {
        // Ensure UserAgeService exists
        if (UserAgeService.Instance == null && autoCreateAgeService)
        {
            GameObject ageServiceGO = new GameObject("UserAgeService");
            UserAgeService ageService = ageServiceGO.AddComponent<UserAgeService>();
            
            // Assign custom UI if provided through Inspector
            if (customAgeCanvas != null)
            {
                ageService.ageScreenCanvas = customAgeCanvas;
                Debug.Log("Age Verification: Custom canvas assigned from Inspector");
            }
            if (customAgeSlider != null)
            {
                ageService.ageSlider = customAgeSlider;
                Debug.Log("Age Verification: Custom slider assigned from Inspector");
            }
            if (customConfirmButton != null)
            {
                ageService.confirmButton = customConfirmButton;
                Debug.Log("Age Verification: Custom confirm button assigned from Inspector");
            }
            
            // If no UI is provided, warn the user
            if (customAgeCanvas == null || customAgeSlider == null || customConfirmButton == null)
            {
                Debug.LogWarning("Age Verification: Some UI elements are missing! Please assign Age Screen Canvas, Slider, and Confirm Button in the Inspector.");
            }
        }
        
        // Subscribe to age verification completion
        UserAgeService.OnAgeVerified += OnAgeVerificationComplete;
        UserAgeService.OnCoppaStatusDetermined += OnCoppaStatusDetermined;
        
        // If age is already verified, proceed immediately
        if (UserAgeService.Instance != null && UserAgeService.Instance.IsAgeVerified)
        {
            OnAgeVerificationComplete(UserAgeService.Instance.UserAge);
        }
    }
    
    private void OnAgeVerificationComplete(int userAge)
    {
        Debug.Log($"Age verification complete! User age: {userAge}");
        
        // Wait a moment before loading next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Invoke(nameof(LoadNextScene), 1f);
        }
    }
    
    private void OnCoppaStatusDetermined(bool isUnderCoppaAge)
    {
        Debug.Log($"COPPA compliance status: {(isUnderCoppaAge ? "Child-directed content" : "General audience")}");
        
        // You can add additional logic here based on COPPA status
        // For example, disable certain features for children
        if (isUnderCoppaAge)
        {
            // Disable chat systems, social features, etc.
            Debug.Log("Child-directed mode activated - social features disabled");
        }
    }
    
    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        UserAgeService.OnAgeVerified -= OnAgeVerificationComplete;
        UserAgeService.OnCoppaStatusDetermined -= OnCoppaStatusDetermined;
    }
    
    // Public methods for UI buttons or other scripts
    public void ResetAgeVerification()
    {
        if (UserAgeService.Instance != null)
        {
            UserAgeService.Instance.ResetAgeVerification();
        }
    }
    
    public void SetTestAge(int age)
    {
        if (UserAgeService.Instance != null)
        {
            UserAgeService.Instance.SetAge(age);
        }
    }
}
