# Phase 3 Test Guide: Procedural Wave Generator (Infinite Mode)

## Overview

This document provides step-by-step instructions to set up and test the Infinite Mode wave generation system, which procedurally creates waves based on a budget system with difficulty scaling.

---

## Prerequisites

- Unity project is open
- Phase 1 (Spawn Zone System) is complete and tested
- Phase 2 (Wave Provider Abstraction) is complete and tested
- The following scripts exist:
  - `Assets/Scripts/Data/EnemyConfig_SO.cs`
  - `Assets/Scripts/Data/InfiniteModeConfig_SO.cs`
  - `Assets/Scripts/Spawning/InfiniteWaveGenerator.cs`
  - `Assets/Scripts/Editor/InfiniteWaveGeneratorEditor.cs`

---

## Part 1: Understanding the Budget System

### How Wave Generation Works

```
Wave N requested
      ↓
Calculate Budget = StartingBudget × (Multiplier ^ (N-1)) + FlatIncrease × (N-1)
      ↓
Get Available Enemies (filtered by minWaveToAppear)
      ↓
While budget > 0:
   → Select random enemy (weighted)
   → If affordable, add to wave
   → Subtract enemy cost from budget
      ↓
Return RuntimeWaveData
```

### Example Progression

| Wave | Budget | Enemies (Est.) | Special |
|------|--------|----------------|---------|
| 1 | 30 | 3-4 | - |
| 5 | 52 | 5-6 | SWARM |
| 10 | 85 | 8-10 | BOSS |
| 20 | 165 | 15-18 | BOSS |
| 50 | 400+ | 25-30 | BOSS |

---

## Part 2: Creating Enemy Configuration Assets

### Step 2.1: Create Chaser Enemy Config

1. In the **Project** window, navigate to `Assets/Data/Enemies/`
2. Right-click → **Create** → **Project Mayhem** → **Enemy Config**
3. Name it `ChaserConfig`
4. Select it and configure in Inspector:

| Setting | Value | Reason |
|---------|-------|--------|
| **Pool Tag** | `Chaser` | Must match prefab name in ObjectPool |
| **Display Name** | `Chaser Enemy` | For UI/debug |
| **Difficulty Cost** | `8` | Cheap, basic enemy |
| **Min Wave To Appear** | `1` | Available from start |
| **Base Weight** | `2.0` | More likely to spawn |
| **Is Boss** | ❌ | Not a boss |
| **Max Per Wave** | `0` | Unlimited |
| **Spawn Interval Multiplier** | `1.0` | Normal spawn speed |

### Step 2.2: Create Shooter Enemy Config

1. Right-click in `Assets/Data/Enemies/` → **Create** → **Project Mayhem** → **Enemy Config**
2. Name it `ShooterConfig`
3. Configure:

| Setting | Value | Reason |
|---------|-------|--------|
| **Pool Tag** | `Shooter` | Must match prefab name |
| **Display Name** | `Shooter Enemy` | For UI/debug |
| **Difficulty Cost** | `15` | More expensive, harder enemy |
| **Min Wave To Appear** | `3` | Unlocks on wave 3 |
| **Base Weight** | `1.0` | Normal spawn chance |
| **Is Boss** | ❌ | Not a boss |
| **Max Per Wave** | `10` | Limit to 10 per wave |
| **Spawn Interval Multiplier** | `1.2` | Slightly slower spawn |

### Step 2.3: (Optional) Create Boss Enemy Config

If you have a boss enemy type:

| Setting | Value |
|---------|-------|
| **Pool Tag** | `Boss` |
| **Difficulty Cost** | `50` |
| **Min Wave To Appear** | `10` |
| **Base Weight** | `1.0` |
| **Is Boss** | ✅ |
| **Max Per Wave** | `1` |
| **Is Unique** | ✅ |

### Step 2.4: Verify Pool Tags

**Important:** Pool tags must match your ObjectPoolManager setup!

1. Find your ObjectPoolManager in the scene
2. Check the pool names/tags
3. Ensure EnemyConfig pool tags match exactly

