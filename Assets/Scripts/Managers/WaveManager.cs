using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectMayhem.Audio;
using ProjectMayhem.Spawning;

/// <summary>
/// Manages the spawning of enemy waves using an IWaveProvider for wave data
/// and SpawnZoneManager for spawn locations.
/// </summary>
[DefaultExecutionOrder(-100)]
public class WaveManager : MonoBehaviour
{
    /// <summary>
    /// Event fired when the wave number changes. Parameter is the new wave number (1-indexed).
    /// </summary>
    public event System.Action<int> OnWaveChanged;

    /// <summary>
    /// Event fired when a wave is completed.
    /// </summary>
    public event System.Action<int> OnWaveCompleted;

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static WaveManager Instance { get; private set; }

    [Header("Wave Provider")]
    [Tooltip("The component that provides wave data. Must implement IWaveProvider.")]
    [SerializeField] private MonoBehaviour waveProviderComponent;

    // Legacy spawn system - kept for backwards compatibility but hidden from Inspector
    // The new system uses SpawnZoneManager exclusively
    private bool useSpawnZones = true;
    
    // [Header("Legacy Spawn Points (Fallback)")]
    // [Tooltip("Fallback spawn points if SpawnZoneManager is not available.")]
    // [SerializeField] private Transform[] legacySpawnPoints;
    private Transform[] legacySpawnPoints; // Hidden - use SpawnZoneManager instead

    // Wave provider interface
    private IWaveProvider waveProvider;

    // Current wave state
    private RuntimeWaveData currentWave;
    private int enemiesAlive;
    private bool waveIsSpawning;
    private Coroutine spawnCoroutine;

    /// <summary>
    /// Gets the current wave number (1-indexed).
    /// </summary>
    public int CurrentWaveNumber => waveProvider?.CurrentWaveIndex ?? 0;

    /// <summary>
    /// Gets the total number of waves, or -1 for infinite mode.
    /// </summary>
    public int TotalWaves => waveProvider?.TotalWaves ?? 0;

    /// <summary>
    /// Gets whether the current mode has finite waves.
    /// </summary>
    public bool IsFiniteMode => waveProvider?.IsFinite ?? true;

    /// <summary>
    /// Gets the number of enemies currently alive.
    /// </summary>
    public int EnemiesAlive => enemiesAlive;

