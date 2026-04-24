using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example script showing how to use age verification data in your game features.
/// This demonstrates COPPA-compliant feature toggling based on user age.
/// </summary>
public class AgeBasedFeatureManager : MonoBehaviour
{
    [Header("Child-Safe Features")]
    [Tooltip("Features available for users under 13")]
    public GameObject[] childSafeFeatures;
    
    [Header("Teen Features")]
    [Tooltip("Features available for users 13-17")]
    public GameObject[] teenFeatures;
    
    [Header("Adult Features")]
    [Tooltip("Features available for users 18+")]
    public GameObject[] adultFeatures;
    
    [Header("Social Features")]
    [Tooltip("Chat, messaging, social sharing - disabled for children")]
    public GameObject[] socialFeatures;
    
    [Header("Analytics Features")]
    [Tooltip("Advanced analytics - limited for children")]
    public MonoBehaviour[] analyticsComponents;
    
    [Header("In-App Purchase Features")]
    [Tooltip("Purchase buttons and stores - require parental consent for children")]
    public GameObject[] purchaseFeatures;
    
    void Start()
    {
        // Subscribe to age verification events
        UserAgeService.OnAgeVerified += OnAgeVerified;
        UserAgeService.OnCoppaStatusDetermined += OnCoppaStatusDetermined;
        
        // If age is already verified, apply settings immediately
        if (UserAgeService.Instance != null && UserAgeService.Instance.IsAgeVerified)
        {
            ApplyAgeBasedSettings(UserAgeService.Instance.UserAge, UserAgeService.Instance.IsUnderCoppaAge);
        }
        else
        {
            // Disable all age-restricted features until verification
            DisableAllFeatures();
        }
    }
    
    private void OnAgeVerified(int userAge)
    {
        bool isChild = userAge < 13;
        ApplyAgeBasedSettings(userAge, isChild);
    }
    
    private void OnCoppaStatusDetermined(bool isUnderCoppaAge)
    {
        Debug.Log($"COPPA status determined: {(isUnderCoppaAge ? "Child" : "Not child")}");
    }
    
    private void ApplyAgeBasedSettings(int userAge, bool isChild)
    {
        Debug.Log($"Applying age-based settings for age: {userAge}");
        
        // Enable appropriate features based on age
        if (isChild) // Under 13 - COPPA protected
        {
            EnableChildSafeMode();
        }
        else if (userAge < 18) // 13-17 - Teen mode
        {
            EnableTeenMode();
        }
        else // 18+ - Full features
        {
            EnableAdultMode();
        }
        
        // Configure social features
        ConfigureSocialFeatures(isChild);
        
        // Configure analytics
        ConfigureAnalytics(isChild);
        
        // Configure purchases
        ConfigurePurchases(isChild);
    }
    
    private void DisableAllFeatures()
    {
        Debug.Log("Disabling all features until age verification");
        
        SetFeaturesActive(childSafeFeatures, false);
        SetFeaturesActive(teenFeatures, false);
        SetFeaturesActive(adultFeatures, false);
        SetFeaturesActive(socialFeatures, false);
        SetFeaturesActive(purchaseFeatures, false);
    }
    
    private void EnableChildSafeMode()
    {
        Debug.Log("Enabling child-safe mode (COPPA compliant)");
        
        SetFeaturesActive(childSafeFeatures, true);
        SetFeaturesActive(teenFeatures, false);
        SetFeaturesActive(adultFeatures, false);
        
        // Show child-safe content only
        ShowChildSafeContent();
    }
    
    private void EnableTeenMode()
    {
        Debug.Log("Enabling teen mode");
        
        SetFeaturesActive(childSafeFeatures, true);
        SetFeaturesActive(teenFeatures, true);
        SetFeaturesActive(adultFeatures, false);
        
        // Show age-appropriate content
        ShowTeenContent();
    }
    
    private void EnableAdultMode()
    {
        Debug.Log("Enabling adult mode - all features available");
        
        SetFeaturesActive(childSafeFeatures, true);
        SetFeaturesActive(teenFeatures, true);
        SetFeaturesActive(adultFeatures, true);
        
        // Show all content
        ShowAllContent();
    }
    
    private void ConfigureSocialFeatures(bool isChild)
    {
        // Disable social features for children (COPPA requirement)
        bool enableSocial = !isChild;
        SetFeaturesActive(socialFeatures, enableSocial);
        
        if (isChild)
        {
            Debug.Log("Social features disabled for child user (COPPA compliance)");
        }
        else
        {
            Debug.Log("Social features enabled for non-child user");
        }
    }
    
