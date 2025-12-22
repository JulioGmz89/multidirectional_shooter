# Wave & Spawning System - Implementation Plan

## Overview

This document outlines the implementation plan for a new wave and spawning system that supports both **Campaign Mode** (hand-crafted waves) and **Infinite Mode** (procedurally generated waves). The system will provide more variety, easier tuning, and dynamic gameplay adjustments.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         WaveDirector                                │
│  • Orchestrates wave flow                                           │
│  • Monitors game state (player health, score, time)                 │
│  • Triggers dynamic events (power-ups, breathers)                   │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌──────────────────┐ ┌────────────────┐ ┌─────────────────────┐
│  IWaveProvider   │ │ SpawnZone      │ │ PowerUpSpawner      │
│  (Interface)     │ │ Manager        │ │                     │
└────────┬─────────┘ └────────────────┘ └─────────────────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌──────────────┐
│Campaign│ │ Infinite     │
│Provider│ │ Generator    │
└────────┘ └──────────────┘
```

---

## Phase 1: Spawn Zone System

**Goal:** Replace fixed spawn points with flexible spawn zones for variety.

### 1.1 SpawnZone Component

A component that defines an area where enemies/power-ups can spawn.

```csharp
// SpawnZone.cs
public class SpawnZone : MonoBehaviour
{
    public enum ZoneShape { Rectangle, Circle }
    public enum ZoneType { Enemy, PowerUp, Both }
    
    [Header("Zone Configuration")]
    public ZoneShape shape;
    public ZoneType zoneType;
    public Vector2 size;           // For rectangle
    public float radius;           // For circle
    
    [Header("Spawn Rules")]
    public float minDistanceFromPlayer;
    public bool mustBeOffScreen;
    public float weight = 1f;      // Selection probability weight
    
    public Vector2 GetRandomPointInZone();
    public bool IsValidSpawnPoint(Vector2 point, Transform player);
}
```

### 1.2 SpawnZoneManager

Manages all spawn zones and provides spawn points on request.

```csharp
// SpawnZoneManager.cs
public class SpawnZoneManager : MonoBehaviour
{
    public static SpawnZoneManager Instance { get; private set; }
    
    private List<SpawnZone> enemyZones;
    private List<SpawnZone> powerUpZones;
    
    public Vector2 GetEnemySpawnPoint();
    public Vector2 GetPowerUpSpawnPoint();
    public Vector2[] GetSpawnPoints(int count, float minSpacing);
}
```

### 1.3 Editor Visualization

Custom editor script to visualize spawn zones in the Scene view.

```csharp
// Editor/SpawnZoneEditor.cs
[CustomEditor(typeof(SpawnZone))]
public class SpawnZoneEditor : Editor
{
    // Draw gizmos for zone boundaries
    // Color-code by zone type
    // Show spawn point preview
}
```

### Files to Create:
- [x] `Assets/Scripts/Spawning/SpawnZone.cs` ✅ COMPLETED
- [x] `Assets/Scripts/Spawning/SpawnZoneManager.cs` ✅ COMPLETED
- [x] `Assets/Scripts/Editor/SpawnZoneEditor.cs` ✅ COMPLETED

### Estimated Effort: 2-3 hours

### ✅ PHASE 1 COMPLETE

---

## Phase 2: Wave Provider Abstraction

**Goal:** Create an interface that abstracts wave data generation, allowing different implementations for campaign vs infinite mode.

### 2.1 Runtime Wave Data

A runtime representation of a wave (not a ScriptableObject).

```csharp
// RuntimeWaveData.cs
[System.Serializable]
public class RuntimeWaveData
{
    [System.Serializable]
    public struct EnemySpawnInfo
    {
        public string poolTag;
        public int count;
        public float spawnInterval;
        public float initialDelay;
    }
    
