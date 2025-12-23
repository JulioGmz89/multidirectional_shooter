# Phase 5 Test Plan: Wave Director System

## Overview
This test plan verifies the WaveDirector system that monitors game state and dynamically adjusts difficulty and power-up spawning.

---

## Test Setup

### Prerequisites
1. WaveManager configured and working (from Phase 2)
2. PowerUpSpawner configured (from Phase 4)
3. Player object with Health component

### WaveDirector Configuration
1. Create GameObject named "WaveDirector"
2. Add `WaveDirector` component
3. Player Health will be auto-found, or assign manually
4. Default settings should work for testing

---

## Test Cases

### Test 1: Auto-Setup
**Steps:**
1. Add WaveDirector to scene without assigning Player Health
2. Ensure Player exists with "Player" tag and Health component
3. Enter Play Mode

**Expected:**
- WaveDirector auto-finds player Health
- No errors in console
- Inspector shows player health percent updating

**Pass:** ☐

---

### Test 2: Intensity Phase Transitions
**Steps:**
1. Enter Play Mode
2. Observe Inspector's "Phase" indicator
3. Wait and watch phase transitions

**Expected:**
- Starts in "BuildUp" phase (blue)
- Transitions to "Peak" after ~15s or rapid kills (red)
- May transition to "Sustain" (orange) or "Relax" (green)

**Pass:** ☐

---

### Test 3: Kill Tracking
**Steps:**
1. Enter Play Mode
2. Kill enemies rapidly (or click "Simulate Kill" button)
3. Watch "Recent Kills" counter

**Expected:**
- Recent Kills increases with each kill
- Count decreases after 10 seconds (tracking window)
- Time Since Kill resets to 0 on each kill

**Pass:** ☐

---

### Test 4: Rapid Kill Breather Trigger
**Steps:**
1. Enter Play Mode
2. Click "Simulate Kill" 10+ times rapidly
3. Watch for phase change to "Relax"

**Expected:**
- After 10 rapid kills, breather triggers
- Phase changes to "Relax" (green)
- Intensity drops to ~0.2
- Returns to "BuildUp" after relaxDuration (8s)

**Pass:** ☐

---

### Test 5: Health Tracking
**Steps:**
1. Enter Play Mode
2. Observe "Player Health" bar and percentage
3. Take damage (or modify health in inspector)

**Expected:**
- Health bar updates in real-time
- Status indicators light up:
  - "Low HP" at ≤30% (orange)
  - "Critical" at ≤15% (red)

**Pass:** ☐

---

### Test 6: Low Health Power-Up Bonus
**Steps:**
1. Enter Play Mode
2. Check "Power-Up Bonus" at full health (should be +0%)
3. Reduce player health to 30%
4. Check Power-Up Bonus again

**Expected:**
- At low health: +20% bonus shown
- At critical health: +40% bonus shown
- Bonus resets when health recovers

**Pass:** ☐

---

### Test 7: Difficulty Multiplier
**Steps:**
1. Enter Play Mode
2. Check "Difficulty Multiplier" in various states:
   - Full health, no kills
   - Low health
   - Critical health
   - During Relax phase
   - High kill streak at high health

**Expected:**
- Normal state: ~1.0x
- Low health: ~0.7x
- Critical health: ~0.56x
- Relax phase: ~0.6x
- High performance: ~1.2x

**Pass:** ☐

---

### Test 8: Spawn Interval Modifier
**Steps:**
1. Enter Play Mode
2. Check "Spawn Interval" multiplier
3. Trigger different phases/states

**Expected:**
- Normal: 1.0x
- Relax phase: 1.5x (slower spawns)
- Low health: 1.25x (slower)
- Peak phase at high health: 0.85x (faster)

**Pass:** ☐

---

### Test 9: WaveManager Integration - Kill Notification
**Steps:**
1. Enter Play Mode
2. Start a wave and kill an enemy
3. Watch WaveDirector stats update

**Expected:**
- Recent Kills increases
- Time Since Kill resets
- Console shows kill debug (if debug enabled)

**Pass:** ☐

---

### Test 10: WaveManager Integration - Power-Up Bonus
**Steps:**
1. Configure PowerUpSpawner with low base chance (5%)
2. Enter Play Mode, reduce health to critical
3. Kill enemies and observe spawn behavior

**Expected:**
- Power-up chance = base (5%) + director bonus (40%) = 45%
- More power-ups spawn when struggling

**Pass:** ☐

---

### Test 11: WaveManager Integration - Spawn Interval
**Steps:**
1. Enter Play Mode
2. Trigger Relax phase (rapid kills)
3. Start a new wave during Relax
4. Observe enemy spawn timing

