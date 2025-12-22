# Phase 1 Test Guide: Spawn Zone System

## Overview

This document provides step-by-step instructions to set up and test the Spawn Zone system before integrating it with the Wave Manager.

---

## Prerequisites

- Unity project is open
- The following scripts exist:
  - `Assets/Scripts/Spawning/SpawnZone.cs`
  - `Assets/Scripts/Spawning/SpawnZoneManager.cs`
  - `Assets/Scripts/Editor/SpawnZoneEditor.cs`

---

## Part 1: Setting Up the SpawnZoneManager

### Step 1.1: Create the Manager GameObject

1. In the **Hierarchy** window, right-click and select **Create Empty**
2. Name it `SpawnZoneManager`
3. With the GameObject selected, go to **Inspector** and click **Add Component**
4. Search for `SpawnZoneManager` (under ProjectMayhem.Spawning namespace) and add it

### Step 1.2: Configure the Manager

In the Inspector, you'll see these settings:

| Setting | Recommended Value | Description |
|---------|------------------|-------------|
| Auto Discover Zones | ✅ Checked | Automatically finds all SpawnZone components |
| Manual Zones | Leave empty | Only needed if auto-discover is off |
| Fallback Radius | 15 | Backup spawn radius if no zones exist |
| Max Spawn Attempts | 20 | How many tries to find a valid point |

> **Note:** Leave "Auto Discover Zones" checked for now. The manager will find zones automatically.

---

## Part 2: Creating Spawn Zones

### Step 2.1: Create Your First Enemy Spawn Zone

1. In the **Hierarchy**, right-click and select **Create Empty**
2. Name it `SpawnZone_Enemy_Left`
3. Add the **SpawnZone** component (search under ProjectMayhem.Spawning)
4. Position it to the **left side** of your play area (e.g., X: -12, Y: 0, Z: 0)

### Step 2.2: Configure the Zone

Set these values in the Inspector:

| Setting | Value | Why |
|---------|-------|-----|
| **Shape** | Rectangle | Good for screen edges |
| **Zone Type** | Enemy | Only enemies spawn here |
| **Size** | X: 3, Y: 10 | Tall strip on the side |
| **Min Distance From Player** | 3 | Enemies won't spawn too close |
| **Max Distance From Player** | 0 | 0 = no max limit |
| **Must Be Off Screen** | ✅ Checked | Enemies spawn outside camera view |
| **Weight** | 1 | Equal chance with other zones |
| **Gizmo Color** | Orange/Red | Visual identification |

### Step 2.3: Verify the Zone is Visible

1. Make sure **Gizmos** are enabled in the Scene view (top toolbar)
2. You should see an **orange/red rectangle** where you placed the zone
3. A small **colored sphere** above indicates the zone type:
   - 🔴 Red = Enemy
   - 🟢 Green = Power-Up
   - 🟡 Yellow = Both

---

## Part 3: Creating Multiple Zones

### Step 3.1: Create Additional Enemy Zones

Repeat the process to create zones around the play area:

| Zone Name | Position | Size | Notes |
|-----------|----------|------|-------|
| `SpawnZone_Enemy_Left` | X: -12, Y: 0 | 3 x 10 | Left edge |
| `SpawnZone_Enemy_Right` | X: 12, Y: 0 | 3 x 10 | Right edge |
| `SpawnZone_Enemy_Top` | X: 0, Y: 8 | 20 x 3 | Top edge |
| `SpawnZone_Enemy_Bottom` | X: 0, Y: -8 | 20 x 3 | Bottom edge |

> **Quick Tip:** After creating the first zone, you can:
> 1. Select it
> 2. Press **Ctrl+D** to duplicate
> 3. Rename and reposition the duplicate

### Step 3.2: Create a Power-Up Zone

1. Create a new empty GameObject named `SpawnZone_PowerUp_Center`
2. Add the **SpawnZone** component
3. Configure:
   - **Shape:** Circle
   - **Zone Type:** PowerUp
   - **Radius:** 8
   - **Position:** X: 0, Y: 0 (center of play area)
   - **Min Distance From Player:** 2
   - **Must Be Off Screen:** ❌ Unchecked (power-ups can spawn on screen)
   - **Gizmo Color:** Green

---

## Part 4: Using the Preview Tools

### Step 4.1: Preview Spawn Points

1. Select any SpawnZone in the Hierarchy
2. In the Inspector, find **Preview Tools**
3. Check **Show Preview Points**
4. Click **Regenerate Points**

You'll see colored dots in the Scene view:
- **Green dots** = Valid spawn points
- **Red dots with X** = Invalid (too close to player or on screen)

### Step 4.2: Test with Player in Scene

1. Enter **Play Mode**
2. Move the player around
3. Select a spawn zone
4. Watch how the preview points change (some become invalid near the player)

### Step 4.3: See Distance Indicators

When a SpawnZone is selected and the Player exists in the scene:
- A **red circle** shows the minimum distance from player
- A **green circle** shows the maximum distance (if set)