    public List<EnemySpawnInfo> enemies;
    public float powerUpChance;
    public float timeToNextWave;
    public int waveNumber;
    public float difficultyMultiplier;
}
```

### 2.2 IWaveProvider Interface

```csharp
// IWaveProvider.cs
public interface IWaveProvider
{
    RuntimeWaveData GetNextWave();
    bool HasMoreWaves();
    void Reset();
    int CurrentWaveIndex { get; }
}
```

### 2.3 CampaignWaveProvider

Reads from existing Wave_SO ScriptableObjects.

```csharp
// CampaignWaveProvider.cs
public class CampaignWaveProvider : MonoBehaviour, IWaveProvider
{
    [SerializeField] private List<Wave_SO> waves;
    
    public RuntimeWaveData GetNextWave();  // Converts Wave_SO to RuntimeWaveData
    public bool HasMoreWaves();
    public void Reset();
}
```

### 2.4 Refactor WaveManager

Update WaveManager to use IWaveProvider instead of directly reading Wave_SO.

```csharp
// WaveManager.cs (Modified)
public class WaveManager : MonoBehaviour
{
    [SerializeField] private MonoBehaviour waveProviderComponent; // Must implement IWaveProvider
    private IWaveProvider waveProvider;
    
    // Use waveProvider.GetNextWave() instead of waves[index]
}
```

### Files to Create/Modify:
- [x] `Assets/Scripts/Spawning/RuntimeWaveData.cs` ✅ COMPLETED
- [x] `Assets/Scripts/Spawning/IWaveProvider.cs` ✅ COMPLETED
- [x] `Assets/Scripts/Spawning/CampaignWaveProvider.cs` ✅ COMPLETED
- [x] `Assets/Scripts/Managers/WaveManager.cs` (Modified) ✅ COMPLETED

### Estimated Effort: 2-3 hours

### ✅ PHASE 2 COMPLETE

---

## Phase 3: Procedural Wave Generator (Infinite Mode)

**Goal:** Generate waves algorithmically based on difficulty progression.

### 3.1 Enemy Configuration Data

Define enemy "costs" and properties for the generator.

```csharp
// EnemyConfig_SO.cs
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Project Mayhem/Enemy Config")]
public class EnemyConfig_SO : ScriptableObject
{
    public string poolTag;
    public int difficultyCost;      // How much "budget" this enemy consumes
    public int minWaveToAppear;     // First wave this enemy can spawn
    public float baseWeight;        // Selection probability
    public bool isBoss;
}
```

### 3.2 Infinite Mode Configuration

```csharp
// InfiniteModeConfig_SO.cs
[CreateAssetMenu(fileName = "InfiniteModeConfig", menuName = "Project Mayhem/Infinite Mode Config")]
public class InfiniteModeConfig_SO : ScriptableObject
{
    [Header("Difficulty Scaling")]
    public int startingBudget = 50;
    public float budgetIncreasePerWave = 1.2f;  // Multiplier
    public int maxBudget = 500;
    
    [Header("Enemy Pool")]
    public List<EnemyConfig_SO> availableEnemies;
    
    [Header("Timing")]
    public float baseSpawnInterval = 1.5f;
    public float minSpawnInterval = 0.3f;
    public float spawnIntervalReduction = 0.95f; // Per wave
    
    [Header("Power-ups")]
    public float basePowerUpChance = 0.1f;
    public float powerUpChanceIncrease = 0.02f;  // Per wave
    public float maxPowerUpChance = 0.4f;
}
```

### 3.3 InfiniteWaveGenerator

```csharp
// InfiniteWaveGenerator.cs
public class InfiniteWaveGenerator : MonoBehaviour, IWaveProvider
{
    [SerializeField] private InfiniteModeConfig_SO config;
    
    private int currentWave = 0;
    
    public RuntimeWaveData GetNextWave()
    {
        currentWave++;
        int budget = CalculateBudget(currentWave);
        return GenerateWave(budget);
    }
    
    private int CalculateBudget(int wave);
    private RuntimeWaveData GenerateWave(int budget);
    private List<EnemyConfig_SO> GetAvailableEnemies(int wave);
    
