# Age Verification System Documentation

## Overview
This age verification system ensures COPPA (Children's Online Privacy Protection Act) compliance by collecting user age at startup and initializing SDKs with appropriate settings.

## Components

### 1. UserAgeService.cs
**Main age verification service that handles:**
- Age collection UI
- Age validation and storage
- COPPA compliance determination
- SDK configuration settings
- Event notifications

### 2. AgeVerificationSetup.cs
**Helper script for easy integration:**
- Automatic UserAgeService creation
- Scene management after verification
- Custom UI integration support
- Event handling examples

### 3. Modified SDK Managers
**Updated existing SDK managers:**
- **AdMobAdsManager.cs**: Now waits for age verification and configures ads accordingly
- **GDPRManager.cs**: Integrates age data with GDPR consent flow
- **ArcadiaSdkManager.cs**: Orchestrates the entire initialization process

## Setup Instructions

### Basic Setup (Automatic UI)
1. Add `AgeVerificationSetup.cs` to a GameObject in your first scene
2. Configure the `nextSceneName` field to your main menu scene
3. The system will automatically create the age verification UI

### Custom UI Setup
1. Create your own age verification UI with:
   - Canvas for the age screen
   - InputField for age input
   - Button for confirmation
2. Add `UserAgeService.cs` to a GameObject
3. Assign your UI elements to the UserAgeService fields
4. Add `AgeVerificationSetup.cs` and assign your custom UI elements

### Integration with Existing Code
The system automatically integrates with your existing SDK managers. No additional code changes needed for basic functionality.

## Age Verification Flow

1. **App Starts** → UserAgeService checks for previously verified age
2. **If Not Verified** → Shows age verification screen
3. **User Enters Age** → Validates and stores age
4. **Age Verified** → Triggers events and initializes SDKs
5. **SDKs Initialize** → With age-appropriate settings (COPPA compliant)

## COPPA Compliance Features

### Age Categories
- **Under 13**: Child-directed content, strict privacy settings
- **13-17**: Teen content, moderate privacy settings  
- **18+**: General/Adult content, standard privacy settings

### SDK Configuration
- **AdMob**: Automatically sets `TagForChildDirectedTreatment` and content ratings
- **GDPR**: Configures `TagForUnderAgeOfConsent` for privacy compliance
- **Analytics**: Can be configured to collect minimal data for children

### Content Filtering
- **G Rating**: For children under 13
- **PG Rating**: For teens 13-17
- **T/M Rating**: For adults 18+

## API Reference

### UserAgeService Properties
```csharp
public int UserAge { get; }                    // User's verified age
public bool IsAgeVerified { get; }             // Whether age has been verified
public bool IsUnderCoppaAge { get; }           // True if under 13
public bool IsChildDirected { get; }           // Same as IsUnderCoppaAge
public COPPASettings GetCOPPASettings()        // Returns COPPA configuration
```

### UserAgeService Events
```csharp
public static event Action<int> OnAgeVerified;           // Fired when age is verified
public static event Action<bool> OnCoppaStatusDetermined; // Fired with COPPA status
```

### UserAgeService Methods
```csharp
public void ResetAgeVerification()             // Clear stored age data
public void SetAge(int age)                    // Programmatically set age
```

### COPPASettings Structure
```csharp
public class COPPASettings
{
    public bool isChildDirected;               // Child-directed content flag
    public bool tagForChildDirectedTreatment;  // COPPA compliance flag
    public bool tagForUnderAgeOfConsent;       // GDPR compliance flag
    public int userAge;                        // User's age
    public string maxAdContentRating;          // Content rating (G/PG/T/M)
}
```

## Customization Options

### Age Limits
```csharp
public int minimumAge = 1;         // Minimum acceptable age
public int maximumAge = 120;       // Maximum acceptable age  
public int coppaAgeLimit = 13;     // COPPA age threshold
```

### UI Customization
- Modify `CreateAgeScreenUI()` method in UserAgeService
- Or provide your own UI elements via inspector
- Customize colors, fonts, and layout as needed

### Validation Rules
- Modify `ValidateAge()` method for custom validation
- Add additional checks (e.g., birth date verification)
- Implement parental consent for children

## Advanced Features

### Testing
```csharp
// Reset age verification for testing
UserAgeService.Instance.ResetAgeVerification();

// Set specific age for testing
UserAgeService.Instance.SetAge(10); // Test child mode
UserAgeService.Instance.SetAge(25); // Test adult mode
```

### Event Handling
```csharp
void Start()
{
    UserAgeService.OnAgeVerified += (age) => {
        Debug.Log($"User is {age} years old");
        // Your logic here
    };
    
    UserAgeService.OnCoppaStatusDetermined += (isChild) => {
        if (isChild) {
            // Disable social features
            // Enable parental controls
            // Show child-appropriate content
        }
    };
}
```

### SDK Integration
The system automatically configures:
- **AdMob**: Child-safe ad settings
- **Firebase Analytics**: Privacy-compliant data collection
- **AppLovin MAX**: Age-appropriate ad content
- **Custom SDKs**: Use `UserAgeService.Instance.GetCOPPASettings()`

## Legal Compliance

### COPPA Requirements
- ✅ Age verification before data collection
- ✅ Parental consent mechanisms (can be added)
- ✅ Child-safe ad content filtering
- ✅ Limited data collection for children

### GDPR Requirements  
- ✅ Age-of-consent compliance (13-16 depending on country)
- ✅ Privacy-first design
- ✅ User consent management
- ✅ Data minimization for minors

### Best Practices
1. **Collect minimal data** for users under 13
2. **Use safe content ratings** for all ages
3. **Implement parental controls** for children
4. **Regular compliance audits** of your implementation
5. **Clear privacy policies** explaining data use

## Troubleshooting

### Common Issues
1. **Age screen not showing**: Check if age was previously verified
2. **SDKs not initializing**: Ensure UserAgeService exists before SDK managers
3. **UI not responsive**: Verify Canvas and EventSystem are present
4. **Events not firing**: Check event subscription timing

### Debug Logs
Enable detailed logging in UserAgeService to track:
- Age verification status
- COPPA compliance settings
- SDK initialization timing
- UI interaction events

### Reset for Testing
```csharp
// Complete reset of age verification
PlayerPrefs.DeleteKey("UserAge");
PlayerPrefs.DeleteKey("AgeVerified");
PlayerPrefs.Save();
```

## Support and Updates
This age verification system is designed to be:
- **Future-proof**: Easy to update for new regulations
- **Modular**: Components can be modified independently  
- **Extensible**: Add new SDKs or features easily
- **Compliant**: Meets current COPPA and GDPR requirements

For additional features or compliance requirements, modify the existing components or create new ones following the established patterns.