---

## Part 5: Testing the SpawnZoneManager

### Step 5.1: Test in Play Mode

1. Select the **SpawnZoneManager** in the Hierarchy
2. Enter **Play Mode**
3. In the Inspector, click **Test Enemy Spawn Point**
4. A **cyan sphere** will appear at the generated spawn point for 2 seconds
5. Click multiple times to see different spawn locations

### Step 5.2: Verify Zone Statistics

While in Play Mode, the SpawnZoneManager Inspector shows:
- **Total Zones:** Should match how many you created
- **Enemy Zones:** Zones with Enemy or Both type
- **Power-Up Zones:** Zones with PowerUp or Both type

---

## Part 6: Testing Checklist

Run through this checklist to verify everything works:

### Zone Visualization
- [ ] Spawn zones appear as colored shapes in Scene view
- [ ] Rectangle zones show as filled rectangles with wire outline
- [ ] Circle zones show as circle outlines
- [ ] Zone type indicators (colored spheres) appear above zones
- [ ] Selected zones have brighter/more visible gizmos

### Zone Configuration
- [ ] Changing Shape switches between Rectangle/Circle correctly
- [ ] Changing Size/Radius updates the visual immediately
- [ ] Zone Type dropdown works (Enemy, PowerUp, Both)
- [ ] Gizmo Color changes the zone's display color

### Preview Points
- [ ] "Show Preview Points" displays dots in the zone
- [ ] "Regenerate Points" creates new random points
- [ ] Points near the player show as red (invalid)
- [ ] Points off-screen show as green when "Must Be Off Screen" is checked

### SpawnZoneManager
- [ ] Manager shows correct zone counts in Inspector
- [ ] "Test Enemy Spawn Point" creates visible markers
- [ ] Spawn points appear within zone boundaries
- [ ] Spawn points respect minimum player distance
- [ ] "Create New Spawn Zone" button works
- [ ] "Select All Spawn Zones" selects all zones

### Edge Cases
- [ ] With no zones: Manager uses fallback radius (yellow circle at origin)
- [ ] With all invalid points: Manager still returns a point (with warning in Console)

---

## Part 7: Recommended Zone Layout

Here's a suggested layout for a standard arena:

```
            ┌────────────────────────────┐
            │   SpawnZone_Enemy_Top      │
            │   (Rectangle: 20x3)        │
            └────────────────────────────┘
                         
┌───┐                                   ┌───┐
│   │                                   │   │
│ L │     ┌─────────────────────┐       │ R │
│ E │     │                     │       │ I │
│ F │     │  SpawnZone_PowerUp  │       │ G │
│ T │     │  (Circle, radius 8) │       │ H │
│   │     │                     │       │ T │
│   │     └─────────────────────┘       │   │
│   │                                   │   │
└───┘                                   └───┘
            ┌────────────────────────────┐
            │  SpawnZone_Enemy_Bottom    │
            │  (Rectangle: 20x3)         │
            └────────────────────────────┘
```

---

## Part 8: Troubleshooting

### Problem: Zones don't appear in Scene view
**Solution:** 
1. Make sure Gizmos are enabled (click "Gizmos" button in Scene view toolbar)
2. Check that the SpawnZone component was added correctly
3. Verify the zone has non-zero Size/Radius

### Problem: All preview points are red (invalid)
**Solution:**
1. Check if "Must Be Off Screen" is enabled—the zone might be fully on screen
2. Reduce "Min Distance From Player" if it's larger than the zone
3. Make sure the Player has the "Player" tag

### Problem: SpawnZoneManager shows 0 zones
**Solution:**
1. Verify "Auto Discover Zones" is checked
2. Make sure SpawnZone components are added to GameObjects
3. Enter Play Mode (zones are discovered on Awake)

### Problem: "ProjectMayhem.Spawning" namespace not found
**Solution:**
1. Wait for Unity to compile scripts (check bottom-right progress bar)
2. If errors persist, check Console for compilation errors

---

## Next Steps

Once all tests pass:
1. ✅ Phase 1 is complete
2. Proceed to **Phase 2: Wave Provider Abstraction**
3. The WaveManager will be updated to use `SpawnZoneManager.Instance.GetEnemySpawnPoint()`

---

## Quick Reference

### Key Methods (for future integration)

```csharp
// Get a single enemy spawn point
Vector2 point = SpawnZoneManager.Instance.GetEnemySpawnPoint();

// Get a single power-up spawn point  
Vector2 point = SpawnZoneManager.Instance.GetPowerUpSpawnPoint();

// Get multiple spawn points with spacing
Vector2[] points = SpawnZoneManager.Instance.GetSpawnPoints(
    count: 5, 
    minSpacing: 2f, 
    forEnemies: true
);

// Get spawn point near a location
Vector2 point = SpawnZoneManager.Instance.GetSpawnPointNear(
    center: enemyDeathPosition, 
    maxDistance: 5f, 
    forEnemies: false  // for power-up
);
```
