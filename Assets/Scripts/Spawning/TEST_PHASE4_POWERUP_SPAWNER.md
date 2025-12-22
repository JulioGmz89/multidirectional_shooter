# Phase 4 Test Plan: Power-Up Spawning Integration

## Overview
This test plan verifies the PowerUpSpawner system and its integration with WaveManager.

---

## Test Setup

### Prerequisites
1. Have SpawnZoneManager configured with at least one power-up zone (ZoneType = PowerUp or Both)
2. Add PowerUpSpawner component to GameManager or dedicated spawner object
3. Ensure ObjectPoolManager has pools for power-up prefabs (`PowerUp_RapidFire`, `PowerUp_Shield`)

### PowerUpSpawner Configuration
1. Create GameObject named "PowerUpSpawner" 
2. Add `PowerUpSpawner` component
3. Configure Power-Up Pool:
   - Add entry: Pool Tag = `PowerUp_RapidFire`, Weight = 1.0, Min Wave = 1, Enabled = ✓
   - Add entry: Pool Tag = `PowerUp_Shield`, Weight = 0.5, Min Wave = 3, Enabled = ✓
4. Set spawn settings:
   - Chance On Enemy Kill: 0.1 (10%)
   - Spawn On Wave Complete: ✓
   - Wave Complete Chance: 0.5 (50%)
   - Max Active Power-Ups: 3
   - Cooldown: 5 seconds

---

## Test Cases

### Test 1: Basic Spawn Functionality
**Steps:**
1. Enter Play Mode
2. Open PowerUpSpawner Inspector
3. In "Testing & Preview" section, click "Spawn Random"

**Expected:**
- Power-up spawns at valid location from SpawnZoneManager
- "Active Power-Ups" count increases to 1
- Console shows spawn debug message (if debug mode enabled)

**Pass:** ☐

---

### Test 2: Weight Distribution
**Steps:**
1. In Inspector, set preview wave to 1
2. Observe weight distribution bar

**Expected:**
- Only `PowerUp_RapidFire` shows (100% green bar)
- `PowerUp_Shield` excluded (min wave = 3)

3. Set preview wave to 3+
4. Observe weight distribution bar

**Expected:**
- Both power-ups show in bar
- RapidFire ≈ 67% (weight 1.0 of 1.5)
- Shield ≈ 33% (weight 0.5 of 1.5)

**Pass:** ☐

---

### Test 3: Enemy Kill Integration
**Steps:**
1. Enter Play Mode, start wave
2. Kill enemies and watch for power-up spawns
3. Check console for spawn attempts

**Expected:**
- ~10% of kills trigger spawn attempt
- Spawns respect cooldown (5s between spawns)
- Spawns respect max limit (3 active)

**Alternative Test:**
1. Use "Try Kill Spawn" button repeatedly
2. Observe spawn rate matches 10% chance

**Pass:** ☐

---

### Test 4: Wave Complete Integration
**Steps:**
1. Enter Play Mode
2. Complete a wave (kill all enemies)
3. Observe power-up spawn behavior

**Expected:**
- ~50% chance to spawn power-up on wave complete
- Wave complete uses configured chance (not guaranteed unless set)

**Pass:** ☐

---

### Test 5: Wave Number Sync
**Steps:**
1. Enter Play Mode
2. Check PowerUpSpawner's "Current Wave" in runtime stats
3. Progress through waves

**Expected:**
- Current Wave updates when WaveManager advances
- Power-ups unlock at correct waves (Shield at wave 3+)

**Test with Inspector:**
1. Use "Set Wave" button to manually set wave number
2. Verify weight distribution updates correctly

**Pass:** ☐

---

### Test 6: Cooldown System
**Steps:**
1. Enter Play Mode
2. Spawn a power-up using "Spawn Random"
3. Immediately try to spawn another
4. Check "Can Spawn" status

**Expected:**
- Second spawn fails
- "Can Spawn" shows "No (cooldown/limit)"
- After 5 seconds, spawning re-enabled

**Pass:** ☐

---

### Test 7: Max Active Limit
**Steps:**
1. Set Max Active Power-Ups to 2
2. Enter Play Mode
3. Spawn 2 power-ups using "Spawn Random"
4. Try to spawn a third

**Expected:**
- Third spawn fails
- "Active Power-Ups" shows 2
- "Can Spawn" shows No

