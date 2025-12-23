# Off-Screen Indicator System Proposal

## Overview

This document outlines the implementation plan for an **Off-Screen Indicator System** that will help players locate enemies and power-ups that are outside their camera view. Indicators will appear at the edge of the screen, pointing toward off-screen targets with visual differentiation based on target type.

---

## Problem Statement

Currently, players struggle to locate enemies and buffs spread across the map. This creates:
- Frustration when searching for remaining enemies to complete waves
- Missed power-up opportunities
- Reduced tactical awareness and gameplay flow

---

## Proposed Solution

Create a dynamic indicator system that:
1. Displays arrow/pointer indicators at the screen edge
2. Points toward off-screen enemies and power-ups
3. Uses distinct colors and shapes per target type
4. Automatically hides when targets enter the visible screen area
5. Shows distance information (optional)

---

## System Architecture

### Core Components

```
OffScreenIndicatorSystem/
├── Scripts/
│   ├── Core/
│   │   ├── OffScreenIndicatorManager.cs    # Main singleton manager
│   │   ├── OffScreenIndicator.cs           # Individual indicator behavior
│   │   └── ITrackable.cs                   # Interface for trackable objects
│   │
│   ├── Config/
│   │   └── IndicatorConfig.cs              # ScriptableObject for settings
│   │
│   └── Components/
│       └── TrackableTarget.cs              # Component to mark objects as trackable
│
├── Prefabs/
│   ├── IndicatorCanvas.prefab              # UI Canvas for indicators
│   ├── EnemyIndicator.prefab               # Enemy arrow prefab
│   ├── PowerUpIndicator.prefab             # Power-up arrow prefab
│   └── ShooterEnemyIndicator.prefab        # Specific enemy type indicator
│
└── Sprites/
    ├── arrow_indicator.png                 # Base arrow sprite
    ├── diamond_indicator.png               # Diamond shape for power-ups
    └── skull_indicator.png                 # Skull shape for dangerous enemies
```

---

## Detailed Component Design

### 1. ITrackable Interface

```csharp
public interface ITrackable
{
    Transform TrackableTransform { get; }
    IndicatorType IndicatorType { get; }
    bool IsTrackingEnabled { get; }
    int TrackingPriority { get; }  // Higher priority = always shown
}
```

### 2. IndicatorType Enum

```csharp
public enum IndicatorType
{
    // Enemies
    ChaserEnemy,        // Red arrow
    ShooterEnemy,       // Orange arrow with warning
    
    // Power-ups
    RapidFire,          // Yellow diamond
    Shield,             // Blue diamond
    
    // Future expandability
    Boss,               // Large red skull
    Objective           // Green marker
}
```

### 3. IndicatorConfig (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "IndicatorConfig", menuName = "ProjectMayhem/Indicator Config")]
public class IndicatorConfig : ScriptableObject
{
    [System.Serializable]
    public class IndicatorSettings
    {
        public IndicatorType type;
        public Sprite sprite;
        public Color color;
        public float scale;
        public bool showDistance;
        public bool pulseAnimation;
        public float edgePadding;       // Distance from screen edge
    }

    public List<IndicatorSettings> indicators;
    public float screenEdgeBuffer;       // Margin from actual screen edge
    public float fadeDistance;           // Start fading when target approaches screen
    public int maxIndicators;            // Performance limit
    public float updateInterval;         // How often to update positions (optimization)
}
```

### 4. OffScreenIndicatorManager

**Responsibilities:**
- Maintain registry of all trackable objects
- Pool and manage indicator UI elements
- Calculate screen positions for indicators
- Handle indicator visibility and transitions

**Key Features:**
- Object pooling for indicators (reuse existing `ObjectPoolManager`)
- Frame-rate independent updates
- Camera frustum culling integration
- Priority system when too many targets exist

### 5. OffScreenIndicator

**Responsibilities:**
- Display individual indicator sprite
- Rotate to point toward target
- Animate (pulse, scale based on distance)
- Fade in/out smoothly

### 6. TrackableTarget Component

**Responsibilities:**
- Auto-register with `OffScreenIndicatorManager` on enable
- Auto-unregister on disable/destroy
- Provide tracking data (type, priority, enabled state)

---

## Integration with Existing Systems

### Enemy Integration

Modify `ChaserEnemy.cs` and `ShooterEnemy.cs`:

```csharp
// Add to ChaserEnemy.cs
public class ChaserEnemy : MonoBehaviour, IPooledObject, ITrackable
{
    // ITrackable implementation
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => IndicatorType.ChaserEnemy;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 1;
    
