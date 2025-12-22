using System.Collections.Generic;
using UnityEngine;
using ProjectMayhem.Data;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Procedurally generates waves for Infinite Mode based on configuration settings.
    /// Implements IWaveProvider for seamless integration with WaveManager.
    /// </summary>
    public class InfiniteWaveGenerator : MonoBehaviour, IWaveProvider
    {
        [Header("Configuration")]
        [Tooltip("The configuration asset that defines infinite mode settings.")]
        [SerializeField] private InfiniteModeConfig_SO config;

        [Header("Debug")]
        [Tooltip("Enable to log detailed wave generation info to console.")]
        [SerializeField] private bool debugLogging = false;

        // Internal state
        private int currentWaveIndex = 0;
        private System.Random randomGenerator;
        private int seed;

        /// <summary>
        /// Gets the current wave index (0-based, so wave 1 = index 0 after first GetNextWave).
        /// </summary>
        public int CurrentWaveIndex => currentWaveIndex;

        /// <summary>
        /// Returns -1 to indicate infinite waves.
        /// </summary>
        public int TotalWaves => -1;

        /// <summary>
        /// Infinite mode is not finite.
        /// </summary>
        public bool IsFinite => false;

        /// <summary>
        /// Event fired when a special wave is generated (boss, swarm).
        /// </summary>
        public event System.Action<int, string> OnSpecialWave;

        private void Awake()
        {
            // Initialize with a random seed based on time
            seed = System.Environment.TickCount;
            randomGenerator = new System.Random(seed);
        }

        /// <summary>
        /// Sets a specific seed for reproducible wave generation.
        /// Useful for daily challenges or seeded runs.
        /// </summary>
        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            randomGenerator = new System.Random(seed);
            currentWaveIndex = 0;
            
            if (debugLogging)
            {
                Debug.Log($"InfiniteWaveGenerator: Seed set to {seed}");
            }
        }

        /// <summary>
        /// Gets the current seed.
        /// </summary>
        public int GetSeed() => seed;

        /// <summary>
        /// Gets the next wave and advances the wave counter.
        /// </summary>
        public RuntimeWaveData GetNextWave()
        {
            currentWaveIndex++;
            return GenerateWave(currentWaveIndex);
        }

        /// <summary>
        /// Peeks at the next wave without advancing the counter.
        /// </summary>
        public RuntimeWaveData PeekNextWave()
        {
            // Create a temporary random generator with same state to peek
            // Note: This is a simple implementation; for true peek we'd need to save/restore state
            return GenerateWavePreview(currentWaveIndex + 1);
        }

        /// <summary>
        /// Infinite mode always has more waves.
        /// </summary>
        public bool HasMoreWaves() => true;

        /// <summary>
        /// Resets the generator to wave 0.
        /// </summary>
        public void Reset()
        {
            currentWaveIndex = 0;
            randomGenerator = new System.Random(seed);
            
            if (debugLogging)
            {
                Debug.Log("InfiniteWaveGenerator: Reset to wave 0");
            }
        }

        /// <summary>
        /// Generates a wave for the specified wave number.
        /// </summary>
        private RuntimeWaveData GenerateWave(int waveNumber)
        {
            if (config == null)
            {
                Debug.LogError("InfiniteWaveGenerator: No config assigned!");
                return CreateEmptyWave(waveNumber);
            }

            // Determine wave type
            bool isBossWave = config.IsBossWave(waveNumber);
            bool isSwarmWave = config.IsSwarmWave(waveNumber);

            // Fire special wave event
            if (isBossWave)
            {
                OnSpecialWave?.Invoke(waveNumber, "Boss");
            }
            else if (isSwarmWave)
            {
                OnSpecialWave?.Invoke(waveNumber, "Swarm");
            }

            // Calculate budget
            int budget = config.CalculateBudget(waveNumber);

            // Get available enemies
            List<EnemyConfig_SO> availableEnemies;
            if (isBossWave)
            {
                availableEnemies = config.GetAvailableEnemiesForWave(waveNumber);
            }
            else if (isSwarmWave)
            {
                availableEnemies = config.GetNonBossEnemiesForWave(waveNumber);
            }
            else
            {
                availableEnemies = config.GetAvailableEnemiesForWave(waveNumber);
            }

            if (availableEnemies.Count == 0)
            {
                Debug.LogWarning($"InfiniteWaveGenerator: No enemies available for wave {waveNumber}");
                return CreateEmptyWave(waveNumber);
            }

            // Generate enemy composition
            List<EnemySpawnEntry> spawnEntries = GenerateEnemyComposition(budget, availableEnemies, waveNumber, isSwarmWave);

            // Create RuntimeWaveData
            RuntimeWaveData wave = new RuntimeWaveData(waveNumber);
            wave.timeToNextWave = config.CalculateTimeBetweenWaves(waveNumber);
            wave.powerUpChanceOnKill = config.CalculatePowerUpChance(waveNumber);
            wave.spawnPowerUpOnComplete = config.SpawnPowerUpOnWaveComplete;
            wave.difficultyMultiplier = 1f + ((waveNumber - 1) * 0.1f);

            // Calculate spawn interval for this wave
            float baseInterval = config.CalculateSpawnInterval(waveNumber);

            // Convert spawn entries to RuntimeWaveData format
            foreach (var entry in spawnEntries)
            {
                float interval = baseInterval * entry.enemy.SpawnIntervalMultiplier;
                wave.AddEnemyGroup(
                    entry.enemy.PoolTag,
                    entry.count,
                    interval,
                    entry.enemy.InitialSpawnDelay
                );
            }

            if (debugLogging)
            {
                string waveType = isBossWave ? " [BOSS]" : (isSwarmWave ? " [SWARM]" : "");
                Debug.Log($"InfiniteWaveGenerator: Generated Wave {waveNumber}{waveType} - " +
                          $"Budget: {budget}, Enemies: {wave.TotalEnemyCount}, Groups: {wave.enemies.Count}");
            }

            return wave;
        }

        /// <summary>
        /// Generates a preview of a wave (for UI/debug purposes).
        /// </summary>
        private RuntimeWaveData GenerateWavePreview(int waveNumber)
        {
            // Use a separate random instance to not affect main generation
            var previewRandom = new System.Random(seed + waveNumber);
            
            if (config == null)
            {
                return CreateEmptyWave(waveNumber);
            }

            int budget = config.CalculateBudget(waveNumber);
            var availableEnemies = config.GetAvailableEnemiesForWave(waveNumber);
            
            if (availableEnemies.Count == 0)
            {
                return CreateEmptyWave(waveNumber);
            }

            // Simplified preview generation
            RuntimeWaveData preview = new RuntimeWaveData(waveNumber);
            preview.timeToNextWave = config.CalculateTimeBetweenWaves(waveNumber);
            preview.difficultyMultiplier = 1f + ((waveNumber - 1) * 0.1f);

            // Estimate enemy count
            int avgCost = 0;
            foreach (var enemy in availableEnemies)
            {
                avgCost += enemy.DifficultyCost;
            }
            avgCost = Mathf.Max(1, avgCost / availableEnemies.Count);
            
            int estimatedCount = Mathf.Clamp(budget / avgCost, config.MinEnemiesPerWave, config.MaxEnemiesPerWave);
            
            // Just add a placeholder entry for preview
            preview.AddEnemyGroup("Preview", estimatedCount, config.CalculateSpawnInterval(waveNumber));

            return preview;
        }

        /// <summary>
        /// Helper struct to track enemy composition during generation.
        /// </summary>
        private struct EnemySpawnEntry
        {
            public EnemyConfig_SO enemy;
            public int count;
        }

        /// <summary>
        /// Generates the enemy composition for a wave using the budget system.
        /// </summary>
        private List<EnemySpawnEntry> GenerateEnemyComposition(int budget, List<EnemyConfig_SO> availableEnemies, int waveNumber, bool isSwarmWave)
        {
            List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
            Dictionary<EnemyConfig_SO, int> enemyCounts = new Dictionary<EnemyConfig_SO, int>();
            
            int remainingBudget = budget;
            int totalEnemies = 0;
            int maxEnemies = config.MaxEnemiesPerWave;
            
            // For swarm waves, allow more enemies
            if (isSwarmWave)
            {
                maxEnemies = Mathf.RoundToInt(maxEnemies * config.SwarmWaveCountMultiplier);
            }

            // Calculate total weight for weighted random selection
            float totalWeight = 0f;
            foreach (var enemy in availableEnemies)
            {
                totalWeight += enemy.GetWeightForWave(waveNumber);
            }

            // Keep adding enemies until budget is exhausted or max reached
            int safetyCounter = 1000; // Prevent infinite loops
            while (remainingBudget > 0 && totalEnemies < maxEnemies && safetyCounter > 0)
            {
                safetyCounter--;

                // Find an affordable enemy
                EnemyConfig_SO selectedEnemy = SelectWeightedEnemy(availableEnemies, totalWeight, remainingBudget, enemyCounts, waveNumber);
                
                if (selectedEnemy == null)
                {
                    // No affordable enemies left
                    break;
                }

                // Add enemy
                if (!enemyCounts.ContainsKey(selectedEnemy))
                {
                    enemyCounts[selectedEnemy] = 0;
                }
                enemyCounts[selectedEnemy]++;
                remainingBudget -= selectedEnemy.DifficultyCost;
                totalEnemies++;
            }

            // Ensure minimum enemy count
            while (totalEnemies < config.MinEnemiesPerWave && availableEnemies.Count > 0 && safetyCounter > 0)
            {
                safetyCounter--;
                
                // Add the cheapest enemy
                EnemyConfig_SO cheapest = GetCheapestEnemy(availableEnemies);
                if (cheapest == null) break;
                
                if (!enemyCounts.ContainsKey(cheapest))
                {
                    enemyCounts[cheapest] = 0;
                }
                enemyCounts[cheapest]++;
                totalEnemies++;
            }

            // Convert to entries list
            foreach (var kvp in enemyCounts)
            {
                entries.Add(new EnemySpawnEntry { enemy = kvp.Key, count = kvp.Value });
            }

            // Shuffle the order for variety
            ShuffleList(entries);

            return entries;
        }

        /// <summary>
        /// Selects an enemy using weighted random selection.
        /// </summary>
        private EnemyConfig_SO SelectWeightedEnemy(List<EnemyConfig_SO> enemies, float totalWeight, int remainingBudget, Dictionary<EnemyConfig_SO, int> currentCounts, int waveNumber)
        {
            // Filter to affordable enemies that haven't hit their max
            List<EnemyConfig_SO> affordable = new List<EnemyConfig_SO>();
            float affordableWeight = 0f;

            foreach (var enemy in enemies)
            {
                if (enemy.DifficultyCost <= remainingBudget)
                {
                    int currentCount = currentCounts.ContainsKey(enemy) ? currentCounts[enemy] : 0;
                    if (enemy.CanAddMore(currentCount))
                    {
                        affordable.Add(enemy);
                        affordableWeight += enemy.GetWeightForWave(waveNumber);
                    }
                }
            }

            if (affordable.Count == 0 || affordableWeight <= 0)
            {
                return null;
            }

            // Weighted random selection
            float randomValue = (float)randomGenerator.NextDouble() * affordableWeight;
            float currentWeight = 0f;

            foreach (var enemy in affordable)
            {
                currentWeight += enemy.GetWeightForWave(waveNumber);
                if (randomValue <= currentWeight)
                {
                    return enemy;
                }
            }

            // Fallback
            return affordable[affordable.Count - 1];
        }

        /// <summary>
        /// Gets the cheapest enemy from the list.
        /// </summary>
        private EnemyConfig_SO GetCheapestEnemy(List<EnemyConfig_SO> enemies)
        {
            EnemyConfig_SO cheapest = null;
            int lowestCost = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.DifficultyCost < lowestCost)
                {
                    lowestCost = enemy.DifficultyCost;
                    cheapest = enemy;
                }
            }

            return cheapest;
        }

        /// <summary>
        /// Shuffles a list using Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = randomGenerator.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Creates an empty wave (fallback for errors).
        /// </summary>
        private RuntimeWaveData CreateEmptyWave(int waveNumber)
        {
            RuntimeWaveData wave = new RuntimeWaveData(waveNumber);
            wave.timeToNextWave = 3f;
            wave.difficultyMultiplier = 1f;
            return wave;
        }

        /// <summary>
        /// Sets the configuration at runtime.
        /// </summary>
        public void SetConfig(InfiniteModeConfig_SO newConfig)
        {
            config = newConfig;
            Reset();
        }

