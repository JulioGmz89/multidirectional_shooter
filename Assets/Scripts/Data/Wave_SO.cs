using UnityEngine;

/// <summary>
/// A ScriptableObject that defines the properties of a single enemy wave.
/// </summary>
[CreateAssetMenu(fileName = "NewWave", menuName = "Project Mayhem/Wave", order = 1)]
public class Wave_SO : ScriptableObject
{
    [System.Serializable]
    public struct EnemySpawn
    {
        [Tooltip("The enemy prefab to spawn. This must have a corresponding pool in the ObjectPoolManager.")]
        public GameObject enemyPrefab;
        [Tooltip("The number of enemies of this type to spawn.")]
        public int count;
        [Tooltip("The time in seconds between each enemy spawn.")]
        public float spawnInterval;
    }

    [Header("Wave Properties")]
    [Tooltip("The groups of enemies to spawn in this wave.")]
    public EnemySpawn[] enemyGroups;

    [Tooltip("The time in seconds to wait before starting the next wave after this one is complete.")]
    public float timeToNextWave = 5f;
}