**Expected:**
- Enemies spawn slower (1.5x interval)
- Gives player breathing room

**Pass:** ☐

---

### Test 12: Help Power-Up Spawn
**Steps:**
1. Enter Play Mode
2. Reduce health to critical (<15%)
3. Ensure PowerUpSpawner.CanSpawn is true

**Expected:**
- Director triggers help power-up spawn
- Console shows "Spawned help power-up for struggling player"

**Pass:** ☐

---

### Test 13: Wave Duration Tracking
**Steps:**
1. Set expectedWaveDuration to 30s (shorter for testing)
2. Enter Play Mode, start wave
3. Wait 30+ seconds without completing wave

**Expected:**
- "Wave Long" indicator lights up
- Power-Up Bonus increases
- Difficulty Multiplier decreases

**Pass:** ☐

---

### Test 14: Event Firing
**Steps:**
1. Subscribe to WaveDirector events in test script:
   - OnBreatherStart
   - OnBreatherEnd
   - OnPhaseChanged
   - OnPlayerLowHealth
   - OnPlayerRecovered
2. Trigger each condition

**Expected:**
- Events fire at appropriate times
- OnBreatherStart includes duration parameter
- OnPhaseChanged includes new phase

**Pass:** ☐

---

### Test 15: Reset Functionality
**Steps:**
1. Enter Play Mode
2. Advance through waves, trigger various states
3. Click "Reset Director"

**Expected:**
- Phase returns to BuildUp
- Intensity returns to 0.5
- All timers reset
- Kill tracking cleared

**Pass:** ☐

---

### Test 16: Peak Phase Duration
**Steps:**
1. Enter Play Mode
2. Get to Peak phase (kill enemies)
3. Wait for peakDuration (30s)

**Expected:**
- After 30s of Peak, transitions to Sustain
- If breather conditions met, goes to Relax instead

**Pass:** ☐

---

### Test 17: Editor Testing Buttons
**Steps:**
1. Enter Play Mode
2. Click "Simulate Kill" - watch kill stats
3. Click "Simulate Damage" - watch damage timer
4. Click "Reset Director" - watch state reset

**Expected:**
- All buttons function correctly
- Stats update immediately

**Pass:** ☐

---

## Integration Test

### Full Gameplay Loop
**Steps:**
1. Configure all systems (SpawnZones, WaveManager, PowerUpSpawner, WaveDirector)
2. Play through multiple waves
3. Vary playstyle:
   - Kill quickly (should trigger breathers)
   - Take damage (should get help)
   - Play slowly (should get easier)

**Expected:**
- Game feels responsive to player skill
- Struggling players get help
- Skilled players get challenge
- Natural pacing with breathers

**Pass:** ☐

---

## Performance Notes

- WaveDirector updates every frame but minimal overhead
- Kill queue cleanup is O(n) but n is typically <20
- Event subscriptions cleaned up in OnDestroy

---

## Summary

| Test | Description | Pass |
|------|-------------|------|
| 1 | Auto-Setup | ☐ |
| 2 | Intensity Phase Transitions | ☐ |
| 3 | Kill Tracking | ☐ |
| 4 | Rapid Kill Breather | ☐ |
| 5 | Health Tracking | ☐ |
| 6 | Low Health Power-Up Bonus | ☐ |
| 7 | Difficulty Multiplier | ☐ |
| 8 | Spawn Interval Modifier | ☐ |
| 9 | WaveManager - Kill Notification | ☐ |
| 10 | WaveManager - Power-Up Bonus | ☐ |
| 11 | WaveManager - Spawn Interval | ☐ |
| 12 | Help Power-Up Spawn | ☐ |
| 13 | Wave Duration Tracking | ☐ |
| 14 | Event Firing | ☐ |
| 15 | Reset Functionality | ☐ |
| 16 | Peak Phase Duration | ☐ |
| 17 | Editor Testing Buttons | ☐ |
| Integration | Full Gameplay Loop | ☐ |

**Phase 5 Complete:** ☐

---

## Quick Setup Checklist

1. ☐ Add `WaveDirector` component to scene
2. ☐ Verify player auto-detection works
3. ☐ Test intensity phases visually
4. ☐ Verify WaveManager integration
5. ☐ Test power-up bonus at low health
6. ☐ Playtest full loop

---

## Next Steps

Phase 5 completes the core spawning system! Optional Phase 6 adds a visual wave editor tool for designers.

### System Complete Features:
- ✅ Spawn Zones (flexible spawn areas)
- ✅ Wave Provider Abstraction (Campaign + Infinite modes)
- ✅ Procedural Wave Generation (Infinite Mode)
- ✅ Power-Up Spawning (integrated with waves)
- ✅ Wave Director (dynamic difficulty)
