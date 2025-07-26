using UnityEngine;
using UnityEngine.UI;
using System;

public class UserAgeService : MonoBehaviour
{
    [Header("Age Screen UI")]
    public Canvas ageScreenCanvas;
    public Slider ageSlider;
    public Text ageDisplayText; // Shows the current slider value
    public Button confirmButton;
    public Text instructionText;
    public Text errorText;
    
    [Header("Settings")]
    public int minimumAge = 1;
    public int maximumAge = 120;
    public int coppaAgeLimit = 13; // Under 13 is considered child under COPPA
    
    // Events
    public static event Action<int> OnAgeVerified;
    public static event Action<bool> OnCoppaStatusDetermined; // true if user is under COPPA age limit
    
    // Singleton
    public static UserAgeService Instance { get; private set; }
    
    // Private variables
    private int userAge = -1;
    private bool isAgeVerified = false;
    
    // Properties
    public int UserAge => userAge;
    public bool IsAgeVerified => isAgeVerified;
    public bool IsUnderCoppaAge => userAge > 0 && userAge < coppaAgeLimit;
    public bool IsChildDirected => IsUnderCoppaAge;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAgeScreen();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Check if age was previously verified
        if (HasPreviouslyVerifiedAge())
        {
            LoadPreviousAge();
            HideAgeScreen();
            TriggerAgeVerificationEvents();
        }
        else
        {
            ShowAgeScreen();
        }
    }
    
    private void InitializeAgeScreen()
    {
        // Validate that required UI components are assigned
        if (ageScreenCanvas == null)
        {
            Debug.LogError("UserAgeService: Age Screen Canvas is not assigned! Please assign it in the Inspector.");
            return;
        }
        
        if (ageSlider == null)
        {
            Debug.LogError("UserAgeService: Age Slider is not assigned! Please assign it in the Inspector.");
            return;
        }
        
        if (confirmButton == null)
        {
            Debug.LogError("UserAgeService: Confirm Button is not assigned! Please assign it in the Inspector.");
            return;
        }
        
        // Setup slider
        ageSlider.minValue = minimumAge;
        ageSlider.maxValue = maximumAge;
        ageSlider.wholeNumbers = true;
        ageSlider.value = 20;
        
        // Setup event listeners
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        ageSlider.onValueChanged.AddListener(OnAgeSliderChanged);
        
        SetupUIText();
        UpdateAgeDisplay();
    }
    
    private void SetupUIText()
    {
        if (instructionText != null)
        {
            instructionText.text = "Please select your age to continue:";
        }
        
        if (errorText != null)
        {
            errorText.text = "";
        }
    }
    
    private void UpdateAgeDisplay()
    {
        if (ageDisplayText != null && ageSlider != null)
        {
            ageDisplayText.text = $"Age: {(int)ageSlider.value}";
        }
    }
    
    private void OnAgeSliderChanged(float value)
    {
        UpdateAgeDisplay();
        
        // Clear any error messages when slider changes
        if (errorText != null)
        {
            errorText.text = "";
        }
    }
    
    private void OnConfirmButtonClicked()
    {
        if (ValidateAge())
        {
            SaveAge();
            HideAgeScreen();
            TriggerAgeVerificationEvents();
        }
    }
    
    private void OnAgeInputChanged(string value)
    {
        if (errorText != null)
        {
            errorText.text = "";
        }
    }
    
    private bool ValidateAge()
    {
        if (ageSlider == null)
        {
            ShowError("Age slider is not configured properly.");
            return false;
        }
        
        int age = (int)ageSlider.value;
        
        if (age < minimumAge || age > maximumAge)
        {
            ShowError($"Please select an age between {minimumAge} and {maximumAge}.");
            return false;
        }
        
        userAge = age;
        return true;
    }
    
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
        Debug.LogWarning($"Age Verification Error: {message}");
    }
    
    private void SaveAge()
    {
        PlayerPrefs.SetInt("UserAge", userAge);
        PlayerPrefs.SetInt("AgeVerified", 1);
        PlayerPrefs.Save();
        isAgeVerified = true;
        
        Debug.Log($"Age verified and saved: {userAge} (COPPA Status: {(IsUnderCoppaAge ? "Child" : "Adult")})");
    }
    
    private bool HasPreviouslyVerifiedAge()
    {
        return PlayerPrefs.GetInt("AgeVerified", 0) == 1;
    }
    
    private void LoadPreviousAge()
    {
        userAge = PlayerPrefs.GetInt("UserAge", -1);
        isAgeVerified = userAge > 0;
        
        Debug.Log($"Loaded previous age: {userAge} (COPPA Status: {(IsUnderCoppaAge ? "Child" : "Adult")})");
    }
    
    private void ShowAgeScreen()
    {
        if (ageScreenCanvas != null)
        {
            ageScreenCanvas.gameObject.SetActive(true);
            Debug.Log("Age verification screen shown");
        }
    }
    
    private void HideAgeScreen()
    {
        if (ageScreenCanvas != null)
        {
            ageScreenCanvas.gameObject.SetActive(false);
            Debug.Log("Age verification screen hidden");
        }
    }
    
    private void TriggerAgeVerificationEvents()
    {
        OnAgeVerified?.Invoke(userAge);
        OnCoppaStatusDetermined?.Invoke(IsUnderCoppaAge);
        
        Debug.Log($"Age verification events triggered for age: {userAge}");
    }
    
    // Public methods for external access
    public void ResetAgeVerification()
    {
        PlayerPrefs.DeleteKey("UserAge");
        PlayerPrefs.DeleteKey("AgeVerified");
        PlayerPrefs.Save();
        
        userAge = -1;
        isAgeVerified = false;
        
        ShowAgeScreen();
        
        Debug.Log("Age verification reset");
    }
    
    public void SetAge(int age)
    {
        if (age >= minimumAge && age <= maximumAge)
        {
            userAge = age;
            SaveAge();
            HideAgeScreen();
            TriggerAgeVerificationEvents();
        }
        else
        {
            Debug.LogError($"Invalid age: {age}. Must be between {minimumAge} and {maximumAge}");
        }
    }
    
    // Get COPPA compliance settings for SDK initialization
    public COPPASettings GetCOPPASettings()
    {
        return new COPPASettings
        {
            isChildDirected = IsChildDirected,
            tagForChildDirectedTreatment = IsChildDirected,
            tagForUnderAgeOfConsent = IsUnderCoppaAge,
            userAge = userAge,
            maxAdContentRating = IsChildDirected ? "G" : "PG"
        };
    }
}

[System.Serializable]
public class COPPASettings
{
    public bool isChildDirected;
    public bool tagForChildDirectedTreatment;
    public bool tagForUnderAgeOfConsent;
    public int userAge;
    public string maxAdContentRating;
}
