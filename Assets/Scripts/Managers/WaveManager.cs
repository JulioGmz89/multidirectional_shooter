using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning of enemy waves based on ScriptableObject definitions.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // Singleton instance
    public static WaveManager Instance { get; private set; }

    [Header("Wave Configuration")]
    [Tooltip("The list of waves to be spawned in order.")]
    [SerializeField] private List<Wave_SO> waves;
    [Tooltip("The spawn points for enemies.")]
    [SerializeField] private Transform[] spawnPoints;

    private int currentWaveIndex = 0;
    private int enemiesAlive;
    private bool waveIsSpawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        if (newState == GameState.Gameplay)
        {
            // Start the first wave when gameplay begins
            if (currentWaveIndex == 0)
            {
                StartNextWave();
            }
        }
    }

    private void StartNextWave()
    {
        if (currentWaveIndex < waves.Count)
        {
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }
        else
        {
            // All waves have been successfully cleared.
            Debug.Log("All waves completed! Player wins!");
            GameStateManager.Instance.ChangeState(GameState.Victory);
        }
    }

    private IEnumerator SpawnWave(Wave_SO wave)
    {
        Debug.Log($"Starting Wave {currentWaveIndex + 1}");
        waveIsSpawning = true;
        enemiesAlive = 0;
        foreach (var group in wave.enemyGroups)
        {
            enemiesAlive += group.count;
        }

        foreach (var group in wave.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                if (spawnPoints.Length == 0)
                {
                    Debug.LogError("No spawn points assigned in WaveManager.");
                    yield break;
                }

                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                string poolTag = group.enemyPrefab.name;
                ObjectPoolManager.Instance.SpawnFromPool(poolTag, spawnPoint.position, spawnPoint.rotation);
                
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }

        waveIsSpawning = false;
    }

    /// <summary>
    /// Called by enemies when they are defeated.
    /// </summary>
    public void OnEnemyDefeated()
    {
        enemiesAlive--;

        // Only check for wave completion if spawning is finished and all enemies are defeated.
        if (!waveIsSpawning && enemiesAlive <= 0)
        {
            Debug.Log("Wave completed!");
            Wave_SO currentWave = waves[currentWaveIndex];
            currentWaveIndex++;
            Invoke(nameof(StartNextWave), currentWave.timeToNextWave);
        }
    }
}