    public bool HasMoreWaves() => true; // Infinite!
    public void Reset() => currentWave = 0;
}
```

### 3.4 Wave Generation Algorithm

```
1. Calculate budget for wave N
2. Get list of enemies available at wave N
3. While budget > 0:
   a. Select random enemy (weighted by baseWeight)
   b. If enemy.cost <= remainingBudget:
      - Add enemy to wave
      - Subtract cost from budget
   c. If no enemies affordable, break
4. Shuffle spawn order for variety
5. Calculate spawn intervals based on wave number
6. Return RuntimeWaveData
```

### Files to Create:
- [ ] `Assets/Scripts/Data/EnemyConfig_SO.cs`
- [ ] `Assets/Scripts/Data/InfiniteModeConfig_SO.cs`
- [ ] `Assets/Scripts/Spawning/InfiniteWaveGenerator.cs`
- [ ] `Assets/Data/Enemies/` (Enemy config assets)
- [ ] `Assets/Data/InfiniteModeConfig.asset`

### Estimated Effort: 3-4 hours

---

## Phase 4: Power-Up Spawning Integration

**Goal:** Integrate power-up spawning into the wave system with dynamic triggers.

### 4.1 PowerUpSpawner

```csharp
// PowerUpSpawner.cs
public class PowerUpSpawner : MonoBehaviour
{
    public static PowerUpSpawner Instance { get; private set; }
    
    [SerializeField] private List<PowerUpConfig> powerUpPool;
    
    [System.Serializable]
    public class PowerUpConfig
    {
        public string poolTag;
        public float weight;
        public int minWave;
    }
    
    public void TrySpawnPowerUp(float chance);
    public void SpawnPowerUpAt(Vector2 position);
    public void SpawnRandomPowerUp();
    
    private string SelectRandomPowerUp();
}
```

### 4.2 Integration Points

Power-ups spawn when:
- Wave is completed (guaranteed)
- Enemy is killed (chance-based)
- Player health is low (director decision)
- Time threshold reached without power-up

### Files to Create:
- [ ] `Assets/Scripts/Spawning/PowerUpSpawner.cs`
- [ ] Modify `WaveManager.cs` to call PowerUpSpawner on wave complete

### Estimated Effort: 1-2 hours

---

## Phase 5: Wave Director System

**Goal:** Monitor game state and make dynamic decisions about spawning and difficulty.

### 5.1 WaveDirector

```csharp
// WaveDirector.cs
public class WaveDirector : MonoBehaviour
{
    public static WaveDirector Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private Health playerHealth;
    
    [Header("Intensity Settings")]
    [SerializeField] private float peakIntensityDuration = 30f;
    [SerializeField] private float restDuration = 10f;
    
    // State tracking
    private float timeSinceLastKill;
    private float timeSinceLastDamage;
    private float currentIntensity;
    private int recentKillCount;
    
    // Public queries for other systems
    public float GetDifficultyMultiplier();
    public float GetPowerUpChanceBonus();
    public bool ShouldTriggerBreather();
    public bool ShouldSpawnHelpPowerUp();
    
    // Events
    public event System.Action OnBreatherStart;
    public event System.Action OnBreatherEnd;
}
```

### 5.2 Director Decisions

| Condition | Action |
|-----------|--------|
| Player health < 30% | Increase power-up spawn chance |
| No kills in 15 seconds | Reduce spawn rate temporarily |
| 10+ kills in 10 seconds | Trigger "breather" moment |
| Wave taking too long | Spawn helpful power-up |

### Files to Create:
- [ ] `Assets/Scripts/Spawning/WaveDirector.cs`

### Estimated Effort: 2-3 hours

---

## Phase 6: Visual Editor Tool (Optional)

**Goal:** Create a custom Unity Editor window for visual wave design.

### 6.1 Features

- Timeline view showing wave progression
- Drag-and-drop enemy types onto timeline
- Preview spawn patterns in Scene view
- Difficulty curve graph
- Export to Wave_SO or RuntimeWaveData
- Import existing Wave_SO for editing

### 6.2 Implementation

```csharp
// Editor/WaveEditorWindow.cs
public class WaveEditorWindow : EditorWindow
{
    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow();
    