```
ObjectPoolManager
├── Pool: "Chaser" ← Must match ChaserConfig.PoolTag
├── Pool: "Shooter" ← Must match ShooterConfig.PoolTag
└── Pool: "Boss" ← Must match BossConfig.PoolTag (if exists)
```

---

## Part 3: Creating Infinite Mode Configuration

### Step 3.1: Create the Config Asset

1. In **Project** window, navigate to `Assets/Data/`
2. Right-click → **Create** → **Project Mayhem** → **Infinite Mode Config**
3. Name it `InfiniteModeConfig`

### Step 3.2: Configure Difficulty Scaling

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Starting Budget** | `30` | Budget for wave 1 |
| **Budget Multiplier Per Wave** | `1.15` | 15% increase each wave |
| **Flat Budget Increase Per Wave** | `5` | +5 budget each wave |
| **Max Budget** | `500` | Cap to prevent impossibility |

### Step 3.3: Configure Enemy Pool

1. Expand **Available Enemies** list
2. Click **+** to add slots
3. Drag your EnemyConfig assets:
   - Slot 0: `ChaserConfig`
   - Slot 1: `ShooterConfig`
   - (Add more as needed)

### Step 3.4: Configure Enemy Limits

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Min Enemies Per Wave** | `3` | At least 3 enemies |
| **Max Enemies Per Wave** | `30` | Prevent overwhelming |

### Step 3.5: Configure Spawn Timing

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Base Spawn Interval** | `1.5` | 1.5s between spawns on wave 1 |
| **Min Spawn Interval** | `0.3` | Fastest possible (wave 50+) |
| **Spawn Interval Reduction Per Wave** | `0.97` | 3% faster each wave |

### Step 3.6: Configure Wave Timing

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Base Time Between Waves** | `5.0` | 5s rest after wave 1 |
| **Min Time Between Waves** | `2.0` | Minimum rest time |
| **Time Between Waves Reduction** | `0.1` | -0.1s per wave |

### Step 3.7: Configure Power-Ups

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Base Power Up Chance On Kill** | `0.05` | 5% on wave 1 |
| **Power Up Chance Increase Per Wave** | `0.01` | +1% per wave |
| **Max Power Up Chance On Kill** | `0.25` | Cap at 25% |
| **Spawn Power Up On Wave Complete** | ✅ | Guaranteed on wave end |

### Step 3.8: Configure Special Waves

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| **Boss Wave Interval** | `10` | Boss every 10 waves |
| **Boss Wave Budget Multiplier** | `1.5` | 50% more budget |
| **Swarm Wave Interval** | `5` | Swarm every 5 waves |
| **Swarm Wave Count Multiplier** | `2.0` | 2x enemies |

---

## Part 4: Setting Up InfiniteWaveGenerator

### Step 4.1: Add the Component

1. In **Hierarchy**, find or create a GameObject (can be same as WaveManager)
2. Add Component → **InfiniteWaveGenerator** (under ProjectMayhem.Spawning)

### Step 4.2: Assign Configuration

1. Drag `InfiniteModeConfig` asset into the **Config** field
2. Optionally enable **Debug Logging** to see generation details

### Step 4.3: Verify in Inspector

You should see:
- Configuration assigned
- Preview Tools section with:
  - Difficulty Curve foldout
  - Wave Preview foldout

---

## Part 5: Testing the Generator (Editor Only)

### Step 5.1: Preview Difficulty Curve

1. Select the InfiniteWaveGenerator
2. Expand **Preview Tools** → **Difficulty Curve**
3. You'll see a table showing:
   - Budget per wave (with visual bars)
   - Spawn intervals
   - Special wave markers (BOSS, SWARM)

### Step 5.2: Preview Specific Wave

1. Expand **Preview Tools** → **Wave Preview**
2. Use the slider to select a wave number
3. View the stats:
   - Budget
   - Spawn Interval
   - Time to Next Wave
   - Power-Up Chance
   - Available Enemy Types

### Step 5.3: Test Config Buttons

On the InfiniteModeConfig asset:
1. Click **Log Wave 1 Stats** - should log basic info
2. Click **Log Wave 10 Stats** - should show boss wave
3. Click **Log Wave 50 Stats** - should show high difficulty

