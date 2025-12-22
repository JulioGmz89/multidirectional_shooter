using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Runtime representation of a wave. This is a plain C# class (not a ScriptableObject)
    /// that can be created at runtime by both CampaignWaveProvider and InfiniteWaveGenerator.
    /// </summary>
    [System.Serializable]
    public class RuntimeWaveData
    {
        /// <summary>
        /// Information about a group of enemies to spawn.
        /// </summary>
        [System.Serializable]
        public struct EnemySpawnInfo
        {
            /// <summary>
            /// The pool tag used to spawn this enemy from ObjectPoolManager.
            /// </summary>
            public string poolTag;

            /// <summary>
            /// Number of enemies of this type to spawn.
            /// </summary>
            public int count;

            /// <summary>
            /// Time in seconds between spawning each enemy of this type.
            /// </summary>
            public float spawnInterval;

            /// <summary>
            /// Optional delay before starting to spawn this enemy group.
            /// </summary>
            public float initialDelay;

            /// <summary>
            /// Creates a new EnemySpawnInfo.
            /// </summary>
            public EnemySpawnInfo(string poolTag, int count, float spawnInterval, float initialDelay = 0f)
            {
                this.poolTag = poolTag;
                this.count = count;
                this.spawnInterval = spawnInterval;
                this.initialDelay = initialDelay;
            }
        }

        /// <summary>
        /// The wave number (1-indexed for display purposes).
        /// </summary>
        public int waveNumber;

        /// <summary>
        /// List of enemy groups to spawn in this wave.
        /// </summary>
        public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();

        /// <summary>
        /// Chance (0-1) to spawn a power-up when an enemy is killed during this wave.
        /// </summary>
        public float powerUpChanceOnKill;

        /// <summary>
        /// Whether to guarantee a power-up spawn when the wave is completed.
        /// </summary>
        public bool spawnPowerUpOnComplete;

        /// <summary>
        /// Time in seconds to wait before starting the next wave.
        /// </summary>
        public float timeToNextWave;

        /// <summary>
        /// Difficulty multiplier for this wave (used by director system).
        /// </summary>
        public float difficultyMultiplier = 1f;

        /// <summary>
        /// Gets the total number of enemies in this wave.
        /// </summary>
        public int TotalEnemyCount
        {
            get
            {
                int total = 0;
                foreach (var group in enemies)
                {
                    total += group.count;
                }
                return total;
            }
        }

        /// <summary>
        /// Gets the estimated duration of this wave based on spawn intervals.
        /// </summary>
        public float EstimatedSpawnDuration
        {
            get
            {
                float duration = 0f;
                foreach (var group in enemies)
                {
                    duration += group.initialDelay;
                    duration += (group.count - 1) * group.spawnInterval;
                }
                return duration;
            }
        }

        /// <summary>
        /// Creates an empty RuntimeWaveData.
        /// </summary>
        public RuntimeWaveData()
        {
            enemies = new List<EnemySpawnInfo>();
        }

        /// <summary>
        /// Creates a RuntimeWaveData with the specified wave number.
        /// </summary>
        public RuntimeWaveData(int waveNumber) : this()
        {
            this.waveNumber = waveNumber;
        }

        /// <summary>
        /// Adds an enemy group to this wave.
        /// </summary>
        public void AddEnemyGroup(string poolTag, int count, float spawnInterval, float initialDelay = 0f)
        {
            enemies.Add(new EnemySpawnInfo(poolTag, count, spawnInterval, initialDelay));
        }

        /// <summary>
        /// Creates a RuntimeWaveData from a Wave_SO ScriptableObject.
        /// </summary>
        public static RuntimeWaveData FromWaveSO(Wave_SO waveSO, int waveNumber)
        {
            if (waveSO == null)
            {
                Debug.LogError("Cannot create RuntimeWaveData from null Wave_SO");
                return null;
            }

            RuntimeWaveData data = new RuntimeWaveData(waveNumber);
            data.timeToNextWave = waveSO.timeToNextWave;
            data.spawnPowerUpOnComplete = true; // Default behavior for campaign waves

            foreach (var group in waveSO.enemyGroups)
            {
                if (group.enemyPrefab != null)
                {
                    data.AddEnemyGroup(
                        poolTag: group.enemyPrefab.name,
                        count: group.count,
                        spawnInterval: group.spawnInterval
                    );
                }
                else
                {
                    Debug.LogWarning($"Wave {waveNumber} has an enemy group with null prefab. Skipping.");
                }
            }

            return data;
        }

        public override string ToString()
        {
            return $"Wave {waveNumber}: {TotalEnemyCount} enemies, {enemies.Count} groups, " +
                   $"~{EstimatedSpawnDuration:F1}s spawn time, {timeToNextWave}s until next";
        }
    }
}
