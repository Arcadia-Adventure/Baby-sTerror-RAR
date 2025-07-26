# Age Verification UI Setup Guide

## How to Create Your Age Verification Screen in Unity

Since the system no longer creates the UI automatically, you need to set up the age verification screen manually in Unity and assign the components through the Inspector.

### Step 1: Create the UI Hierarchy

Create the following UI structure in your scene:

```
AgeVerificationCanvas (Canvas)
├── Background (Image) - Semi-transparent black
└── MainPanel (Image) - White background panel
    ├── TitleText (Text) - "Age Verification"
    ├── InstructionText (Text) - "Please select your age to continue:"
    ├── AgeSlider (Slider) - For age selection
    │   ├── Background (Image) - Slider track
    │   ├── Fill Area (RectTransform)
    │   │   └── Fill (Image) - Slider fill
    │   └── Handle Slide Area (RectTransform)
    │       └── Handle (Image) - Slider handle
    ├── AgeDisplayText (Text) - Shows "Age: 25"
    ├── ConfirmButton (Button) - "Confirm" button
    │   └── Text (Text) - "Confirm"
    └── ErrorText (Text) - For error messages (red color)
```

### Step 2: Configure Canvas Settings

**AgeVerificationCanvas settings:**
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: 1080 x 1920 (or your preferred resolution)
- Match: 0.5 (Width or Height)
- Sorting Order: 1000 (high priority to appear over other UI)

### Step 3: Configure Age Slider

**AgeSlider settings:**
- Direction: Left To Right (horizontal slider)
- Min Value: 1 (will be set automatically by UserAgeService)
- Max Value: 120 (will be set automatically by UserAgeService)
- Whole Numbers: True
- Value: 1 (starting value)

**Slider Visual Components:**
- **Background**: Track that shows the full range
- **Fill**: Visual indicator of current value (optional)
- **Handle**: Draggable element for user interaction

### Step 4: Add Age Display Text

Create a Text component to show the current slider value:
- Position it near the slider
- The UserAgeService will automatically update this with "Age: XX" format
- Font size: 20-24px for good readability

### Step 5: Set Up UserAgeService Component

1. Create a GameObject called "UserAgeService"
2. Add the `UserAgeService` component to it
3. In the Inspector, assign:
   - **Age Screen Canvas**: Your AgeVerificationCanvas
   - **Age Slider**: Your AgeSlider component
   - **Age Display Text**: Your text component that shows current age
   - **Confirm Button**: Your ConfirmButton
   - **Instruction Text**: Your InstructionText (optional)
   - **Error Text**: Your ErrorText (optional)

### Step 6: Configure Settings (Optional)

In the UserAgeService Inspector, you can adjust:
- **Minimum Age**: Default is 1
- **Maximum Age**: Default is 120
- **COPPA Age Limit**: Default is 13 (users under this age are considered children)

### Step 7: Alternative Setup with AgeVerificationSetup

Instead of manually creating UserAgeService, you can:

1. Add `AgeVerificationSetup` component to any GameObject
2. Enable "Auto Create Age Service"
3. Assign your UI components to the "Custom" fields:
   - **Custom Age Canvas**: Your AgeVerificationCanvas
   - **Custom Age Slider**: Your AgeSlider component
   - **Custom Confirm Button**: Your ConfirmButton
4. Set "Next Scene Name" to the scene to load after verification

### Example UI Layout

```
Canvas (1080x1920)
├── Background Panel (Full Screen, Color: 0,0,0,200)
└── Main Panel (600x400, Center, Color: White)
    ├── Title (500x60, Y+140, "Age Verification", Size 32)
    ├── Instruction (500x40, Y+90, "Please select your age:", Size 18)
    ├── Age Slider (400x20, Y+40, Min: 1, Max: 120)
    ├── Age Display (200x30, Y+10, "Age: 25", Size 20)
    ├── Confirm Button (150x50, Y-40, Blue background)
    └── Error Text (500x30, Y-80, Red color, Size 16)
```

### UI Design Tips

**Colors:**
- Background: Semi-transparent black (0, 0, 0, 0.8)
- Main Panel: White or light color
- Confirm Button: Blue or your app's primary color
- Error Text: Red (#FF0000)

**Fonts:**
- Use your game's standard font
- Title: 32px, bold
- Instruction: 18px, regular
- Input: 24px, regular
- Button: 20px, bold
- Error: 16px, regular

**Slider Design:**
- Handle: Large enough for easy touch interaction (44x44px minimum)
- Track: Clear visual distinction between selected/unselected areas
- Fill: Optional color progression from green (child) to blue/red (adult)
- Labels: Consider adding age markers (13, 18, 21) below the slider

**Layout:**
- Center everything for best compatibility
- Leave adequate padding around elements
- Make touch targets at least 44x44 pixels
- Ensure text is readable on all device sizes
- Position age display text clearly visible near slider

### Testing Your Setup

1. **Test in Play Mode**: Run the scene and verify the UI appears
2. **Test Slider Interaction**: Drag the slider and verify age display updates
3. **Test Age Limits**: Check that slider respects min/max values
4. **Test Age Selection**: Try different ages (child/teen/adult ranges)
5. **Test COPPA**: Verify different behaviors for ages under/over 13

### Common Issues

**UI Not Showing:**
- Check if Canvas is enabled
- Verify Sorting Order is high enough
- Ensure Canvas is set to Screen Space Overlay

**Slider Not Working:**
- Verify Slider component is properly configured
- Check if Whole Numbers is enabled
- Ensure Min/Max values are set correctly
- Verify Handle and Fill are assigned

**Age Display Not Updating:**
- Check if Age Display Text is assigned in UserAgeService
- Verify the text component is active
- Ensure OnAgeSliderChanged event is properly connected

**Button Not Responding:**
- Verify Graphic Raycaster is on Canvas
- Check if button is Interactable
- Ensure EventSystem exists in scene
- Verify button onClick event is connected

**Components Not Assigned:**
- Check Inspector assignments on UserAgeService
- Look for error messages in Console
- Verify all required fields are filled

### Integration with Your Game

The age verification will automatically:
- ✅ Show before any other game UI
- ✅ Initialize SDKs with COPPA-compliant settings
- ✅ Configure ads based on user age
- ✅ Set up GDPR consent appropriately
- ✅ Enable/disable features based on age
- ✅ Save age for future sessions

After setup, the system handles everything automatically while using your custom-designed UI!
