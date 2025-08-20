# Smart Camera System - "Enter the Gungeon" Style

## Overview
The Smart Camera System provides dynamic 2D camera behavior similar to "Enter the Gungeon", featuring:
- **Dead Zone**: Camera remains stationary when player moves within a central area
- **Mouse Look-Ahead**: Camera pans ahead based on mouse cursor position
- **Smooth Movement**: Configurable interpolation for responsive yet smooth camera motion
- **Boundary Support**: Optional world boundaries to constrain camera movement

## Components

### 1. SmartCameraConfig (ScriptableObject)
**Location**: `Assets/Scripts/Data/SmartCameraConfig.cs`
**Purpose**: Configuration asset for all camera behavior settings

**Key Settings**:
- `deadZoneSize`: Size of area around player where camera doesn't move
- `mouseInfluence`: How much mouse position affects camera (0-1)
- `maxLookAheadDistance`: Maximum distance camera can look ahead
- `followSpeed`, `lookAheadSpeed`, `returnSpeed`: Different interpolation speeds
- `worldBoundaries`: Optional constraints for camera movement

### 2. SmartCameraController
**Location**: `Assets/Scripts/Camera/SmartCameraController.cs`
**Purpose**: Main camera logic component

**Features**:
- Automatic player detection
- Real-time dead zone calculations
- Mouse position tracking with timeout
- Smooth interpolation using SmoothDamp
- Debug visualization with Gizmos
- Integration with GameStateManager

## Setup Instructions

### Step 1: Create Camera Configuration
1. Right-click in Project window
2. Create → Project Mayhem → Smart Camera Config
3. Name it "SmartCameraConfig"
4. Configure settings in Inspector:
   - **Dead Zone Size**: `(3, 2)` recommended for arena shooter
   - **Mouse Influence**: `0.3` for subtle look-ahead
   - **Max Look Ahead Distance**: `8` units
   - **Follow Speed**: `5` for responsive movement
   - **Look Ahead Speed**: `3` for smooth mouse tracking
   - **Return Speed**: `2` for gentle return to player

### Step 2: Setup Main Camera
1. Select Main Camera in scene
2. Add Component → Smart Camera Controller
3. Assign the SmartCameraConfig asset to Config field
4. Enable "Auto Find Player" or manually assign Player Transform
5. Enable "Show Debug Info" for testing

### Step 3: Verify Integration
- Ensure Player GameObject has "Player" tag OR PlayerController component
- Camera will automatically find and follow the player
- Mouse movement will influence camera position based on configuration

## Configuration Recommendations

### Arena Shooter Settings (Recommended)
```
Dead Zone Size: (3, 2)
Mouse Influence: 0.3
Max Look Ahead Distance: 8
Min Mouse Distance: 1
Follow Speed: 5
Look Ahead Speed: 3
Return Speed: 2
```

### Tight Control Settings
```
Dead Zone Size: (2, 1.5)
Mouse Influence: 0.5
Max Look Ahead Distance: 6
Follow Speed: 8
Look Ahead Speed: 6
Return Speed: 4
```

### Cinematic Settings
```
Dead Zone Size: (4, 3)
Mouse Influence: 0.2
Max Look Ahead Distance: 12
Follow Speed: 2
Look Ahead Speed: 1.5
Return Speed: 1
```

## Technical Details

### Dead Zone Logic
- Camera only moves when player reaches the edge of the dead zone
- Dead zone is centered on player position
- Camera smoothly follows to keep player within frame

### Mouse Look-Ahead Algorithm
1. Calculate mouse world position using camera projection
2. Determine direction and distance from player to mouse
3. Apply mouse influence factor to blend positions
4. Clamp look-ahead distance to maximum allowed
5. Smooth interpolation to target position

### Performance Considerations
- Cached calculations for frequently used values
- Efficient distance checks using sqrMagnitude where possible
- Mouse input timeout prevents unnecessary calculations
- LateUpdate ensures camera moves after player movement

## Integration with Existing Systems

### GameStateManager
- Camera only updates during Gameplay state
- Automatically pauses during Pause/Defeat states

### Input Integration
- Compatible with existing mouse input handling
- Includes null checks for mouse availability
- No conflicts with PlayerController's input handling

### PlayerController
- Works alongside existing mouse input for player aiming
- Compatible with existing input handling
- No interference with player rotation/firing mechanics

### CameraShake System Integration
- **Automatic Integration**: SmartCameraController detects and works with CameraShake component
- **Shake Isolation**: Smart camera pauses updates during active shake events to prevent conflicts
- **Seamless Transitions**: Camera resumes smart following after shake effects complete
- **Conflict Prevention**: Clean separation eliminates position resets and interference issues
- **Setup Requirement**: Both SmartCameraController and CameraShake must be on the same camera GameObject

## Troubleshooting

### Camera Not Following Player
- Verify Player has "Player" tag or PlayerController component
- Check that SmartCameraConfig is assigned
- Ensure Auto Find Player is enabled or Player Transform is manually assigned

### Camera Movement Too Fast/Slow
- Adjust Follow Speed, Look Ahead Speed, and Return Speed in config
- Lower values = slower, smoother movement
- Higher values = faster, more responsive movement

### Mouse Look-Ahead Not Working
- Check Mouse Influence is greater than 0
- Verify Min Mouse Distance allows for mouse influence
- Ensure mouse is moving (input timeout may be active)

### Camera Going Outside World Bounds
- Enable Use Boundaries in config
- Set appropriate World Boundaries rect
- Ensure boundaries are larger than camera view area

### Camera Shake Integration Issues
- **Missing Integration**: Look for warning message about missing CameraShake component
- **Shake Not Working**: Verify CameraShakeManager is properly configured and referencing the camera
- **Setup Issue**: Ensure both SmartCameraController and CameraShake are on the same camera GameObject

### Camera Stops Following Player
- **Player Reference Lost**: Check console for "Player reference lost" warnings
- **Player Death**: Camera will stop following when player dies (expected behavior)
- **Manual Refresh**: Call `RefreshPlayerReference()` on SmartCameraController if needed
- **Debug Check**: Use `HasValidPlayerReference()` to verify player reference status

### Performance Issues
- Reduce mouse input timeout if camera feels sluggish
- Lower smoothing speeds if movement is too slow
- Increase dead zone size to reduce unnecessary camera updates

## Debug Features
- Enable "Show Debug Info" on SmartCameraController
- Green wireframe shows dead zone
- Yellow line shows mouse look-ahead direction
- Red sphere shows camera target position
- Blue wireframe shows world boundaries (if enabled)
- Console warnings for missing components or configuration issues

## Integration Testing Checklist
1. **Basic Movement**: Player moves within dead zone - camera stays still ✅
2. **Dead Zone Exit**: Player moves to edge - camera follows smoothly ✅
3. **Mouse Look-Ahead**: Move mouse around - camera pans ahead of player ✅
4. **Camera Shake**: Kill enemies - camera shakes then resumes following ✅
5. **Enemy Collisions**: Take damage - camera shakes briefly then continues following ✅
6. **Game State**: Pause game - camera stops updating ✅
7. **Player Death**: Camera shakes intensely then stops following (expected) ✅
