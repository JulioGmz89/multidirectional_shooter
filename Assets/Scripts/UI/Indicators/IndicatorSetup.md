# Off-Screen Indicator System - Setup Guide

## Phase 1 Implementation Complete ✅

The core off-screen indicator system has been implemented. This guide explains how to set up the system in your scene.

---

## Files Created

| File | Description |
|------|-------------|
| [ITrackable.cs](ITrackable.cs) | Interface for trackable objects |
| [IndicatorType.cs](IndicatorType.cs) | Enum defining indicator types |
| [IndicatorConfig.cs](IndicatorConfig.cs) | ScriptableObject for configuration |
| [OffScreenIndicator.cs](OffScreenIndicator.cs) | Individual indicator behavior |
| [OffScreenIndicatorManager.cs](OffScreenIndicatorManager.cs) | Main singleton manager |

---

## Setup Instructions

### Step 1: Create the Indicator Configuration Asset

1. In the Project window, right-click in `Assets/Data/UI/` folder (create if needed)
2. Select **Create > ProjectMayhem > UI > Indicator Config**
3. Name it `IndicatorConfig`
4. Configure the settings (see Configuration section below)

### Step 2: Create the Indicator Prefab

1. Create a new UI Canvas in your scene (if you don't have one dedicated for indicators)
   - Set **Render Mode** to `Screen Space - Overlay`
   - Add a **Canvas Scaler** with `Scale With Screen Size`

2. Create the indicator prefab:
   - Right-click in Hierarchy > **UI > Image**
   - Name it `OffScreenIndicator`
   - Add components:
     - `CanvasGroup` (for fading)
     - `OffScreenIndicator` script
   - Set the Image to use an arrow sprite (or use Unity's default)
   - Set **Raycast Target** to `false`
   - Size: 32x32 or as desired
   - Anchor: Center

3. Save as prefab in `Assets/Prefabs/UI/Indicators/`

### Step 3: Set Up the Manager

1. Create an empty GameObject in your scene named `OffScreenIndicatorManager`
2. Add the `OffScreenIndicatorManager` component
3. Assign:
   - **Config**: Your IndicatorConfig asset
   - **Target Camera**: Your main game camera (or leave empty to use Camera.main)
   - **Indicator Container**: A RectTransform on your UI Canvas
   - **Indicator Prefab**: Your indicator prefab

### Step 4: Create Indicator Container

1. On your UI Canvas, create an empty GameObject named `IndicatorContainer`
2. Set it to stretch to fill the entire canvas
3. Assign this to the manager's **Indicator Container** field

---

## Configuration Reference

### IndicatorConfig Settings

```
Indicator Settings (per type):
├── Type          - Which target type this applies to
├── Sprite        - The indicator sprite to display
├── Color         - Tint color for the indicator
├── Scale         - Base size multiplier (1 = normal)
├── Show Distance - Display distance text (Phase 6)
├── Pulse Animation - Enable attention-grabbing pulse
└── Edge Padding  - Pixels from screen edge

Global Settings:
├── Screen Edge Buffer    - Extra margin from screen edge (0-0.15)
├── Fade Start Distance   - When to start fading (world units)
├── Max Indicators        - Performance limit (5-30)
└── Update Interval       - Position update frequency (0.016-0.1s)

Animation Settings:
├── Pulse Duration        - Pulse cycle time in seconds
├── Pulse Scale Range     - Min/Max scale during pulse
└── Fade Duration         - Fade in/out time

Distance Scaling:
├── Min Distance Scale    - Scale at max distance
└── Far Distance Threshold - Distance for min scale
```

### Recommended Default Values

| Setting | Recommended Value |
|---------|------------------|
| Screen Edge Buffer | 0.05 |
| Max Indicators | 20 |
| Update Interval | 0.033 (30fps) |
| Pulse Duration | 0.5 |
| Pulse Scale Range | (0.95, 1.05) |
| Fade Duration | 0.15 |
| Min Distance Scale | 0.7 |
| Far Distance Threshold | 4 |

### Color Recommendations

| Type | Color | Hex |
|------|-------|-----|
| Chaser Enemy | Red | #FF4444 |
| Shooter Enemy | Orange | #FF8800 |
| Rapid Fire | Yellow | #FFDD00 |
| Shield | Cyan | #00DDFF |

---

## Next Steps (Phase 2 & 3)

After setting up the core system, integrate with existing game objects:

### Enemy Integration
Add `ITrackable` interface to enemy scripts and register/unregister with the manager.

### Power-Up Integration  
Add `ITrackable` interface to power-up scripts.

See the main proposal document for detailed integration code.

---

## Troubleshooting

### Indicators not appearing
- Check that targets implement `ITrackable` and call `RegisterTarget()`
- Verify the manager has a valid camera reference
- Ensure `IsTrackingEnabled` returns `true`
- Check the indicator prefab is assigned

### Indicators in wrong position
- Verify the camera reference is correct
- Check that the indicator container is properly sized
- Ensure the target's `TrackableTransform` returns the correct transform

### Performance issues
- Increase `Update Interval` in config
- Reduce `Max Indicators`
- Ensure targets unregister when destroyed

---

## API Reference

### Registering Targets

```csharp
// Register a target
OffScreenIndicatorManager.Instance.RegisterTarget(this);

// Unregister a target
OffScreenIndicatorManager.Instance.UnregisterTarget(this);
```

### Implementing ITrackable

```csharp
public class MyTarget : MonoBehaviour, ITrackable
{
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => IndicatorType.ChaserEnemy;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 1;
    
    private void OnEnable()
    {
        OffScreenIndicatorManager.Instance?.RegisterTarget(this);
    }
    
    private void OnDisable()
    {
        OffScreenIndicatorManager.Instance?.UnregisterTarget(this);
    }
}
```