    // ... existing code
}
```

### Power-Up Integration

Modify `PowerUp.cs`:

```csharp
public class PowerUp : MonoBehaviour, ITrackable
{
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => powerUpType == PowerUpType.RapidFire 
        ? IndicatorType.RapidFire 
        : IndicatorType.Shield;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 2;  // Higher priority than enemies
    
    // ... existing code
}
```

### Camera Integration

The system will use `SmartCameraController`'s camera reference to:
- Calculate viewport bounds
- Determine what's on/off screen
- Position indicators at screen edges

---

## Screen Position Calculation Algorithm

```csharp
public Vector2 CalculateIndicatorPosition(Vector3 targetWorldPos, Camera cam)
{
    // 1. Get direction from camera center to target
    Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
    Vector3 targetScreenPos = cam.WorldToScreenPoint(targetWorldPos);
    
    // 2. If on screen, return null (hide indicator)
    if (IsOnScreen(targetScreenPos))
        return Vector2.zero; // Signal to hide
    
    // 3. Calculate intersection with screen edge
    Vector2 direction = ((Vector2)targetScreenPos - (Vector2)screenCenter).normalized;
    
    // 4. Find edge intersection point
    Vector2 edgePoint = FindScreenEdgeIntersection(screenCenter, direction, edgePadding);
    
    // 5. Calculate rotation angle
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    
    return edgePoint;
}

private Vector2 FindScreenEdgeIntersection(Vector2 center, Vector2 direction, float padding)
{
    float screenWidth = Screen.width - padding * 2;
    float screenHeight = Screen.height - padding * 2;
    
    // Ray-box intersection to find where direction hits screen edge
    float tX = direction.x != 0 ? (screenWidth / 2) / Mathf.Abs(direction.x) : float.MaxValue;
    float tY = direction.y != 0 ? (screenHeight / 2) / Mathf.Abs(direction.y) : float.MaxValue;
    float t = Mathf.Min(tX, tY);
    
    return center + direction * t;
}
```

---

## Visual Design Specifications

### Color Scheme

| Target Type | Color | Hex Code | Shape |
|-------------|-------|----------|-------|
| Chaser Enemy | Red | `#FF4444` | Arrow |
| Shooter Enemy | Orange | `#FF8800` | Arrow with dot |
| Rapid Fire | Yellow | `#FFDD00` | Diamond |
| Shield | Cyan | `#00DDFF` | Diamond |
| Boss | Dark Red | `#AA0000` | Skull |

### Indicator Sizes

- **Base size**: 32x32 pixels (UI scale independent)
- **Near target** (within 2x screen): 1.0x scale
- **Far target** (beyond 4x screen): 0.7x scale
- **Lerp between** based on distance

### Animations

1. **Pulse Animation**: Subtle scale pulse (0.95 - 1.05) every 0.5s
2. **Entry Animation**: Scale from 0 to 1 over 0.2s when appearing
3. **Exit Animation**: Fade out over 0.15s when target enters screen
4. **Distance Pulse**: Faster pulse when target is very close to screen edge

---

## Implementation Phases

### Phase 1: Core System (Priority: High)
1. Create `ITrackable` interface
2. Implement `OffScreenIndicatorManager` singleton
3. Create basic `OffScreenIndicator` prefab
4. Implement screen edge position calculation

**Estimated Time**: 2-3 hours

### Phase 2: Enemy Integration (Priority: High)
1. Add `ITrackable` to `ChaserEnemy`
2. Add `ITrackable` to `ShooterEnemy`
3. Create enemy-specific indicator sprites
4. Test with spawned enemies

**Estimated Time**: 1-2 hours

