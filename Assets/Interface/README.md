# Interface System

A dynamic menu creation system for Unity using prefabs and fluent API with configurable panel animations.

## Overview

The Interface system provides a flexible, builder-pattern-based approach to creating menu systems in Unity. It supports automatic element positioning, panel transitions with customizable animations, and a history stack for navigation.

## Structure

### Namespaces

- **`Interface`** - Main namespace for menu management and panels
- **`Interface.Element`** - Base classes and UI elements

### Core Classes

- **`MainMenuManager`** - Main manager for the menu system
- **`GUIPanel`** - Panel container with title and content area
- **`PanelBuilder`** - Fluent API builder for constructing panels
- **`PanelAnimationHandler`** - Handles panel transition animations
- **`InterfaceElement`** - Base class for all UI elements with animation support

### UI Elements

- **`GUIButton`** - Button with text and click action
- **`GUISlider`** - Slider with label and value display
- **`GUIToggle`** - Toggle switch with label
- **`GUIDropdown`** - Dropdown list with label

## Configuration

### 1. Create Prefabs

Create prefabs for each element type:
- **Panel prefab** - Must have `GUIPanel` component
- **Button prefab** - Must have `GUIButton` component
- **Slider prefab** - Must have `GUISlider` component
- **Toggle prefab** - Must have `GUIToggle` component
- **Dropdown prefab** - Must have `GUIDropdown` component

### 2. Configure Prefabs

In the Inspector, assign references to:
- `Image` components (for fade animations)
- `TextMeshProUGUI` components (for text display)
- Set element sizes (positions are generated automatically)

### 3. Setup MainMenuManager

