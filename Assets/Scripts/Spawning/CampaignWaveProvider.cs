using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Provides waves from a predefined list of Wave_SO ScriptableObjects.
    /// Used for Campaign Mode with hand-crafted wave designs.
    /// </summary>
    public class CampaignWaveProvider : MonoBehaviour, IWaveProvider
    {
        [Header("Wave Configuration")]
        [Tooltip("The list of waves to be spawned in order.")]
        [SerializeField] private List<Wave_SO> waves = new List<Wave_SO>();

        [Header("Power-Up Settings")]
        [Tooltip("Chance (0-1) to spawn a power-up when an enemy is killed.")]
        [SerializeField] private float powerUpChanceOnKill = 0.05f;

        [Tooltip("Whether to spawn a power-up when a wave is completed.")]
        [SerializeField] private bool spawnPowerUpOnWaveComplete = true;

        private int currentWaveIndex = 0;

        /// <summary>
        /// Gets the current wave index (0-based).
        /// </summary>
        public int CurrentWaveIndex => currentWaveIndex;

        /// <summary>
        /// Gets the total number of waves in the campaign.
        /// </summary>
        public int TotalWaves => waves.Count;

        /// <summary>
        /// Campaign mode is finite (has a set number of waves).
        /// </summary>
        public bool IsFinite => true;

        /// <summary>
        /// Gets the list of Wave_SO assets (for editor/debug purposes).
        /// </summary>
        public IReadOnlyList<Wave_SO> Waves => waves;

        private void Awake()
        {
            ValidateWaves();
        }

        /// <summary>
        /// Validates that all waves are properly configured.
        /// </summary>
        private void ValidateWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                if (waves[i] == null)
                {
                    Debug.LogError($"CampaignWaveProvider: Wave at index {i} is null!", this);
                    continue;
                }

                foreach (var group in waves[i].enemyGroups)
                {
                    if (group.enemyPrefab == null)
                    {
                        Debug.LogWarning($"CampaignWaveProvider: Wave {i + 1} has an enemy group with null prefab.", this);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next wave and advances the index.
        /// </summary>
        public RuntimeWaveData GetNextWave()
        {
            if (!HasMoreWaves())
            {
                Debug.LogWarning("CampaignWaveProvider: No more waves available.");
                return null;
            }

            RuntimeWaveData waveData = CreateRuntimeWaveData(currentWaveIndex);
            currentWaveIndex++;
            return waveData;
        }

        /// <summary>
        /// Peeks at the next wave without advancing the index.
        /// </summary>
        public RuntimeWaveData PeekNextWave()
        {
            if (!HasMoreWaves())
            {
                return null;
            }

            return CreateRuntimeWaveData(currentWaveIndex);
        }

        /// <summary>
        /// Creates a RuntimeWaveData from the wave at the specified index.
        /// </summary>
        private RuntimeWaveData CreateRuntimeWaveData(int index)
        {
            if (index < 0 || index >= waves.Count)
            {
                return null;
            }

            Wave_SO waveSO = waves[index];
            if (waveSO == null)
            {
                Debug.LogError($"CampaignWaveProvider: Wave at index {index} is null!");
                return null;
            }

            RuntimeWaveData data = RuntimeWaveData.FromWaveSO(waveSO, index + 1);
            
            // Apply campaign-specific settings
            data.powerUpChanceOnKill = powerUpChanceOnKill;
            data.spawnPowerUpOnComplete = spawnPowerUpOnWaveComplete;
            
            // Calculate difficulty multiplier based on wave progression
            data.difficultyMultiplier = 1f + (index * 0.1f); // 10% increase per wave

            return data;
        }

        /// <summary>
        /// Checks if there are more waves available.
        /// </summary>
        public bool HasMoreWaves()
        {
            return currentWaveIndex < waves.Count;
        }

        /// <summary>
        /// Resets the provider to wave 0.
        /// </summary>
        public void Reset()
        {
            currentWaveIndex = 0;
            Debug.Log("CampaignWaveProvider: Reset to wave 0.");
        }

        /// <summary>
        /// Gets a specific wave by index (0-based) without affecting the current index.
        /// </summary>
        public RuntimeWaveData GetWaveAt(int index)
        {
            return CreateRuntimeWaveData(index);
        }

        /// <summary>
        /// Sets the waves list (useful for runtime configuration or testing).
        /// </summary>
        public void SetWaves(List<Wave_SO> newWaves)
        {
            waves = newWaves ?? new List<Wave_SO>();
            Reset();
            ValidateWaves();
        }

        /// <summary>
        /// Adds a wave to the end of the list.
        /// </summary>
        public void AddWave(Wave_SO wave)
        {
            if (wave != null)
            {
                waves.Add(wave);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp power-up chance
            powerUpChanceOnKill = Mathf.Clamp01(powerUpChanceOnKill);
        }
#endif
    }
}