    /// <summary>
    /// Gets whether a wave is currently being spawned.
    /// </summary>
    public bool IsSpawning => waveIsSpawning;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Get wave provider from assigned component
        InitializeWaveProvider();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleGameStateChange;
    }

    /// <summary>
    /// Initializes the wave provider from the assigned component.
    /// </summary>
    private void InitializeWaveProvider()
    {
        if (waveProviderComponent != null)
        {
            waveProvider = waveProviderComponent as IWaveProvider;
            if (waveProvider == null)
            {
                Debug.LogError($"WaveManager: Assigned component '{waveProviderComponent.GetType().Name}' does not implement IWaveProvider!", this);
            }
            else
            {
                Debug.Log($"WaveManager: Using wave provider '{waveProviderComponent.GetType().Name}'");
            }
        }
        else
        {
            // Try to find a wave provider on the same GameObject
            waveProvider = GetComponent<IWaveProvider>();
            if (waveProvider != null)
            {
                Debug.Log($"WaveManager: Found wave provider on same GameObject.");
            }
            else
            {
                Debug.LogError("WaveManager: No wave provider assigned or found!", this);
            }
        }
    }

    /// <summary>
    /// Sets the wave provider at runtime. Useful for switching between campaign and infinite mode.
    /// </summary>
    public void SetWaveProvider(IWaveProvider provider)
    {
        if (waveIsSpawning)
        {
            Debug.LogWarning("WaveManager: Cannot change provider while spawning. Stop current wave first.");
            return;
        }

        waveProvider = provider;
        waveProvider?.Reset();
        currentWave = null;
        enemiesAlive = 0;

        Debug.Log($"WaveManager: Wave provider changed to {provider?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Handles game state changes.
    /// </summary>
    private void HandleGameStateChange(GameState newState)
    {
        if (newState == GameState.Gameplay)
        {
            // Start the first wave when gameplay begins
            if (CurrentWaveNumber == 0)
            {
                StartNextWave();
            }
        }
        else if (newState == GameState.Pause)
        {
            // Waves continue to track state but spawning is paused via Time.timeScale
        }
        else if (newState == GameState.Defeat || newState == GameState.Victory)
        {
            // Stop any ongoing spawning
            StopSpawning();
        }
    }

    /// <summary>
    /// Starts the next wave.
    /// </summary>
    private void StartNextWave()
    {
        if (waveProvider == null)
        {
            Debug.LogError("WaveManager: No wave provider available!");
            return;
        }

        if (!waveProvider.HasMoreWaves())
        {
            // All waves completed - Victory!
            Debug.Log("WaveManager: All waves completed!");
            GameStateManager.Instance.ChangeState(GameState.Victory);
            return;
        }

        currentWave = waveProvider.GetNextWave();
        if (currentWave == null)
        {
            Debug.LogError("WaveManager: Wave provider returned null wave!");
            return;
        }

        Debug.Log($"WaveManager: Starting {currentWave}");
        OnWaveChanged?.Invoke(currentWave.waveNumber);

        // Sync wave number with PowerUpSpawner
        if (PowerUpSpawner.Instance != null)
        {
            PowerUpSpawner.Instance.CurrentWaveNumber = currentWave.waveNumber;
        }

        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(currentWave));
    }

    /// <summary>
    /// Coroutine that spawns all enemies in a wave.
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(RuntimeWaveData wave)
    {
        Debug.Log($"WaveManager: Spawning Wave {wave.waveNumber}");
        SFX.Play(AudioEvent.WaveStart);

        waveIsSpawning = true;
        enemiesAlive = wave.TotalEnemyCount;

        Debug.Log($"WaveManager: Wave {wave.waveNumber} has {enemiesAlive} enemies in {wave.enemies.Count} groups.");

        foreach (var group in wave.enemies)
        {
            // Initial delay for this group
            if (group.initialDelay > 0)
            {
                yield return new WaitForSeconds(group.initialDelay);
            }

            // Spawn each enemy in the group
            for (int i = 0; i < group.count; i++)
            {
                Vector2 spawnPosition = GetSpawnPosition();
                
                ObjectPoolManager.Instance.SpawnFromPool(group.poolTag, spawnPosition, Quaternion.identity);

                // Wait between spawns (except after the last one)
                if (i < group.count - 1)
                {
                    // Apply director spawn interval modifier if available
                    float interval = group.spawnInterval;
                    if (WaveDirector.Instance != null)
                    {
                        interval *= WaveDirector.Instance.GetSpawnIntervalMultiplier();
                    }
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        waveIsSpawning = false;
        Debug.Log($"WaveManager: Wave {wave.waveNumber} finished spawning.");

        // Check if wave was already cleared during spawning
        if (enemiesAlive <= 0)
        {
            Debug.Log("WaveManager: Wave cleared during spawn.");
            CompleteWave();
        }
    }

    /// <summary>
    /// Gets a spawn position using SpawnZoneManager or legacy spawn points.
    /// </summary>
    private Vector2 GetSpawnPosition()
    {
        // Try SpawnZoneManager first
        if (useSpawnZones && SpawnZoneManager.Instance != null)
        {
            return SpawnZoneManager.Instance.GetEnemySpawnPoint();
        }

        // Fallback to legacy spawn points
        if (legacySpawnPoints != null && legacySpawnPoints.Length > 0)
        {
            Transform spawnPoint = legacySpawnPoints[Random.Range(0, legacySpawnPoints.Length)];
            return spawnPoint.position;
        }

        // Last resort: random position
        Debug.LogWarning("WaveManager: No spawn zones or legacy spawn points available. Using random position.");
        return Random.insideUnitCircle * 10f;
    }

    /// <summary>
    /// Called by enemies when they are defeated.
    /// </summary>
    public void OnEnemyDefeated()
    {
        if (enemiesAlive > 0)
        {
            enemiesAlive--;
        }

        Debug.Log($"WaveManager: Enemy defeated. {enemiesAlive} remaining.");

        // Notify WaveDirector of the kill
        if (WaveDirector.Instance != null)
        {
            WaveDirector.Instance.OnEnemyKilled();
        }

        // Integrate with PowerUpSpawner for chance-based spawns
        if (PowerUpSpawner.Instance != null)
        {
            float baseChance = currentWave?.powerUpChanceOnKill ?? 0f;
            // Add director bonus if available
            float directorBonus = WaveDirector.Instance?.GetPowerUpChanceBonus() ?? 0f;
            PowerUpSpawner.Instance.OnEnemyKilled(baseChance + directorBonus);
        }

        // Check for wave completion
        if (!waveIsSpawning && enemiesAlive <= 0)
        {
            CompleteWave();
        }
    }

    /// <summary>
    /// Handles wave completion.
    /// </summary>
    private void CompleteWave()
    {
        Debug.Log($"WaveManager: Wave {currentWave?.waveNumber ?? 0} completed!");
        SFX.Play(AudioEvent.WaveComplete);

        int completedWaveNumber = currentWave?.waveNumber ?? 0;
        float timeToNextWave = currentWave?.timeToNextWave ?? 3f;

        OnWaveCompleted?.Invoke(completedWaveNumber);

        // Spawn power-up on wave complete if configured
        if (PowerUpSpawner.Instance != null)
        {
            bool guaranteed = currentWave?.spawnPowerUpOnComplete ?? false;
            PowerUpSpawner.Instance.OnWaveComplete(guaranteed);
        }

        // Schedule next wave
        Invoke(nameof(StartNextWave), timeToNextWave);
    }

    /// <summary>
    /// Stops the current spawning coroutine.
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        waveIsSpawning = false;
        CancelInvoke(nameof(StartNextWave));
    }

    /// <summary>
    /// Resets the wave manager to start from wave 1.
    /// </summary>
    public void ResetWaves()
    {
        StopSpawning();
        waveProvider?.Reset();
        currentWave = null;
        enemiesAlive = 0;
        Debug.Log("WaveManager: Reset to wave 1.");
    }

    /// <summary>
    /// Skips to a specific wave number (for debugging).
    /// </summary>
    public void SkipToWave(int waveNumber)
    {
        if (waveProvider == null || !waveProvider.IsFinite)
        {
            Debug.LogWarning("WaveManager: Cannot skip waves with current provider.");
            return;
        }

        StopSpawning();
        waveProvider.Reset();

        // Advance to the desired wave
        for (int i = 1; i < waveNumber && waveProvider.HasMoreWaves(); i++)
        {
            waveProvider.GetNextWave();
        }

        Debug.Log($"WaveManager: Skipped to wave {waveNumber}.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validate that assigned component implements IWaveProvider
        if (waveProviderComponent != null && !(waveProviderComponent is IWaveProvider))
        {
            Debug.LogWarning($"WaveManager: Assigned component '{waveProviderComponent.GetType().Name}' does not implement IWaveProvider!", this);
        }
    }
#endif
}
