using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.Data
{
    /// <summary>
    /// ScriptableObject that configures how infinite mode generates waves.
    /// Controls difficulty scaling, enemy pool, timing, and power-up spawning.
    /// </summary>
    [CreateAssetMenu(fileName = "InfiniteModeConfig", menuName = "Project Mayhem/Infinite Mode Config", order = 3)]
    public class InfiniteModeConfig_SO : ScriptableObject
    {
        [Header("Difficulty Scaling")]
        [Tooltip("Starting budget for wave 1. Higher = more/harder enemies.")]
        [SerializeField] private int startingBudget = 30;

        [Tooltip("Budget multiplier applied each wave. 1.2 = 20% increase per wave.")]
        [SerializeField] [Range(1.0f, 2.0f)] private float budgetMultiplierPerWave = 1.15f;

        [Tooltip("Flat budget added each wave (applied after multiplier).")]
        [SerializeField] private int flatBudgetIncreasePerWave = 5;

        [Tooltip("Maximum budget cap. Prevents waves from becoming impossibly hard.")]
        [SerializeField] private int maxBudget = 500;

        [Header("Enemy Pool")]
        [Tooltip("All enemy types that can appear in infinite mode.")]
        [SerializeField] private List<EnemyConfig_SO> availableEnemies = new List<EnemyConfig_SO>();

        [Header("Enemy Count Limits")]
        [Tooltip("Minimum enemies per wave (regardless of budget).")]
        [SerializeField] private int minEnemiesPerWave = 3;

        [Tooltip("Maximum enemies per wave (prevents overwhelming spawns).")]
        [SerializeField] private int maxEnemiesPerWave = 30;

        [Header("Spawn Timing")]
        [Tooltip("Base time between enemy spawns in wave 1.")]
        [SerializeField] private float baseSpawnInterval = 1.5f;

        [Tooltip("Minimum spawn interval (fastest possible spawning).")]
        [SerializeField] private float minSpawnInterval = 0.3f;

        [Tooltip("Spawn interval multiplier per wave. 0.95 = 5% faster each wave.")]
        [SerializeField] [Range(0.8f, 1.0f)] private float spawnIntervalReductionPerWave = 0.97f;

        [Header("Wave Timing")]
        [Tooltip("Base time between waves.")]
        [SerializeField] private float baseTimeBetweenWaves = 5f;

        [Tooltip("Minimum time between waves.")]
        [SerializeField] private float minTimeBetweenWaves = 2f;

        [Tooltip("Time reduction per wave.")]
        [SerializeField] private float timeBetweenWavesReduction = 0.1f;

        [Header("Power-Up Settings")]
        [Tooltip("Base chance (0-1) to spawn a power-up when an enemy is killed.")]
        [SerializeField] [Range(0f, 1f)] private float basePowerUpChanceOnKill = 0.05f;

        [Tooltip("Power-up chance increase per wave.")]
        [SerializeField] [Range(0f, 0.1f)] private float powerUpChanceIncreasePerWave = 0.01f;

        [Tooltip("Maximum power-up chance on kill.")]
        [SerializeField] [Range(0f, 1f)] private float maxPowerUpChanceOnKill = 0.25f;

        [Tooltip("Whether to spawn a power-up when a wave is completed.")]
        [SerializeField] private bool spawnPowerUpOnWaveComplete = true;

        [Header("Special Wave Events")]
        [Tooltip("Every N waves, spawn a 'boss wave' with special enemies.")]
        [SerializeField] private int bossWaveInterval = 10;

        [Tooltip("Budget multiplier for boss waves.")]
        [SerializeField] private float bossWaveBudgetMultiplier = 1.5f;

        [Tooltip("Every N waves, spawn a 'swarm wave' with many weak enemies.")]
        [SerializeField] private int swarmWaveInterval = 5;

        [Tooltip("Enemy count multiplier for swarm waves (budget stays same).")]
        [SerializeField] private float swarmWaveCountMultiplier = 2f;

        // Public accessors
        public int StartingBudget => startingBudget;
        public float BudgetMultiplierPerWave => budgetMultiplierPerWave;
        public int FlatBudgetIncreasePerWave => flatBudgetIncreasePerWave;
        public int MaxBudget => maxBudget;
        public IReadOnlyList<EnemyConfig_SO> AvailableEnemies => availableEnemies;
        public int MinEnemiesPerWave => minEnemiesPerWave;
        public int MaxEnemiesPerWave => maxEnemiesPerWave;
        public float BaseSpawnInterval => baseSpawnInterval;
        public float MinSpawnInterval => minSpawnInterval;
        public float SpawnIntervalReductionPerWave => spawnIntervalReductionPerWave;
        public float BaseTimeBetweenWaves => baseTimeBetweenWaves;
        public float MinTimeBetweenWaves => minTimeBetweenWaves;
        public float TimeBetweenWavesReduction => timeBetweenWavesReduction;
        public float BasePowerUpChanceOnKill => basePowerUpChanceOnKill;
        public float PowerUpChanceIncreasePerWave => powerUpChanceIncreasePerWave;
        public float MaxPowerUpChanceOnKill => maxPowerUpChanceOnKill;
        public bool SpawnPowerUpOnWaveComplete => spawnPowerUpOnWaveComplete;
        public int BossWaveInterval => bossWaveInterval;
        public float BossWaveBudgetMultiplier => bossWaveBudgetMultiplier;
        public int SwarmWaveInterval => swarmWaveInterval;
        public float SwarmWaveCountMultiplier => swarmWaveCountMultiplier;

        /// <summary>
        /// Calculates the budget for a specific wave number.
        /// </summary>
        public int CalculateBudget(int waveNumber)
        {
            // Apply multiplier: budget = starting * (multiplier ^ (wave-1))
            float multipliedBudget = startingBudget * Mathf.Pow(budgetMultiplierPerWave, waveNumber - 1);
            
            // Apply flat increase: + flatIncrease * (wave-1)
            float totalBudget = multipliedBudget + (flatBudgetIncreasePerWave * (waveNumber - 1));
            
            // Apply boss wave modifier
            if (IsBossWave(waveNumber))
            {
                totalBudget *= bossWaveBudgetMultiplier;
            }
            
            // Clamp to max
            return Mathf.Min(Mathf.RoundToInt(totalBudget), maxBudget);
        }

        /// <summary>
        /// Calculates the spawn interval for a specific wave number.
        /// </summary>
        public float CalculateSpawnInterval(int waveNumber)
        {
            float interval = baseSpawnInterval * Mathf.Pow(spawnIntervalReductionPerWave, waveNumber - 1);
            return Mathf.Max(interval, minSpawnInterval);
        }

        /// <summary>
        /// Calculates the time between waves for a specific wave number.
        /// </summary>
        public float CalculateTimeBetweenWaves(int waveNumber)
        {
            float time = baseTimeBetweenWaves - (timeBetweenWavesReduction * (waveNumber - 1));
            return Mathf.Max(time, minTimeBetweenWaves);
        }

        /// <summary>
        /// Calculates the power-up chance on kill for a specific wave number.
        /// </summary>
        public float CalculatePowerUpChance(int waveNumber)
        {
            float chance = basePowerUpChanceOnKill + (powerUpChanceIncreasePerWave * (waveNumber - 1));
            return Mathf.Min(chance, maxPowerUpChanceOnKill);
        }

        /// <summary>
        /// Checks if the specified wave is a boss wave.
        /// </summary>
        public bool IsBossWave(int waveNumber)
        {
            return bossWaveInterval > 0 && waveNumber % bossWaveInterval == 0;
        }

        /// <summary>
        /// Checks if the specified wave is a swarm wave.
        /// </summary>
        public bool IsSwarmWave(int waveNumber)
        {
            // Don't overlap with boss waves
            if (IsBossWave(waveNumber)) return false;
            return swarmWaveInterval > 0 && waveNumber % swarmWaveInterval == 0;
        }

        /// <summary>
        /// Gets enemies available for a specific wave.
        /// </summary>
        public List<EnemyConfig_SO> GetAvailableEnemiesForWave(int waveNumber)
        {
            List<EnemyConfig_SO> available = new List<EnemyConfig_SO>();
            
            foreach (var enemy in availableEnemies)
            {
                if (enemy != null && enemy.CanAppearInWave(waveNumber))
                {
                    available.Add(enemy);
                }
            }
            
            return available;
        }

        /// <summary>
        /// Gets boss enemies from the pool.
        /// </summary>
        public List<EnemyConfig_SO> GetBossEnemies()
        {
            List<EnemyConfig_SO> bosses = new List<EnemyConfig_SO>();
            
            foreach (var enemy in availableEnemies)
            {
                if (enemy != null && enemy.IsBoss)
                {
                    bosses.Add(enemy);
                }
            }
            
            return bosses;
        }

        /// <summary>
        /// Gets non-boss enemies available for a specific wave (for swarm waves).
        /// </summary>
        public List<EnemyConfig_SO> GetNonBossEnemiesForWave(int waveNumber)
        {
            List<EnemyConfig_SO> regular = new List<EnemyConfig_SO>();
            
            foreach (var enemy in availableEnemies)
            {
                if (enemy != null && !enemy.IsBoss && enemy.CanAppearInWave(waveNumber))
                {
                    regular.Add(enemy);
                }
            }
            
            return regular;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure positive values
            startingBudget = Mathf.Max(10, startingBudget);
            maxBudget = Mathf.Max(startingBudget, maxBudget);
            minEnemiesPerWave = Mathf.Max(1, minEnemiesPerWave);
            maxEnemiesPerWave = Mathf.Max(minEnemiesPerWave, maxEnemiesPerWave);
            baseSpawnInterval = Mathf.Max(0.1f, baseSpawnInterval);
            minSpawnInterval = Mathf.Clamp(minSpawnInterval, 0.1f, baseSpawnInterval);
            baseTimeBetweenWaves = Mathf.Max(1f, baseTimeBetweenWaves);
            minTimeBetweenWaves = Mathf.Clamp(minTimeBetweenWaves, 1f, baseTimeBetweenWaves);
            bossWaveInterval = Mathf.Max(0, bossWaveInterval);
            swarmWaveInterval = Mathf.Max(0, swarmWaveInterval);
        }
#endif
    }
}
