# Phase 2 Test Guide: Wave Provider Abstraction

## Overview

This document provides step-by-step instructions to set up and test the Wave Provider system, which allows the WaveManager to work with different wave sources (Campaign mode with ScriptableObjects, or future Infinite mode).

---

## Prerequisites

- Unity project is open
- Phase 1 (Spawn Zone System) is complete and tested
- The following scripts exist:
  - `Assets/Scripts/Spawning/RuntimeWaveData.cs`
  - `Assets/Scripts/Spawning/IWaveProvider.cs`
  - `Assets/Scripts/Spawning/CampaignWaveProvider.cs`
  - `Assets/Scripts/Managers/WaveManager.cs` (refactored version)

---

## Part 1: Understanding the New Architecture

### Before (Old System)
```
WaveManager
├── List<Wave_SO> waves        ← Directly holds wave data
├── Transform[] spawnPoints    ← Fixed spawn points
└── SpawnWave(Wave_SO wave)    ← Spawns from ScriptableObject
```

### After (New System)
```
WaveManager
├── IWaveProvider waveProvider     ← Interface (can be Campaign or Infinite)
├── SpawnZoneManager (optional)    ← Dynamic spawn positions
└── SpawnWave(RuntimeWaveData)     ← Spawns from runtime data

CampaignWaveProvider : IWaveProvider
├── List<Wave_SO> waves            ← Your existing wave assets
└── GetNextWave() → RuntimeWaveData
```

---

## Part 2: Setting Up CampaignWaveProvider

### Step 2.1: Locate WaveManager

1. In the **Hierarchy**, find your existing `WaveManager` GameObject
2. Select it to view in the Inspector

### Step 2.2: Add CampaignWaveProvider Component

1. With WaveManager selected, click **Add Component**
2. Search for `CampaignWaveProvider` (under ProjectMayhem.Spawning namespace)
3. Add it to the **same GameObject** as WaveManager

### Step 2.3: Configure CampaignWaveProvider

In the CampaignWaveProvider component:

| Setting | Action |
|---------|--------|
| **Waves** | Drag your existing Wave_SO assets here (Wave_01, Wave_02, etc.) |
| **Power Up Chance On Kill** | Set to 0.05 (5% chance) |
| **Spawn Power Up On Wave Complete** | ✅ Check this |

**To add waves:**
1. Click the **+** button to add slots
2. Drag Wave_SO assets from `Assets/Data/Waves/` into each slot
3. Arrange them in order (Wave_01 first, Wave_07 last)

> **Tip:** You can drag multiple assets at once by selecting them all in the Project window.

### Step 2.4: Configure WaveManager

In the WaveManager component:

| Setting | Value | Description |
|---------|-------|-------------|
| **Wave Provider Component** | Drag the CampaignWaveProvider | Links to the provider |
| **Use Spawn Zones** | ✅ Checked | Uses SpawnZoneManager |
| **Legacy Spawn Points** | Leave empty or keep old points | Fallback only |

**Important:** Drag the **same GameObject** (or the CampaignWaveProvider component) into the "Wave Provider Component" field.

---

## Part 3: Verifying the Setup

### Step 3.1: Check Inspector Shows No Errors

After setup, you should see:
- ✅ WaveManager with "Wave Provider Component" assigned
- ✅ CampaignWaveProvider with waves listed
- ❌ No red error text in Inspector
- ❌ No errors in Console

### Step 3.2: Check Component Order

Your WaveManager GameObject should have:
```
WaveManager (GameObject)
├── WaveManager (Script)           ← Orchestrates spawning
├── CampaignWaveProvider (Script)  ← Provides wave data
└── (Optional) Other components
```

---

## Part 4: Testing in Play Mode

### Step 4.1: Enter Play Mode

1. Press **Play** in Unity
2. Open the **Console** window (Window → General → Console)
3. Look for these log messages:

```
WaveManager: Using wave provider 'CampaignWaveProvider'
WaveManager: Starting Wave 1: X enemies, Y groups...
```

### Step 4.2: Verify Wave Progression

Watch the Console as you play:

| Expected Log | Meaning |
|--------------|---------|
| `Starting Wave 1: X enemies...` | Wave data loaded correctly |
| `Wave 1 has X enemies in Y groups` | Enemy count calculated |
| `Enemy defeated. X remaining` | Enemy tracking works |
| `Wave 1 completed!` | Wave completion detected |
| `Starting Wave 2...` | Progression works |

### Step 4.3: Verify Spawn Positions

If SpawnZoneManager is set up:
- Enemies should spawn **within your defined spawn zones**
- Spawn positions should vary (not always the same spot)

If using legacy spawn points:
- Enemies spawn at the old fixed positions
- Console shows: `No spawn zones... using random position` (if no fallback)

### Step 4.4: Test Wave Completion

1. Kill all enemies in Wave 1
2. Verify:
   - Console shows "Wave 1 completed!"
   - Wave complete sound plays
   - After delay, Wave 2 starts

### Step 4.5: Test Victory Condition

Use the debug key (K by default) to kill all enemies quickly:
1. Play through all waves (or skip using WaveTester)
2. When the last wave is cleared:
   - Console shows "All waves completed!"
   - GameState changes to Victory

---

## Part 5: Testing SpawnZone Integration

### Step 5.1: Verify SpawnZoneManager Connection

1. Make sure `SpawnZoneManager` exists in scene
2. Make sure spawn zones exist (from Phase 1)
3. In WaveManager, ensure **Use Spawn Zones** is checked

### Step 5.2: Test Spawn Distribution