---

## Part 6: Testing in Play Mode

### Step 6.1: Switch to Infinite Mode

**Option A: Replace Provider in Inspector**
1. On WaveManager, change "Wave Provider Component" to InfiniteWaveGenerator

**Option B: Switch at Runtime (via script)**
```csharp
// Add this to a test script or call from debug menu
InfiniteWaveGenerator infiniteGen = FindObjectOfType<InfiniteWaveGenerator>();
WaveManager.Instance.SetWaveProvider(infiniteGen);
```

### Step 6.2: Enter Play Mode

1. Press **Play**
2. Watch Console for generation logs (if debug enabled):

```
InfiniteWaveGenerator: Generated Wave 1 - Budget: 30, Enemies: 4, Groups: 2
WaveManager: Starting Wave 1: 4 enemies...
```

### Step 6.3: Verify Enemy Spawning

Check that:
- [ ] Enemies spawn correctly
- [ ] Enemy types match what's configured
- [ ] Wave 1-2 have only Chasers (Shooters unlock wave 3)
- [ ] Wave 3+ can have both Chasers and Shooters

### Step 6.4: Test Wave Progression

Kill all enemies and verify:
- [ ] Wave completes correctly
- [ ] Next wave starts after delay
- [ ] Wave 2 has more enemies than wave 1
- [ ] Spawn interval gets slightly faster

### Step 6.5: Test Special Waves

#### Testing Swarm Wave (Wave 5):
1. Use debug tool to skip to wave 5
2. Or play through waves 1-5
3. Verify:
   - [ ] More enemies than normal
   - [ ] Console shows "[SWARM]" marker
   - [ ] No boss enemies in swarm wave

#### Testing Boss Wave (Wave 10):
1. Skip to wave 10 (or play through)
2. Verify:
   - [ ] Higher budget (1.5x)
   - [ ] Console shows "[BOSS]" marker
   - [ ] Boss enemy spawns (if configured)

### Step 6.6: Test Long Sessions

