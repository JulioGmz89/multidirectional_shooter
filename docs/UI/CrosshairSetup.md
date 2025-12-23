# Crosshair Tracking System - Project Mayhem

## Overview
The Crosshair Tracking System provides real-time mouse cursor tracking with a custom UI crosshair that follows the mouse position and is clamped to screen boundaries. The system automatically manages system cursor visibility based on game state.

## Components

### CrosshairController
**Location**: `Assets/Scripts/UI/CrosshairController.cs`
**Purpose**: Main crosshair control script with mouse tracking, screen clamping, and cursor management

**Key Features**:
- Real-time mouse position tracking using Input System
- Canvas-based UI positioning for pixel-perfect accuracy
- Screen boundary clamping with configurable edge offset
- Automatic system cursor hiding during gameplay
- GameStateManager integration for proper state management
- Runtime control methods for dynamic behavior

## Setup Instructions

### Step 1: Create Crosshair UI
1. **Create Canvas** (if not already present):
   - Right-click in Hierarchy → UI → Canvas
   - Set Render Mode to "Screen Space - Overlay" (recommended)
   - Ensure Canvas Scaler is set to "Scale With Screen Size" for responsive design

2. **Create Crosshair Image**:
   - Right-click on Canvas → UI → Image
   - Name it "Crosshair"
   - Set your crosshair PNG as the Source Image
   - Set Image Type to "Simple"
   - Adjust size as needed (recommended: 32x32 or 64x64 pixels)

3. **Position Setup**:
   - Set Anchor to center-center (0.5, 0.5)
   - Set Pivot to center (0.5, 0.5)
   - Reset Position to (0, 0, 0)

### Step 2: Attach CrosshairController
1. Select the Crosshair Image GameObject
2. Add Component → Crosshair Controller
3. Configure settings in Inspector:
   - **Enable Tracking**: ✅ (checked)
   - **Screen Edge Offset**: `10` (prevents crosshair from going completely off-screen)
   - **Hide System Cursor**: ✅ (checked)
   - **Show Debug Info**: ☐ (uncheck for production)

### Step 3: Verify Integration
- Ensure GameStateManager exists in scene
- Test in Play mode - crosshair should follow mouse
- System cursor should be hidden during gameplay
- Crosshair should be clamped to screen edges

## Configuration Options

### Basic Settings
```
Enable Tracking: true          // Enable/disable crosshair tracking
Screen Edge Offset: 10         // Distance from screen edge (pixels)
Hide System Cursor: true       // Hide system cursor during gameplay
Show Debug Info: false         // Console logging for debugging
```

### Recommended Settings by Use Case

#### Standard Arena Shooter
```
Screen Edge Offset: 10
Hide System Cursor: true
Enable Tracking: true
```

#### Precision Aiming
```
Screen Edge Offset: 5
Hide System Cursor: true
Enable Tracking: true
```

#### Gamepad Support
```
Screen Edge Offset: 15
Hide System Cursor: false
Enable Tracking: false (use SetCrosshairPosition() manually)
```

## Technical Details

### Canvas Coordinate System
- Uses `RectTransformUtility.ScreenPointToLocalPointInRectangle()` for accurate conversion
- Supports all Canvas render modes (Screen Space Overlay, Camera, World Space)
- Automatically detects UI camera for proper coordinate conversion

### Screen Boundary Clamping
- Accounts for crosshair size to prevent partial off-screen rendering
- Uses configurable edge offset for fine-tuning
- Calculates bounds based on canvas rect and crosshair dimensions

### Performance Optimizations
- Cached component references (RectTransform, Canvas, Camera)
- Cached screen bounds and crosshair size calculations
- Only updates during Gameplay state
- Efficient coordinate conversion with minimal allocations

## Public API Methods

### Runtime Control
```csharp
// Enable/disable tracking
crosshairController.SetTrackingEnabled(bool enabled);

// Get current positions
Vector2 canvasPos = crosshairController.GetCrosshairCanvasPosition();
Vector2 screenPos = crosshairController.GetCrosshairScreenPosition();

// Manual positioning (for gamepad support)
crosshairController.SetCrosshairPosition(Vector2 screenPosition);

// Utility methods
crosshairController.RefreshBounds();        // Refresh after resolution change
bool isClamped = crosshairController.IsClamped();  // Check if clamped to edges
```

### Example Usage
```csharp
// Disable crosshair during cutscenes
crosshairController.SetTrackingEnabled(false);

// Position crosshair at screen center
Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
crosshairController.SetCrosshairPosition(screenCenter);

// Check if player is aiming at screen edge
if (crosshairController.IsClamped())
{
    // Player is trying to aim off-screen
    Debug.Log("Crosshair clamped to screen edge");
}
```

## Integration with Existing Systems

### GameStateManager
- **Automatic Integration**: Subscribes to state change events
- **Cursor Management**: Shows system cursor during Pause/Defeat/Menu states
- **Tracking Control**: Only tracks during Gameplay state

### Input System
- **Mouse Input**: Uses `Mouse.current.position.ReadValue()` for tracking
- **Null Safety**: Handles cases where no mouse is connected
- **Performance**: Direct input reading without event subscriptions

### Smart Camera System
- **Compatible**: Works alongside SmartCameraController without conflicts
- **Coordinate Systems**: UI uses screen space, camera uses world space
- **No Interference**: Both systems can use mouse input simultaneously

## Troubleshooting

### Crosshair Not Following Mouse
- Verify CrosshairController is attached to crosshair UI element
- Check that Enable Tracking is enabled
- Ensure Canvas is properly configured
- Verify GameStateManager is in Gameplay state

### System Cursor Still Visible
- Check Hide System Cursor setting is enabled
- Verify GameStateManager integration is working
- Look for other scripts that might be controlling cursor visibility

### Crosshair Going Off-Screen
- Increase Screen Edge Offset value
- Check crosshair RectTransform size settings
- Verify anchor and pivot are set to center (0.5, 0.5)

### Performance Issues
- Disable Show Debug Info in production
- Ensure Canvas is using Screen Space - Overlay for best performance
- Check for multiple CrosshairController instances

### Resolution Changes
- Call `RefreshBounds()` after resolution changes
- Use Canvas Scaler with "Scale With Screen Size" for responsive design
- Test on different screen resolutions and aspect ratios

## Best Practices

### UI Setup
- Use Screen Space - Overlay canvas for best performance
- Set crosshair anchor and pivot to center for proper positioning
- Use appropriate crosshair size (32x32 or 64x64 recommended)

### Performance
- Keep Screen Edge Offset reasonable (5-15 pixels)
- Disable debug logging in production builds
- Use single CrosshairController instance per scene

### User Experience
- Provide option to toggle crosshair visibility in settings
- Consider crosshair color/style customization
- Test crosshair visibility on different backgrounds

### Gamepad Support
- Disable mouse tracking when gamepad is active
- Use `SetCrosshairPosition()` for manual crosshair control
- Implement smooth crosshair movement for gamepad input

## Debug Features
- Enable "Show Debug Info" for console logging
- Gizmos visualization shows screen bounds in Scene view
- Runtime methods for position and state checking
- Automatic bounds validation and error reporting