### Phase 3: Power-Up Integration (Priority: High)
1. Add `ITrackable` to `PowerUp`
2. Create power-up indicator sprites (diamond shape)
3. Differentiate colors by power-up type

**Estimated Time**: 1 hour

### Phase 4: Visual Polish (Priority: Medium)
1. Implement smooth animations
2. Add distance-based scaling
3. Create `IndicatorConfig` ScriptableObject
4. Fine-tune colors and sizes

**Estimated Time**: 2 hours

### Phase 5: Optimization (Priority: Medium)
1. Implement indicator pooling
2. Add update interval throttling
3. Implement max indicator limit with priority sorting
4. Profile and optimize

**Estimated Time**: 1-2 hours

### Phase 6: Optional Enhancements (Priority: Low)
1. Distance text display
2. Threat level indication (multiple enemies in same direction)
3. Minimap integration
4. Settings menu toggle for indicators

**Estimated Time**: 2-3 hours

---

## File Structure After Implementation

```
Assets/
├── Scripts/
│   └── UI/
│       ├── Indicators/
│       │   ├── ITrackable.cs
│       │   ├── IndicatorType.cs
│       │   ├── OffScreenIndicatorManager.cs
│       │   ├── OffScreenIndicator.cs
│       │   └── TrackableTarget.cs
│       └── ... (existing UI scripts)
│
├── Data/
│   └── UI/
│       └── IndicatorConfig.asset
│
├── Prefabs/
│   └── UI/
│       ├── IndicatorCanvas.prefab
│       └── Indicators/
│           ├── EnemyIndicator.prefab
│           ├── ShooterIndicator.prefab
│           └── PowerUpIndicator.prefab
│
└── Sprites/
    └── UI/
        └── Indicators/
            ├── arrow_indicator.png
            ├── diamond_indicator.png
            └── warning_indicator.png
```

---

## Performance Considerations

1. **Object Pooling**: Reuse indicator GameObjects instead of instantiate/destroy
2. **Update Throttling**: Update indicator positions every 2-3 frames instead of every frame
3. **Hybrid Update**: Use coroutines for non-critical updates, Update() for position
4. **Max Indicators**: Limit to 15-20 simultaneous indicators
5. **Priority Culling**: When over limit, hide lowest priority indicators
6. **Camera Frustum Caching**: Cache viewport bounds, recalculate only on camera move

---

## Testing Checklist

- [ ] Indicators appear for off-screen enemies
- [ ] Indicators appear for off-screen power-ups
- [ ] Indicators disappear when target enters screen
- [ ] Indicators correctly point toward targets
- [ ] Different colors for different target types
- [ ] Different shapes for enemies vs power-ups
- [ ] Smooth fade in/out transitions
- [ ] No performance drops with many indicators
- [ ] Works correctly with camera shake
- [ ] Works correctly with camera zoom (if applicable)
- [ ] Indicators update correctly when targets move
- [ ] Indicators removed when targets are destroyed
- [ ] System works after scene reload

---

## Dependencies

- **Unity UI System** (Canvas, Image, RectTransform)
- **Existing Systems**:
  - `SmartCameraController` (camera reference)
  - `ObjectPoolManager` (for indicator pooling)
  - `GameStateManager` (pause handling)

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Too many indicators cluttering screen | High | Implement max limit with priority system |
| Performance impact with many enemies | Medium | Object pooling + update throttling |
| Indicators obscuring gameplay | Medium | Make semi-transparent, add edge padding |
| Confusion with multiple indicators | Low | Group nearby targets, show count |

---

## Future Enhancements

1. **Minimap**: Add optional minimap showing all targets
2. **Threat Direction**: Show warning when enemies approach from blind spots
3. **Custom Indicators**: Allow players to customize colors in settings
4. **Audio Cues**: Optional sound when new threats appear off-screen
5. **Accessibility**: High contrast mode, larger indicators option

---

## Approval

Once this proposal is approved, implementation can begin with Phase 1. The system is designed to be modular, allowing for incremental development and testing.

**Recommended Starting Point**: Create the core manager and interface, then integrate with `ChaserEnemy` as a proof of concept before expanding to other target types.
