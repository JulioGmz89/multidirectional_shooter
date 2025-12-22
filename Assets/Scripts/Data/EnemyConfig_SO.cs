using UnityEngine;

namespace ProjectMayhem.Data
{
    /// <summary>
    /// ScriptableObject that defines an enemy type's properties for the procedural wave generator.
    /// Create one of these for each enemy type in your game.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Project Mayhem/Enemy Config", order = 2)]
    public class EnemyConfig_SO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The pool tag used to spawn this enemy from ObjectPoolManager. Must match the prefab name.")]
        [SerializeField] private string poolTag;

        [Tooltip("Display name for this enemy (used in UI/debug).")]
        [SerializeField] private string displayName;

        [Header("Difficulty Settings")]
        [Tooltip("How much 'budget' this enemy consumes when added to a wave. Higher = harder enemy.")]
        [SerializeField] private int difficultyCost = 10;

        [Tooltip("The minimum wave number where this enemy can appear (1-indexed).")]
        [SerializeField] private int minWaveToAppear = 1;

        [Tooltip("Base selection weight. Higher values make this enemy more likely to be chosen.")]
        [SerializeField] [Range(0.1f, 10f)] private float baseWeight = 1f;

        [Header("Special Properties")]
        [Tooltip("If true, this enemy is treated as a boss and has special spawning rules.")]
        [SerializeField] private bool isBoss = false;

        [Tooltip("Maximum number of this enemy type that can spawn in a single wave. 0 = unlimited.")]
        [SerializeField] private int maxPerWave = 0;

        [Tooltip("If true, only one of this enemy type can be alive at a time.")]
        [SerializeField] private bool isUnique = false;

        [Header("Spawn Modifiers")]
        [Tooltip("Multiplier for spawn interval when spawning this enemy. Higher = slower spawning.")]
        [SerializeField] [Range(0.5f, 3f)] private float spawnIntervalMultiplier = 1f;

        [Tooltip("Additional delay before spawning this enemy type.")]
        [SerializeField] private float initialSpawnDelay = 0f;

        // Public accessors
        public string PoolTag => poolTag;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? poolTag : displayName;
        public int DifficultyCost => difficultyCost;
        public int MinWaveToAppear => minWaveToAppear;
        public float BaseWeight => baseWeight;
        public bool IsBoss => isBoss;
        public int MaxPerWave => maxPerWave;
        public bool IsUnique => isUnique;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;
        public float InitialSpawnDelay => initialSpawnDelay;

        /// <summary>
        /// Checks if this enemy can appear in the specified wave.
        /// </summary>
        public bool CanAppearInWave(int waveNumber)
        {
            return waveNumber >= minWaveToAppear;
        }

        /// <summary>
        /// Gets the effective weight for this enemy at the specified wave.
        /// Can be overridden to implement wave-based weight scaling.
        /// </summary>
        public virtual float GetWeightForWave(int waveNumber)
        {
            // Base implementation: constant weight
            // Could be extended to increase weight as waves progress
            return baseWeight;
        }

        /// <summary>
        /// Checks if adding this enemy would exceed the max per wave limit.
        /// </summary>
        public bool CanAddMore(int currentCount)
        {
            if (maxPerWave <= 0) return true; // Unlimited
            return currentCount < maxPerWave;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure pool tag is set
            if (string.IsNullOrEmpty(poolTag))
            {
                poolTag = name;
            }

            // Ensure positive values
            difficultyCost = Mathf.Max(1, difficultyCost);
            minWaveToAppear = Mathf.Max(1, minWaveToAppear);
            maxPerWave = Mathf.Max(0, maxPerWave);
        }

        /// <summary>
        /// Editor helper to set pool tag from prefab name.
        /// </summary>
        public void SetPoolTagFromPrefab(GameObject prefab)
        {
            if (prefab != null)
            {
                poolTag = prefab.name;
            }
        }
#endif
    }
}