Let the game run for 20+ waves and verify:
- [ ] No crashes or errors
- [ ] Difficulty increases smoothly
- [ ] Budget caps at max (doesn't go infinite)
- [ ] Spawn interval caps at minimum
- [ ] Game remains playable (not impossible)

---

## Part 7: Testing Seed System

### Step 7.1: Test Reproducible Runs

1. Note the seed shown in Inspector (Runtime Info section)
2. Play through a few waves, note enemy composition
3. Reset and use same seed
4. Verify waves generate identically

### Step 7.2: Test Custom Seed

```csharp
// Set a specific seed for testing
InfiniteWaveGenerator gen = FindObjectOfType<InfiniteWaveGenerator>();
gen.SetSeed(12345);
gen.Reset();
```

---

## Part 8: Testing Checklist

### Configuration Setup
- [ ] ChaserConfig created with correct pool tag
- [ ] ShooterConfig created with correct pool tag
- [ ] InfiniteModeConfig created and configured
- [ ] All enemy configs added to Available Enemies list
- [ ] InfiniteWaveGenerator has config assigned

### Editor Preview
- [ ] Difficulty Curve shows reasonable progression
- [ ] Wave Preview shows correct stats
- [ ] Boss waves marked every 10 waves
- [ ] Swarm waves marked every 5 waves (not on boss)

### Basic Functionality
- [ ] Wave 1 generates with correct budget
- [ ] Enemies spawn from correct pools
- [ ] Wave completes when all enemies killed
- [ ] Next wave starts after delay

### Difficulty Scaling
- [ ] Budget increases each wave
- [ ] Spawn interval decreases each wave
- [ ] New enemy types unlock at correct waves
- [ ] Budget caps at max value

### Special Waves
- [ ] Swarm waves have more enemies
- [ ] Boss waves have higher budget
- [ ] Boss enemies spawn on boss waves
- [ ] Special wave events fire correctly

### Edge Cases
- [ ] Empty enemy pool handled gracefully
- [ ] Very high wave numbers work (100+)
- [ ] Seed produces reproducible results
- [ ] Generator reset works correctly

---

## Part 9: Troubleshooting

### Problem: "No config assigned!"

**Solution:**
1. Select InfiniteWaveGenerator in Inspector
2. Drag InfiniteModeConfig asset into Config field

### Problem: No enemies spawn

**Solution:**
1. Check that Available Enemies list has entries
2. Verify pool tags match ObjectPoolManager
3. Check Console for "No enemies available" warning
4. Ensure minWaveToAppear allows enemies in current wave

### Problem: Wrong enemy types spawn

**Solution:**
1. Verify pool tags match prefab names exactly (case-sensitive)
2. Check ObjectPoolManager has matching pools
3. Ensure enemy configs are in Available Enemies list

### Problem: Too many/few enemies

**Solution:**
1. Adjust Starting Budget (higher = more enemies)
2. Adjust Difficulty Cost on enemy configs (lower = more spawns)
3. Check Min/Max Enemies Per Wave limits

### Problem: Game too hard/easy

**Solution:**
Adjust in InfiniteModeConfig:
- **Too Hard:** Reduce BudgetMultiplierPerWave, increase SpawnInterval
- **Too Easy:** Increase BudgetMultiplierPerWave, reduce SpawnInterval

### Problem: Special waves not triggering

**Solution:**
1. Check Boss Wave Interval is > 0
2. Check Swarm Wave Interval is > 0
3. Boss waves override swarm waves on same number
4. Verify wave numbers match intervals (10, 20, 30 for boss)

---

## Part 10: Quick Reference

### Difficulty Tuning Cheat Sheet

| Want More... | Adjust Setting |
|--------------|----------------|
| Total enemies | ↑ StartingBudget, ↓ DifficultyCost |
| Harder enemies | ↓ Weight on easy enemies |
| Faster scaling | ↑ BudgetMultiplierPerWave |
| Slower scaling | ↓ BudgetMultiplierPerWave |
| More power-ups | ↑ BasePowerUpChanceOnKill |
| Faster spawns | ↓ BaseSpawnInterval |

### Enemy Cost Guidelines

| Enemy Type | Suggested Cost |
|------------|----------------|
| Basic (Chaser) | 5-10 |
| Medium (Shooter) | 12-20 |
| Hard (Tank) | 25-40 |
| Boss | 40-60 |
| Mini-Boss | 30-50 |

### Inspector Quick Setup Summary

```
InfiniteWaveGenerator (Component)
├── Config: [InfiniteModeConfig]
└── Debug Logging: ❌ (enable for testing)

InfiniteModeConfig (Asset)
├── Starting Budget: 30
├── Budget Multiplier: 1.15
├── Available Enemies: [Chaser, Shooter, ...]
├── Base Spawn Interval: 1.5
├── Boss Wave Interval: 10
└── Swarm Wave Interval: 5
```

---

## Part 11: Switching Between Campaign and Infinite

### Creating a Mode Selector

You can create a simple script to switch modes:

```csharp
public class GameModeManager : MonoBehaviour
{
    [SerializeField] private CampaignWaveProvider campaignProvider;
    [SerializeField] private InfiniteWaveGenerator infiniteGenerator;

    public void StartCampaignMode()
    {
        WaveManager.Instance.SetWaveProvider(campaignProvider);
    }

    public void StartInfiniteMode()
    {
        WaveManager.Instance.SetWaveProvider(infiniteGenerator);
    }
}
```

### From Main Menu

```csharp
// Campaign button
public void OnCampaignButtonClicked()
{
    GameModeManager.Instance.StartCampaignMode();
    SceneManager.LoadScene("GameScene");
}

// Infinite button
public void OnInfiniteButtonClicked()
{
    GameModeManager.Instance.StartInfiniteMode();
    SceneManager.LoadScene("GameScene");
}
```

---

## Next Steps

Once all tests pass:
1. ✅ Phase 3 is complete
2. Tune difficulty values based on playtesting
3. Proceed to **Phase 4: Power-Up Spawning Integration**
4. The power-up chance values will be used once PowerUpSpawner is implemented