1. Enter Play Mode
2. Watch where enemies spawn:
   - Should appear in different zones
   - Should respect zone rules (off-screen, min distance from player)

### Step 5.3: Test Fallback Behavior

To test the fallback to legacy spawn points:
1. Temporarily **disable** the SpawnZoneManager GameObject
2. Enter Play Mode
3. Enemies should spawn at legacy spawn points (if assigned)
4. Console may show warning about missing SpawnZoneManager

---

## Part 6: Testing Checklist

### Setup Verification
- [ ] CampaignWaveProvider component added to WaveManager GameObject
- [ ] All Wave_SO assets assigned to CampaignWaveProvider
- [ ] WaveManager has "Wave Provider Component" assigned
- [ ] No errors in Console on startup

### Wave Progression
- [ ] Wave 1 starts when gameplay begins
- [ ] Correct number of enemies spawn
- [ ] Enemies spawn from correct pool tags
- [ ] Wave completes when all enemies defeated
- [ ] Next wave starts after delay
- [ ] Victory triggers when all waves complete

### Spawn Integration
- [ ] Enemies spawn in defined SpawnZones (if available)
- [ ] Spawn positions vary between spawns
- [ ] Fallback works if SpawnZoneManager is missing

### Audio Integration
- [ ] Wave start sound plays
- [ ] Wave complete sound plays
- [ ] Enemy sounds still work

### Edge Cases
- [ ] Game handles empty wave list (error logged)
- [ ] Game handles null Wave_SO (warning logged, skipped)
- [ ] Pausing/resuming doesn't break wave state

---

## Part 7: Troubleshooting

### Problem: "No wave provider assigned or found!"

**Solution:**
1. Check that CampaignWaveProvider is on the WaveManager GameObject
2. Drag the CampaignWaveProvider into WaveManager's "Wave Provider Component" field
3. Or ensure CampaignWaveProvider is on the same GameObject (auto-detected)

### Problem: "Wave provider returned null wave!"

**Solution:**
1. Check that waves are assigned in CampaignWaveProvider
2. Verify Wave_SO assets are not corrupted
3. Check Console for earlier warnings about null prefabs

### Problem: Enemies don't spawn

**Solution:**
1. Check that enemy prefabs have ObjectPool entries
2. Verify pool tags match prefab names
3. Check SpawnZoneManager has valid zones
4. Look for errors in Console about missing pools

### Problem: Enemies spawn at origin (0,0)

**Solution:**
1. SpawnZoneManager might be missing
2. Legacy spawn points might be empty
3. Add spawn zones or legacy spawn points

### Problem: Wave never completes

**Solution:**
1. Check that enemies call `WaveManager.Instance.OnEnemyDefeated()` when they die
2. Verify ChaserEnemy and ShooterEnemy have this call in their `Defeat()` method
3. Check Console for enemy count logs

### Problem: "Component does not implement IWaveProvider"

**Solution:**
1. Make sure you're assigning `CampaignWaveProvider`, not another component
2. Wait for Unity to finish compiling
3. Check for compile errors in other scripts

---

## Part 8: Runtime Wave Provider Switching (Advanced)

The new system supports switching wave providers at runtime:

```csharp
// Example: Switch from Campaign to Infinite mode
IWaveProvider infiniteProvider = GetComponent<InfiniteWaveGenerator>();
WaveManager.Instance.SetWaveProvider(infiniteProvider);
```

This will be used when Infinite Mode is implemented in Phase 3.

---

## Part 9: Comparing Old vs New Behavior

### Expected: Identical Gameplay

The refactored system should produce **identical gameplay** to the old system:

| Aspect | Should Be Same? |
|--------|-----------------|
| Enemy types per wave | ✅ Yes |
| Enemy counts | ✅ Yes |
| Spawn timing | ✅ Yes |
| Wave completion | ✅ Yes |
| Victory condition | ✅ Yes |
| Spawn positions | ⚠️ Different if using SpawnZones |

### New Features Available

| Feature | Description |
|---------|-------------|
| `OnWaveCompleted` event | Subscribe to know when a wave ends |
| `EnemiesAlive` property | Check remaining enemy count |
| `IsSpawning` property | Check if wave is still spawning |
| `ResetWaves()` | Restart from wave 1 |
| `SkipToWave(n)` | Debug: jump to specific wave |

---

## Part 10: Quick Reference

### Key Classes

```csharp
// Get current wave number (1-indexed)
int wave = WaveManager.Instance.CurrentWaveNumber;

// Check if finite mode (campaign) or infinite
bool isFinite = WaveManager.Instance.IsFiniteMode;

// Get enemies remaining
int remaining = WaveManager.Instance.EnemiesAlive;

// Reset to wave 1
WaveManager.Instance.ResetWaves();

// Subscribe to wave events
WaveManager.Instance.OnWaveChanged += (waveNum) => { };
WaveManager.Instance.OnWaveCompleted += (waveNum) => { };
```

### Inspector Setup Summary

```
WaveManager GameObject
│
├── WaveManager (Component)
│   ├── Wave Provider Component: [Drag CampaignWaveProvider here]
│   ├── Use Spawn Zones: ✅
│   └── Legacy Spawn Points: (optional fallback)
│
└── CampaignWaveProvider (Component)
    ├── Waves: [Wave_01, Wave_02, ..., Wave_07]
    ├── Power Up Chance On Kill: 0.05
    └── Spawn Power Up On Wave Complete: ✅
```

---

## Next Steps

Once all tests pass:
1. ✅ Phase 2 is complete
2. Proceed to **Phase 3: Procedural Wave Generator (Infinite Mode)**
3. The InfiniteWaveGenerator will implement the same IWaveProvider interface