    private void ConfigureAnalytics(bool isChild)
    {
        // Limit analytics data collection for children
        foreach (var analyticsComponent in analyticsComponents)
        {
            if (analyticsComponent != null)
            {
                // You can implement IChildSafeAnalytics interface or use component-specific methods
                if (analyticsComponent is MonoBehaviour component)
                {
                    // Example: Disable detailed analytics for children
                    component.enabled = !isChild;
                }
            }
        }
        
        Debug.Log($"Analytics configured for {(isChild ? "child" : "adult")} user");
    }
    
    private void ConfigurePurchases(bool isChild)
    {
        // Children require parental consent for purchases
        if (isChild)
        {
            // Enable purchase features but add parental consent flow
            SetFeaturesActive(purchaseFeatures, true);
            AddParentalConsentToPurchases();
            Debug.Log("Purchase features enabled with parental consent requirement");
        }
        else
        {
            // Normal purchase flow for adults
            SetFeaturesActive(purchaseFeatures, true);
            Debug.Log("Purchase features enabled without restrictions");
        }
    }
    
    private void AddParentalConsentToPurchases()
    {
        // Add parental consent flow to purchase buttons
        foreach (var purchaseFeature in purchaseFeatures)
        {
            if (purchaseFeature != null)
            {
                Button purchaseButton = purchaseFeature.GetComponent<Button>();
                if (purchaseButton != null)
                {
                    // Remove existing listeners and add parental consent check
                    purchaseButton.onClick.RemoveAllListeners();
                    purchaseButton.onClick.AddListener(() => RequestParentalConsent());
                }
            }
        }
    }
    
    private void RequestParentalConsent()
    {
        Debug.Log("Requesting parental consent for purchase...");
        // Implement parental consent flow here
        // This could involve email verification, PIN entry, etc.
        
        // Example implementation:
        ShowParentalConsentDialog();
    }
    
    private void ShowParentalConsentDialog()
    {
        // Show a dialog explaining that parental consent is required
        Debug.Log("Showing parental consent dialog");
        
        // You can implement this with Unity's UI system or a third-party solution
        // For now, we'll just log the requirement
        string consentMessage = "This purchase requires parental consent. " +
                              "Please have a parent or guardian complete the purchase verification.";
        
        Debug.Log(consentMessage);
        
        // In a real implementation, you might:
        // 1. Show a dialog with the consent message
        // 2. Request parent email for verification
        // 3. Send a verification code to the parent
        // 4. Only allow purchase after verification
    }
    
    private void ShowChildSafeContent()
    {
        Debug.Log("Configuring content for child-safe viewing");
        // Implement child-safe content filtering here
        // - Remove violence, inappropriate language
        // - Show educational content
        // - Use bright, friendly colors
    }
    
    private void ShowTeenContent()
    {
        Debug.Log("Configuring content for teen viewing");
        // Implement teen-appropriate content
        // - Mild competitive elements
        // - Social features with moderation
        // - Age-appropriate challenges
    }
    
    private void ShowAllContent()
    {
        Debug.Log("Configuring content for adult viewing");
        // Show all available content
        // - Full competitive features
        // - All social features
        // - Complete game experience
    }
    
    private void SetFeaturesActive(GameObject[] features, bool active)
    {
        if (features == null) return;
        
        foreach (var feature in features)
        {
            if (feature != null)
            {
                feature.SetActive(active);
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        UserAgeService.OnAgeVerified -= OnAgeVerified;
        UserAgeService.OnCoppaStatusDetermined -= OnCoppaStatusDetermined;
    }
    
    // Public methods for external control
    public void RefreshAgeBasedSettings()
    {
        if (UserAgeService.Instance != null && UserAgeService.Instance.IsAgeVerified)
        {
            ApplyAgeBasedSettings(UserAgeService.Instance.UserAge, UserAgeService.Instance.IsUnderCoppaAge);
        }
    }
    
    public bool CanAccessFeature(string featureName)
    {
        if (UserAgeService.Instance == null || !UserAgeService.Instance.IsAgeVerified)
        {
            return false; // No access until age verified
        }
        
        int userAge = UserAgeService.Instance.UserAge;
        
        // Example feature restrictions
        switch (featureName.ToLower())
        {
            case "chat":
            case "social":
                return userAge >= 13; // No social features for children
                
            case "purchase":
                return true; // Available but with parental consent for children
                
            case "analytics":
                return userAge >= 13; // Limited analytics for children
                
            case "competitive":
                return userAge >= 10; // Age-appropriate competitive features
                
            default:
                return true; // Most features available by default
        }
    }
}
