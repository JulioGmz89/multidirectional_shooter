# Trail Renderer Implementation Guide for Project Mayhem

## Overview
This document provides setup instructions and architectural details for the Trail Renderer system implemented for "Project Mayhem" - a 2D Arena Shooter in Unity.

## Architecture

### Core Components

1. **TrailRendererController** - Main component that manages individual trail effects
2. **TrailRendererConfig** - ScriptableObject for configuring different trail types
3. **TrailManager** - Singleton manager for centralized trail setup and management
4. **Integration with existing systems** - PlayerController and Projectile classes

### Key Features

- **Configurable trail settings** per object type (PlayerShip, PlayerProjectile, EnemyProjectile)
- **Velocity-based trail activation** - trails only show when objects are moving
- **Object pooling integration** - proper trail cleanup and initialization
- **Performance optimized** - automatic trail disabling during non-gameplay states
- **Visual differentiation** - different colors and properties for different object types

## Setup Instructions

### 1. Create Trail Materials

Create materials for different trail types in your project:

1. **Player Ship Trail Material**
   - Create new Material in Project window
   - Set Shader to "Sprites/Default" or "UI/Default"
   - Configure blue/cyan gradient colors
   - Set Rendering Mode to "Transparent"

2. **Player Projectile Trail Material**
   - Similar setup with white/blue colors
   - Brighter appearance for visibility

3. **Enemy Projectile Trail Material**
   - Red/orange color scheme
   - Slightly different properties for enemy distinction

### 2. Configure Trail Settings

1. Create a TrailRendererConfig ScriptableObject:
   ```
   Right-click in Project → Create → Project Mayhem → Trail Renderer Config
   ```

2. Configure settings for each object type:
   - **PlayerShip**: Longer trail (0.5s), wider (0.3f start width)
   - **PlayerProjectile**: Medium trail (0.3s), narrow (0.15f start width)
   - **EnemyProjectile**: Short trail (0.25s), narrow (0.12f start width)

### 3. Setup Trail Manager

1. Create an empty GameObject in your scene named "TrailManager"
2. Add the TrailManager component
3. Assign the TrailRendererConfig ScriptableObject
4. Assign the trail materials you created

### 4. Player Ship Setup

The PlayerController automatically sets up trails when `enableTrail` is true (default).
No additional setup required - trails will be created automatically.

### 5. Projectile Setup

Projectiles automatically detect their type (Player vs Enemy) and configure appropriate trails.
The system integrates with the existing object pooling system.

## Configuration Options

### TrailRendererController.TrailSettings

- **startWidth/endWidth**: Trail width at start and end points
- **time**: How long the trail persists (in seconds)
- **colorGradient**: Color transition from start to end
- **trailMaterial**: Material used for rendering
- **enabledByDefault**: Whether trail starts enabled
- **minVelocityThreshold**: Minimum speed required to show trail
- **sortingLayerName**: Sorting layer for 2D rendering order (e.g., "Background", "Default", "Foreground")
- **orderInLayer**: Order within the sorting layer (lower values render behind higher values)
- **Note**: TrailRenderer always uses world space by default in Unity

### Performance Settings

- **useVelocityThreshold**: Automatically disable trails when not moving
- **velocityCheckInterval**: How often to check velocity (default: 0.1s)

### Debug Settings

- **enableDebugLogging**: Enable console logging for trail events (velocity changes, enable/disable)
- **showDebugInfo**: Show visual debug information in Scene view (velocity vectors, trail status)

## Best Practices

### Performance
- Keep trail times short (0.2-0.5 seconds) for better performance
- Use velocity thresholds to avoid unnecessary trail rendering
- Trails automatically disable during pause/defeat states

### Visual Design
- Use contrasting colors for player vs enemy projectiles
- Player ship trails should be more prominent than projectile trails
- Consider using additive blending for glowing effects

### Integration
- The system automatically handles object pooling cleanup
- Trails are properly initialized when objects spawn from pools
- Game state changes automatically manage trail visibility

## Layer Control & Rendering Order

### Controlling Trail Depth in 2D

To ensure trails render behind objects (which is usually desired):

1. **Set Sorting Layer**: Use `sortingLayerName` to control which layer trails render on
   - Common layers: "Background", "Default", "Foreground"
   - Create custom layers in Project Settings → Tags and Layers → Sorting Layers

2. **Set Order in Layer**: Use `orderInLayer` to fine-tune rendering order
   - **Negative values** render behind (recommended for trails)
   - **Positive values** render in front
   - Default configuration: `-1` for player ship, `-2` for projectiles

### Example Layer Setup
```
Background Layer (orderInLayer: 0) - Background sprites
Default Layer (orderInLayer: -2) - Projectile trails
Default Layer (orderInLayer: -1) - Player ship trail  
Default Layer (orderInLayer: 0) - Player ship, projectiles
Default Layer (orderInLayer: 1) - UI elements
```

## Debugging Trail Issues

### Debug Features

1. **Console Logging** (`enableDebugLogging = true`)
   - Trail enable/disable events
   - Velocity threshold changes
   - Trail configuration application

2. **Scene View Visualization** (`showDebugInfo = true`)
   - **Yellow line**: Current velocity vector
   - **Green/Red label**: Trail status (ON/OFF) with details
   - **Yellow circle**: Velocity threshold visualization

### Debug Methods

```csharp
// Get trail status information
string status = trailController.GetTrailDebugInfo();
Debug.Log(status);

// Manual trail control for testing
trailController.SetTrailEnabled(true);
trailController.ClearTrail();
```

### Common Debug Scenarios

1. **Trail not visible**: Check sorting layer and order in layer
2. **Trail appears in front**: Set negative `orderInLayer` value
3. **Trail not activating**: Check velocity threshold and current speed
4. **Trail persists when stopped**: Verify velocity-based activation is enabled

## Troubleshooting

### Common Issues

1. **Trails not appearing**
   - Check if TrailManager exists in scene
   - Verify TrailRendererConfig is assigned
   - Ensure materials are properly configured
   - **Check sorting layer and order in layer settings**

2. **Trails rendering in wrong order**
   - **Set negative `orderInLayer` values** for trails to render behind objects
   - Verify sorting layer names match your project setup
   - Use Scene view debug visualization to check trail status

3. **Performance issues**
   - Reduce trail time duration
   - Increase velocity threshold
   - Check if too many trails are active simultaneously

4. **Trails not clearing properly**
   - Verify TrailManager cleanup methods are being called
   - Check object pooling integration

### Debug Options

The TrailManager includes editor tools for runtime setup:
- Enable "Setup Trails On Validate" for testing
- Use editor validation to setup trails for existing objects
- **Enable debug logging and scene view visualization** for detailed troubleshooting

## Technical Details

### Object Pooling Integration
- Trails are cleared when objects return to pool
- Trails are re-initialized when objects spawn from pool
- No memory leaks or trail artifacts

### Game State Management
- Trails automatically disable during non-gameplay states
- Re-enable when returning to gameplay
- Proper cleanup on scene transitions

### Extensibility
- Easy to add new object types by extending TrailRendererConfig
- Configurable per-object-type settings
- Runtime modification of trail properties supported

## Future Enhancements

Potential improvements for the trail system:
- Dynamic trail intensity based on speed
- Trail color changes based on power-ups
- Particle effects integration
- Trail length scaling with object size
