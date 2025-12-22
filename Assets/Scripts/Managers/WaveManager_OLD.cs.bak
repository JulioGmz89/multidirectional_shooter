using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectMayhem.Audio;

/// <summary>
/// Manages the spawning of enemy waves based on ScriptableObject definitions.
/// </summary>
[DefaultExecutionOrder(-100)]
public class WaveManager : MonoBehaviour
{
    public event System.Action<int> OnWaveChanged;
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
        Debug.Log("Attempting to start next wave.");
        if (currentWaveIndex < waves.Count)
        {
            Debug.Log($"Starting wave {currentWaveIndex + 1} of {waves.Count}.");
            OnWaveChanged?.Invoke(currentWaveIndex + 1);
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }
        else
        {
            // All waves have been successfully cleared.
            Debug.LogWarning("All waves completed!");
            GameStateManager.Instance.ChangeState(GameState.Victory);
        }
    }

    private IEnumerator SpawnWave(Wave_SO wave)
    {
        Debug.Log($"Starting Wave {currentWaveIndex + 1}");
        SFX.Play(AudioEvent.WaveStart);
        waveIsSpawning = true;
        enemiesAlive = 0;
        foreach (var group in wave.enemyGroups)
        {
            enemiesAlive += group.count;
        }
        Debug.Log($"Wave {currentWaveIndex + 1} has {enemiesAlive} enemies.");

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
        Debug.Log($"Wave {currentWaveIndex + 1} has finished spawning.");

        // If all enemies were defeated while the wave was still spawning, trigger completion check.
        if (enemiesAlive <= 0)
        {
            Debug.Log("Wave cleared during spawn. Triggering completion.");
            // Manually call OnEnemyDefeated to trigger the next wave logic.
            // It's safe because enemiesAlive is already <= 0.
            OnEnemyDefeated();
        }
    }

    /// <summary>
    /// Called by enemies when they are defeated.
    /// </summary>
    public void OnEnemyDefeated()
    {
        // This check prevents the count from going negative if called manually after wave clear.
        if (enemiesAlive > 0)
        {
            enemiesAlive--;
        }

        Debug.Log($"Enemy defeated. {enemiesAlive} enemies remaining.");
        // Only check for wave completion if spawning is finished and all enemies are defeated.
        if (!waveIsSpawning && enemiesAlive <= 0)
        {
            Debug.Log("Wave completed!");
            SFX.Play(AudioEvent.WaveComplete);
            Wave_SO currentWave = waves[currentWaveIndex];
            Debug.Log("Wave complete. Preparing for next wave.");
            currentWaveIndex++;
            Invoke(nameof(StartNextWave), currentWave.timeToNextWave);
        }
    }
}