    private void OnGUI();
    private void DrawTimeline();
    private void DrawEnemyPalette();
    private void DrawDifficultyCurve();
    private void HandleDragAndDrop();
}
```

### Files to Create:
- [ ] `Assets/Scripts/Editor/WaveEditorWindow.cs`

### Estimated Effort: 4-6 hours (optional, lower priority)

---

## Migration Plan

### Step 1: Non-Breaking Changes
1. Implement SpawnZone system alongside existing spawn points
2. Create IWaveProvider and CampaignWaveProvider
3. Both systems coexist

### Step 2: Gradual Migration
1. Update WaveManager to use IWaveProvider
2. Create SpawnZones in scene, test thoroughly
3. Remove old spawn point references

### Step 3: New Features
1. Add InfiniteWaveGenerator
2. Implement PowerUpSpawner
3. Add WaveDirector

### Step 4: Polish
1. Tune difficulty curves
2. Create enemy configs
3. Test infinite mode extensively

---

## File Structure

```
Assets/Scripts/
├── Spawning/
│   ├── SpawnZone.cs
│   ├── SpawnZoneManager.cs
│   ├── IWaveProvider.cs
│   ├── RuntimeWaveData.cs
│   ├── CampaignWaveProvider.cs
│   ├── InfiniteWaveGenerator.cs
│   ├── PowerUpSpawner.cs
│   └── WaveDirector.cs
├── Data/
│   ├── Wave_SO.cs (existing)
│   ├── EnemyConfig_SO.cs
│   └── InfiniteModeConfig_SO.cs
├── Editor/
│   ├── SpawnZoneEditor.cs
│   └── WaveEditorWindow.cs (optional)
└── Managers/
    └── WaveManager.cs (modified)

Assets/Data/
├── Waves/ (existing)
├── Enemies/
│   ├── ChaserConfig.asset
│   └── ShooterConfig.asset
└── InfiniteModeConfig.asset
```

---

## Testing Checklist

### Phase 1 - Spawn Zones
- [ ] Zones visualize correctly in editor
- [ ] Random points are within zone boundaries
- [ ] Player distance check works
- [ ] Off-screen check works
- [ ] Weighted selection works

### Phase 2 - Wave Provider
- [ ] CampaignWaveProvider loads existing waves
- [ ] RuntimeWaveData correctly represents wave
- [ ] WaveManager works with new abstraction
- [ ] Existing campaign plays identically

### Phase 3 - Infinite Mode
- [ ] Budget calculation scales correctly
- [ ] Enemy selection respects weights
- [ ] New enemy types unlock at correct waves
- [ ] Spawn intervals decrease appropriately
- [ ] No crashes after 100+ waves

### Phase 4 - Power-ups
- [ ] Power-ups spawn at valid locations
- [ ] Spawn chance works correctly
- [ ] Wave completion triggers spawn

### Phase 5 - Director
- [ ] Low health triggers help
- [ ] Breather moments feel natural
- [ ] Difficulty scales appropriately

---

## Estimated Total Effort

| Phase | Effort | Priority |
|-------|--------|----------|
| Phase 1: Spawn Zones | 2-3 hours | High |
| Phase 2: Wave Provider | 2-3 hours | High |
| Phase 3: Infinite Generator | 3-4 hours | High |
| Phase 4: Power-up Spawning | 1-2 hours | Medium |
| Phase 5: Wave Director | 2-3 hours | Medium |
| Phase 6: Visual Editor | 4-6 hours | Low |

**Total: 14-21 hours**

---

## Next Steps

1. Review and approve this plan
2. Begin Phase 1: Spawn Zone System
3. Iterate based on testing feedback