#if UNITY_EDITOR
        [Header("Editor Testing")]
        [SerializeField] private int testWaveNumber = 1;

        /// <summary>
        /// Editor method to preview a specific wave.
        /// </summary>
        [ContextMenu("Preview Test Wave")]
        private void PreviewTestWave()
        {
            if (config == null)
            {
                Debug.LogError("No config assigned!");
                return;
            }

            // Temporarily enable debug logging
            bool wasLogging = debugLogging;
            debugLogging = true;

            var wave = GenerateWavePreview(testWaveNumber);
            Debug.Log($"=== Wave {testWaveNumber} Preview ===\n" +
                      $"Budget: {config.CalculateBudget(testWaveNumber)}\n" +
                      $"Spawn Interval: {config.CalculateSpawnInterval(testWaveNumber):F2}s\n" +
                      $"Time to Next: {config.CalculateTimeBetweenWaves(testWaveNumber):F1}s\n" +
                      $"Power-up Chance: {config.CalculatePowerUpChance(testWaveNumber):P0}\n" +
                      $"Is Boss Wave: {config.IsBossWave(testWaveNumber)}\n" +
                      $"Is Swarm Wave: {config.IsSwarmWave(testWaveNumber)}");

            debugLogging = wasLogging;
        }

        /// <summary>
        /// Editor method to show difficulty curve.
        /// </summary>
        [ContextMenu("Show Difficulty Curve (20 waves)")]
        private void ShowDifficultyCurve()
        {
            if (config == null)
            {
                Debug.LogError("No config assigned!");
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Difficulty Curve ===");
            sb.AppendLine("Wave | Budget | Interval | Enemies (Est)");
            sb.AppendLine("-----|--------|----------|---------------");

            for (int i = 1; i <= 20; i++)
            {
                int budget = config.CalculateBudget(i);
                float interval = config.CalculateSpawnInterval(i);
                int estEnemies = Mathf.Clamp(budget / 10, config.MinEnemiesPerWave, config.MaxEnemiesPerWave);
                string special = config.IsBossWave(i) ? " [BOSS]" : (config.IsSwarmWave(i) ? " [SWARM]" : "");
                
                sb.AppendLine($"  {i,2} |   {budget,4} |   {interval,5:F2}s |  ~{estEnemies}{special}");
            }

            Debug.Log(sb.ToString());
        }
#endif
    }
}