1. Create a GameObject with `MainMenuManager` component
2. Assign all prefabs to corresponding fields
3. Set `_panelsParent` (optional, defaults to manager's transform)
4. Configure animation settings:
   - `_animationDuration` - Duration of panel transitions
   - `_panelAnimationType` - Animation type flags (Fade, Slide, or both)
   - `_slideDirection` - Direction for slide animation (Up, Down, Left, Right)
   - `_slideDistance` - Distance panels slide during transition
   - `_bounceAmount` - Bounce effect intensity for slide animations

## Panel Animation System

The system supports configurable panel animations using flags, allowing you to combine multiple animation types.

### Animation Types

**`PanelAnimationType`** (Flags enum):
- `None` - No animation
- `Fade` - Fade in/out animation
- `Slide` - Slide animation with bounce effect

Animations can be combined: `PanelAnimationType.Fade | PanelAnimationType.Slide`

### Slide Animation

The slide animation includes:
- **Bounce Effect**: Panel starts slightly in the opposite direction before sliding
- **Configurable Direction**: Up, Down, Left, or Right
- **Smooth Easing**: Uses easing functions for natural movement

### Animation Configuration

All animation settings are configured in `MainMenuManager` and automatically applied to all created panels:

```csharp
[Header("Panel Animation Settings")]
[SerializeField] private PanelAnimationType _panelAnimationType = PanelAnimationType.Fade;
[SerializeField] private SlideDirection _slideDirection = SlideDirection.Left;
[SerializeField] private float _slideDistance = 500f;
[SerializeField] private float _bounceAmount = 50f;
```

## Usage

### Basic Example

```csharp
using Interface;

public class MenuSetup : MonoBehaviour
{
    private MainMenuManager _menuManager;
    private GUIPanel _mainMenu;
    private GUIPanel _optionsPanel;

    private void Start()
    {
        _menuManager = GetComponent<MainMenuManager>();
        
        // Create panels
        _optionsPanel = _menuManager.CreatePanel("Options")
            .AddButton("Graphics", () => { /* action */ })
            .AddBackButton()
            .Build();

        _mainMenu = _menuManager.CreatePanel("Main menu")
            .AddButton("Play", () => { /* action */ })
            .AddButton("Options", _optionsPanel)
            .AddButton("Exit", () => _menuManager.ExitGame())
            .Build();

        // Show main menu
        _menuManager.ChangePanel(_mainMenu);
    }
}
```

### Options Panel Example

```csharp
// Graphics options panel
var panelOptionsGraphic = _menuManager.CreatePanel("Options - Graphic")
    .AddDropDown("Resolution", HandleResolutionChange, GetAvailableResolutions(), GetCurrentResolutionIndex)
    .AddToggle("FullScreen", HandleFullScreen, GetFullScreenCurrentMode)
    .AddDropDown("Quality", HandleQualityChange, GetAvailableQualitySettings(), GetCurrentQualityIndex)
    .AddBackButton()
    .Build();

// Audio options panel
var panelOptionsAudio = _menuManager.CreatePanel("Options - Audio")
    .AddSlider("Master volume", HandleMasterVolumeChange, GetMasterVolume)
    .AddSlider("Music volume", HandleMusicVolumeChange, GetMusicVolume)
    .AddBackButton()
    .Build();

// Main options panel
var panelOptions = _menuManager.CreatePanel("Options")
    .AddButton("Graphics", panelOptionsGraphic)
    .AddButton("Audio", panelOptionsAudio)
    .AddBackButton()
    .Build();
```

### Localization Support

The system supports Unity Localization package:

```csharp
var localizedString = _menuManager.GetLocalizedString("MainMenu_Play");
var panel = _menuManager.CreatePanel(localizedString)
    .AddButton(localizedString, () => { /* action */ })
    .Build();
```

## API Reference

### MainMenuManager

#### Panel Creation
- `CreatePanel(string name)` - Creates a new panel and returns `PanelBuilder`
- `CreatePanel(LocalizedString localizedName)` - Creates panel with localized name

#### Navigation
- `ChangePanel(GUIPanel panel)` - Changes active panel with animation
- `ChangePanel(string panelName)` - Changes panel by name
- `GoBack()` - Returns to previous panel in history
- `ExitGame()` - Exits the game (quits in build, stops play mode in editor)

#### Element Creation (Internal)
- `CreateButton(string text, Action onClick)` - Creates a button element
- `CreateSlider(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null)` - Creates a slider
- `CreateToggle(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)` - Creates a toggle
- `CreateDropdown(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)` - Creates a dropdown

#### Localization
- `GetLocalizedString(string key)` - Creates a LocalizedString from a key
- `GetBackButtonText()` - Returns back button text (localized or default)
- `GetExitButtonText()` - Returns exit button text (localized or default)

### PanelBuilder (Fluent API)

#### Buttons
- `AddButton(string text, Action onClick, bool isBottomButton = false)` - Adds button with action
- `AddButton(string text, GUIPanel targetPanel, bool isBottomButton = false)` - Adds button that changes panel
- `AddButton(LocalizedString localizedText, Action onClick, bool isBottomButton = false)` - Adds localized button
- `AddBackButton()` - Adds back button (uses manager's back button text)

#### Controls
- `AddSlider(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null)` - Adds slider
- `AddToggle(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)` - Adds toggle
- `AddDropDown(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)` - Adds dropdown

#### Building
- `Build()` - Completes panel construction and returns `GUIPanel`

### GUIPanel

#### Properties
- `PanelName` - Gets panel name
- `IsVisible` - Gets visibility state
- `IsAnimating` - Gets animation state

#### Methods
- `SetPanelName(string name)` - Sets panel name
- `SetPanelName(LocalizedString localizedName)` - Sets localized panel name
- `SetAnimationDuration(float duration)` - Sets animation duration
- `SetAnimationSettings(PanelAnimationType type, SlideDirection direction, float slideDistance, float bounceAmount)` - Configures animations
- `ShowPanel()` - Shows panel with animation
- `HidePanel()` - Hides panel with animation
- `AddElement(InterfaceElement element, bool isBottomElement = false)` - Adds element to panel

## Features

- ✅ **Automatic Element Positioning** - Elements are automatically positioned vertically
- ✅ **Configurable Panel Animations** - Fade and/or slide animations with bounce effects
- ✅ **Panel History Stack** - Navigate back through panel history
- ✅ **Fluent API** - Easy, readable panel construction
- ✅ **Current Value Support** - Sliders, toggles, and dropdowns can display current values
- ✅ **Prefab-Based** - All elements use prefabs with manual reference assignment
- ✅ **Localization Support** - Unity Localization package integration
- ✅ **Scroll Support** - Automatic scroll view when content exceeds panel height
- ✅ **Modular Animation System** - Easy to extend with new animation types

## Architecture

### Animation System

The animation system is modular and extensible:

- **`PanelAnimationHandler`** - Handles animation execution
- **`AnimateFade()`** - Fade animation implementation
- **`AnimateSlide()`** - Slide animation with bounce implementation
- **`StartCoroutinesParallel()`** - Runs multiple animations simultaneously

New animation types can be added by:
1. Adding a flag to `PanelAnimationType`
2. Creating an `Animate[Type]()` method in `PanelAnimationHandler`
3. Adding the animation to the active animations list in `Animate()`

### Element Hierarchy

```
InterfaceElement (base class)
├── GUIPanel
├── GUIButton
├── GUISlider
├── GUIToggle
└── GUIDropdown
```

All elements inherit animation capabilities from `InterfaceElement`.

## Notes

- Element sizes are set in prefabs
- Positions are automatically generated by code
- Panels are vertical containers with automatic spacing
- Each panel has a title displayed in `TextMeshProUGUI`
- Bottom elements (like back buttons) are positioned at the bottom of the panel
- Scroll view is automatically enabled when content exceeds available space
- Animation settings are global and applied to all panels created by the manager

## Performance Considerations

- Animations use coroutines for smooth frame-rate independent transitions
- Multiple animations can run in parallel using `StartCoroutinesParallel()`
- Element collections are cached to avoid repeated `GetComponentsInChildren()` calls
- Scroll view is only created when needed (when content exceeds panel height)
