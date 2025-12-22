using System.Collections.Generic;
using UnityEngine;
using ProjectMayhem.Audio;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Manages spawning of power-ups during gameplay.
    /// Works with SpawnZoneManager to find valid spawn locations.
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance for easy access.
        /// </summary>
        public static PowerUpSpawner Instance { get; private set; }

        /// <summary>
        /// Configuration for a power-up type.
        /// </summary>
        [System.Serializable]
        public class PowerUpConfig
        {
            [Tooltip("The pool tag used to spawn this power-up from ObjectPoolManager.")]
            public string poolTag;

            [Tooltip("Relative weight for random selection. Higher = more likely.")]
            [Min(0.1f)]
            public float weight = 1f;

            [Tooltip("Minimum wave number before this power-up can appear.")]
            [Min(1)]
            public int minWave = 1;

            [Tooltip("Whether this power-up is currently enabled.")]
            public bool enabled = true;
        }

        /// <summary>
        /// Spawn trigger types for power-ups.
        /// </summary>
        public enum SpawnTrigger
        {
            EnemyKilled,
            WaveComplete,
            Manual
        }

        [Header("Power-Up Pool")]
        [Tooltip("List of available power-up configurations.")]
        [SerializeField] private List<PowerUpConfig> powerUpPool = new List<PowerUpConfig>();

        [Header("Default Spawn Settings")]
        [Tooltip("Base chance (0-1) to spawn a power-up when an enemy is killed.")]
        [Range(0f, 1f)]
        [SerializeField] private float baseChanceOnKill = 0.1f;

        [Tooltip("Whether to spawn a power-up when a wave is completed.")]
        [SerializeField] private bool spawnOnWaveComplete = true;

        [Tooltip("Chance (0-1) to spawn a power-up on wave complete.")]
        [Range(0f, 1f)]
        [SerializeField] private float waveCompleteChance = 0.5f;

        [Header("Spawn Limits")]
        [Tooltip("Maximum power-ups that can exist at once. 0 = unlimited.")]
        [SerializeField] private int maxActivePowerUps = 3;

        [Tooltip("Minimum time between power-up spawns in seconds.")]
        [SerializeField] private float spawnCooldown = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Runtime state
        private int currentWaveNumber = 1;
        private float lastSpawnTime = -999f;
        private int activePowerUpCount = 0;
        private float totalWeight = 0f;

        /// <summary>
        /// Gets or sets the current wave number for filtering power-ups.
        /// </summary>
        public int CurrentWaveNumber
        {
            get => currentWaveNumber;
            set => currentWaveNumber = Mathf.Max(1, value);
        }

        /// <summary>
        /// Gets the number of currently active power-ups.
        /// </summary>
        public int ActivePowerUpCount => activePowerUpCount;

        /// <summary>
        /// Gets whether spawning is allowed (cooldown and limit checks).
        /// </summary>
        public bool CanSpawn => 
            (maxActivePowerUps <= 0 || activePowerUpCount < maxActivePowerUps) &&
            (Time.time - lastSpawnTime >= spawnCooldown);

        /// <summary>
        /// Event fired when a power-up is spawned.
        /// </summary>
        public System.Action<string, Vector2> OnPowerUpSpawned;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("PowerUpSpawner: Duplicate instance found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CalculateTotalWeight();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            CalculateTotalWeight();
        }

        /// <summary>
        /// Recalculates the total weight for weighted random selection.
        /// </summary>
        private void CalculateTotalWeight()
        {
            totalWeight = 0f;
            foreach (var config in powerUpPool)
            {
                if (config.enabled && config.minWave <= currentWaveNumber)
                {
                    totalWeight += config.weight;
                }
            }
        }

        /// <summary>
        /// Attempts to spawn a power-up based on the given chance.
        /// </summary>
        /// <param name="chance">The chance (0-1) to spawn a power-up.</param>
        /// <param name="trigger">The trigger that caused this spawn attempt.</param>
        /// <returns>True if a power-up was spawned.</returns>
        public bool TrySpawnPowerUp(float chance, SpawnTrigger trigger = SpawnTrigger.Manual)
        {
            if (!CanSpawn)
            {
                if (debugMode)
                {
                    Debug.Log($"PowerUpSpawner: Cannot spawn - cooldown or limit reached.");
                }
                return false;
            }

            if (Random.value > chance)
            {
                if (debugMode)
                {
                    Debug.Log($"PowerUpSpawner: Chance roll failed ({chance:P0}).");
                }
                return false;
            }

            return SpawnRandomPowerUp(trigger);
        }

        /// <summary>
        /// Spawns a random power-up at a valid spawn location.
        /// </summary>
        /// <param name="trigger">The trigger that caused this spawn.</param>
        /// <returns>True if a power-up was spawned.</returns>
        public bool SpawnRandomPowerUp(SpawnTrigger trigger = SpawnTrigger.Manual)
        {
            if (!CanSpawn)
            {
                if (debugMode)
                {
                    Debug.Log($"PowerUpSpawner: Cannot spawn - cooldown or limit reached.");
                }
                return false;
            }

            // Get valid power-ups for current wave
            var validConfigs = GetValidPowerUps();
            if (validConfigs.Count == 0)
            {
                Debug.LogWarning("PowerUpSpawner: No valid power-ups available for current wave.");
                return false;
            }

            // Select random power-up based on weights
            PowerUpConfig selectedConfig = SelectWeightedRandom(validConfigs);
            if (selectedConfig == null)
            {
                Debug.LogError("PowerUpSpawner: Failed to select a power-up config.");
                return false;
            }

            // Get spawn position from SpawnZoneManager
            Vector2 spawnPosition = GetSpawnPosition();

            return SpawnPowerUpAt(selectedConfig.poolTag, spawnPosition, trigger);
        }

        /// <summary>
        /// Spawns a specific power-up at the given position.
        /// </summary>
        /// <param name="poolTag">The pool tag of the power-up to spawn.</param>
        /// <param name="position">The position to spawn at.</param>
        /// <param name="trigger">The trigger that caused this spawn.</param>
        /// <returns>True if the power-up was spawned successfully.</returns>
        public bool SpawnPowerUpAt(string poolTag, Vector2 position, SpawnTrigger trigger = SpawnTrigger.Manual)
        {
            if (string.IsNullOrEmpty(poolTag))
            {
                Debug.LogError("PowerUpSpawner: Pool tag is null or empty.");
                return false;
            }

            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("PowerUpSpawner: ObjectPoolManager not found!");
                return false;
            }

            // Spawn from pool
            GameObject powerUpObject = ObjectPoolManager.Instance.SpawnFromPool(poolTag, position, Quaternion.identity);
            if (powerUpObject == null)
            {
                Debug.LogError($"PowerUpSpawner: Failed to spawn power-up '{poolTag}' from pool.");
                return false;
            }

            // Update state
            lastSpawnTime = Time.time;
            activePowerUpCount++;

            // Register for destruction callback to track active count
            var tracker = powerUpObject.GetComponent<PowerUpTracker>();
            if (tracker == null)
            {
                tracker = powerUpObject.AddComponent<PowerUpTracker>();
            }
            tracker.Initialize(this);

            if (debugMode)
            {
                Debug.Log($"PowerUpSpawner: Spawned '{poolTag}' at {position} (trigger: {trigger}).");
            }

            OnPowerUpSpawned?.Invoke(poolTag, position);
            return true;
        }

        /// <summary>
        /// Called when an enemy is killed. Uses wave-specific or default chance.
        /// </summary>
        /// <param name="overrideChance">Optional override chance. Use negative to use defaults.</param>
        public void OnEnemyKilled(float overrideChance = -1f)
        {
            float chance = overrideChance >= 0f ? overrideChance : baseChanceOnKill;
            TrySpawnPowerUp(chance, SpawnTrigger.EnemyKilled);
        }

        /// <summary>
        /// Called when a wave is completed. Spawns power-up if enabled.
        /// </summary>
        /// <param name="spawnGuaranteed">If true, ignores chance and spawns guaranteed.</param>
        public void OnWaveComplete(bool spawnGuaranteed = false)
        {
            if (!spawnOnWaveComplete && !spawnGuaranteed)
            {
                return;
            }

            if (spawnGuaranteed)
            {
                SpawnRandomPowerUp(SpawnTrigger.WaveComplete);
            }
            else
            {
                TrySpawnPowerUp(waveCompleteChance, SpawnTrigger.WaveComplete);
            }
        }

        /// <summary>
        /// Gets all power-up configs valid for the current wave.
        /// </summary>
        private List<PowerUpConfig> GetValidPowerUps()
        {
            var valid = new List<PowerUpConfig>();
            foreach (var config in powerUpPool)
            {
                if (config.enabled && config.minWave <= currentWaveNumber)
                {
                    valid.Add(config);
                }
            }
            return valid;
        }

        /// <summary>
        /// Selects a power-up config using weighted random selection.
        /// </summary>
        private PowerUpConfig SelectWeightedRandom(List<PowerUpConfig> configs)
        {
            if (configs.Count == 0) return null;
            if (configs.Count == 1) return configs[0];

            float localTotalWeight = 0f;
            foreach (var config in configs)
            {
                localTotalWeight += config.weight;
            }

            float randomValue = Random.value * localTotalWeight;
            float cumulative = 0f;

            foreach (var config in configs)
            {
                cumulative += config.weight;
                if (randomValue <= cumulative)
                {
                    return config;
                }
            }

            return configs[configs.Count - 1];
        }

        /// <summary>
        /// Gets a spawn position using SpawnZoneManager or fallback.
        /// </summary>
        private Vector2 GetSpawnPosition()
        {
            // Try SpawnZoneManager first
            if (SpawnZoneManager.Instance != null && SpawnZoneManager.Instance.PowerUpZones.Count > 0)
            {
                return SpawnZoneManager.Instance.GetPowerUpSpawnPoint();
            }

            // Fallback: random position in camera view
            Camera cam = Camera.main;
            if (cam != null)
            {
                float halfHeight = cam.orthographicSize * 0.8f;
                float halfWidth = halfHeight * cam.aspect;
                return new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    Random.Range(-halfHeight, halfHeight)
                );
            }

            // Last resort
            return Random.insideUnitCircle * 5f;
        }

        /// <summary>
        /// Called by PowerUpTracker when a power-up is collected or destroyed.
        /// </summary>
        internal void OnPowerUpRemoved()
        {
            activePowerUpCount = Mathf.Max(0, activePowerUpCount - 1);

            if (debugMode)
            {
                Debug.Log($"PowerUpSpawner: Power-up removed. {activePowerUpCount} remaining.");
            }
        }

        /// <summary>
        /// Resets the spawner state (call when restarting level).
        /// </summary>
        public void Reset()
        {
            currentWaveNumber = 1;
            lastSpawnTime = -999f;
            activePowerUpCount = 0;
            CalculateTotalWeight();
        }

        /// <summary>
        /// Gets the power-up pool for editor inspection.
        /// </summary>
        public IReadOnlyList<PowerUpConfig> PowerUpPool => powerUpPool;

        /// <summary>
        /// Gets the base chance on kill for editor display.
        /// </summary>
        public float BaseChanceOnKill => baseChanceOnKill;

        /// <summary>
        /// Gets the wave complete chance for editor display.
        /// </summary>
        public float WaveCompleteChance => waveCompleteChance;
    }

    /// <summary>
    /// Helper component to track power-up lifetime and notify spawner on removal.
    /// </summary>
    public class PowerUpTracker : MonoBehaviour
    {
        private PowerUpSpawner spawner;
        private bool hasNotified = false;

        public void Initialize(PowerUpSpawner powerUpSpawner)
        {
            spawner = powerUpSpawner;
            hasNotified = false;
        }

        private void OnDisable()
        {
            NotifyRemoval();
        }

        private void OnDestroy()
        {
            NotifyRemoval();
        }

        private void NotifyRemoval()
        {
            if (!hasNotified && spawner != null)
            {
                hasNotified = true;
                spawner.OnPowerUpRemoved();
            }
        }
    }
}