5. Collect one power-up with player
6. Try spawning again

**Expected:**
- Active count decreases to 1
- New spawn succeeds (after cooldown)

**Pass:** ☐

---

### Test 8: Power-Up Tracking
**Steps:**
1. Enter Play Mode
2. Spawn power-ups
3. Collect them with player OR let them be destroyed
4. Check "Active Power-Ups" count

**Expected:**
- Count decreases when power-ups are collected
- Count decreases when power-ups are destroyed
- PowerUpTracker component auto-added to spawned power-ups

**Pass:** ☐

---

### Test 9: Spawn Zone Integration
**Steps:**
1. Set up multiple power-up zones with different weights
2. Enter Play Mode
3. Spawn multiple power-ups, observe positions

**Expected:**
- Power-ups spawn within designated zones
- Higher weight zones get more spawns
- Respects zone's player distance requirements

**Pass:** ☐

---

### Test 10: Fallback Positioning
**Steps:**
1. Remove all spawn zones from scene
2. Enter Play Mode
3. Try spawning power-up

**Expected:**
- Power-up spawns at fallback position (within camera view)
- No errors in console about missing zones
- Warning logged about using fallback

**Pass:** ☐

---

### Test 11: Disable/Enable Power-Ups
**Steps:**
1. Disable `PowerUp_Shield` in pool (uncheck enabled)
2. Set preview wave to 3+
3. Observe weight distribution

**Expected:**
- Only RapidFire shows (100%)
- Shield excluded despite being at valid wave

4. Enter Play Mode, spawn multiple times
5. All spawns should be RapidFire

**Pass:** ☐

---

### Test 12: Reset Functionality
**Steps:**
1. Enter Play Mode
2. Advance to wave 5
3. Spawn some power-ups
4. Click "Reset Spawner"

**Expected:**
- Current Wave resets to 1
- Active Power-Ups count resets to 0
- Cooldown timer resets

**Pass:** ☐

---

### Test 13: RuntimeWaveData Integration
**Steps:**
1. Configure CampaignWaveProvider with Wave_SO that has:
   - `powerUpChanceOnKill` = 0.5 (50%)
   - `spawnPowerUpOnComplete` = true
2. Enter Play Mode, play that wave
3. Kill enemies

**Expected:**
- 50% kill chance used (overrides default 10%)
- Guaranteed spawn on wave complete

**Pass:** ☐

---

### Test 14: InfiniteWaveGenerator Integration  
**Steps:**
1. Configure InfiniteWaveGenerator
2. Enter Play Mode
3. Observe power-up behavior across multiple waves

**Expected:**
- Power-up chances scale with wave settings
- Wave number syncs correctly in infinite mode

**Pass:** ☐

---

## Performance Notes

- PowerUpTracker adds minimal overhead
- Weighted selection is O(n) where n = number of power-up types (typically <10)
- No per-frame allocation in normal operation

---

## Summary

| Test | Description | Pass |
|------|-------------|------|
| 1 | Basic Spawn | ☐ |
| 2 | Weight Distribution | ☐ |
| 3 | Enemy Kill Integration | ☐ |
| 4 | Wave Complete Integration | ☐ |
| 5 | Wave Number Sync | ☐ |
| 6 | Cooldown System | ☐ |
| 7 | Max Active Limit | ☐ |
| 8 | Power-Up Tracking | ☐ |
| 9 | Spawn Zone Integration | ☐ |
| 10 | Fallback Positioning | ☐ |
| 11 | Disable/Enable Power-Ups | ☐ |
| 12 | Reset Functionality | ☐ |
| 13 | RuntimeWaveData Integration | ☐ |
| 14 | InfiniteWaveGenerator Integration | ☐ |

**Phase 4 Complete:** ☐

---

## Quick Setup Checklist

1. ☐ Add `PowerUpSpawner` component to scene
2. ☐ Configure power-up pool with prefab pool tags
3. ☐ Ensure ObjectPoolManager has matching pools
4. ☐ Set up spawn zones with PowerUp type
5. ☐ Test spawn/collect cycle
6. ☐ Verify WaveManager integration

---

## Next Phase

After Phase 4 completion, proceed to **Phase 5: Wave Director System** for dynamic difficulty adjustment and intelligent spawning decisions.
