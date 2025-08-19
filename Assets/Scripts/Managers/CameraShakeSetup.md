# Camera Shake System Setup Guide

## Overview
The Camera Shake System provides responsive visual feedback for key game events in Project Mayhem. It consists of two main components that work together to create smooth, configurable camera shake effects.

## Components

### 1. CameraShake.cs
- **Purpose**: Handles the actual camera shake mechanics
- **Features**: 
  - Configurable intensity and duration
  - Smooth interpolation and decay
  - Multiple shake types with predefined settings
  - Debug testing capabilities in editor

### 2. CameraShakeManager.cs
- **Purpose**: Centralized singleton manager for triggering shakes
- **Features**:
  - Singleton pattern for global access
  - Configurable intensity/duration per event type
  - Auto-detection of CameraShake component
  - Type-safe shake triggering methods

## Setup Instructions

### Step 1: Camera Setup
1. Select your Main Camera in the scene
2. Add the `CameraShake` component to the Main Camera
3. Configure the shake settings in the inspector:
   - **Max Shake Intensity**: 1.0 (recommended)
   - **Shake Decay**: 5.0 (recommended)
   - **Shake Smoothness**: 0.9 (recommended)

### Step 2: Manager Setup
1. Create an empty GameObject in your scene (name it "CameraShakeManager")
2. Add the `CameraShakeManager` component to this GameObject
3. Configure the shake intensities and durations for different events:
   - **Enemy Death**: Intensity 0.15, Duration 0.2s
   - **Player Death**: Intensity 0.8, Duration 1.5s
   - **Player Damage**: Intensity 0.25, Duration 0.3s
   - **Explosion**: Intensity 0.5, Duration 0.8s

### Step 3: Verification
The system is already integrated into the following game events:
- ✅ Player taking damage (Health.cs)
- ✅ Player death (PlayerController.cs)
- ✅ Enemy death (ChaserEnemy.cs, ShooterEnemy.cs)

## Usage Examples

### Triggering Custom Shakes
```csharp
// Custom shake with specific intensity and duration
CameraShakeManager.Instance.TriggerShake(0.5f, 1.0f);

// Predefined shake types
CameraShakeManager.Instance.TriggerExplosionShake();
CameraShakeManager.Instance.TriggerPlayerDeathShake();
CameraShakeManager.Instance.TriggerEnemyDeathShake();
CameraShakeManager.Instance.TriggerPlayerDamageShake();

// Stop any current shake
CameraShakeManager.Instance.StopShake();
```

### Adding Shakes to New Events
```csharp
// In any script where you want to trigger a shake:
if (CameraShakeManager.Instance != null)
{
    CameraShakeManager.Instance.TriggerExplosionShake();
}
```

## Testing
1. Use the debug controls in the CameraShake component inspector
2. Set `Test Shake` to true to trigger a test shake
3. Adjust `Test Intensity` and `Test Duration` to experiment with values
4. Play the game and verify shakes trigger on:
   - Player taking damage
   - Player death
   - Enemy deaths

## Architecture Benefits

### Separation of Concerns
- `CameraShake`: Pure shake mechanics and math
- `CameraShakeManager`: Event coordination and configuration

### Performance Optimized
- Single coroutine per shake effect
- Smooth interpolation reduces jitter
- Automatic cleanup and position reset

### Designer Friendly
- All shake parameters exposed in inspector
- Predefined shake types for common events
- Easy to add new shake types

### Robust Error Handling
- Null checks for manager instance
- Auto-detection of camera components
- Graceful degradation if components missing

## Customization

### Adding New Shake Types
1. Add new intensity/duration fields to `CameraShakeManager`
2. Create a new public method following the pattern:
```csharp
public void TriggerNewEventShake()
{
    if (cameraShake != null)
    {
        cameraShake.Shake(newEventIntensity, newEventDuration);
    }
}
```

### Modifying Shake Behavior
- Adjust decay curves in `CameraShake.ShakeCoroutine()`
- Modify shake patterns by changing the random offset calculation
- Add rotation shake by modifying the shake offset to include Z-axis rotation

## Best Practices
1. Keep shake intensities between 0.1-1.0 for best results
2. Shorter durations (0.1-0.5s) for quick feedback events
3. Longer durations (1.0-2.0s) for dramatic events like player death
4. Always null-check `CameraShakeManager.Instance` before calling
5. Use predefined shake methods rather than custom values when possible
