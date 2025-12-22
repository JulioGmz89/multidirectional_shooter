using System.Collections.Generic;
using UnityEngine;
using ProjectMayhem.Audio;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Monitors game state and makes dynamic decisions about spawning and difficulty.
    /// Acts as a "game master" that adjusts the experience based on player performance.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance for easy access.
        /// </summary>
        public static WaveDirector Instance { get; private set; }

        /// <summary>
        /// The current intensity phase of the game.
        /// </summary>
        public enum IntensityPhase
        {
            BuildUp,    // Ramping up intensity
            Peak,       // Maximum intensity
            Sustain,    // Maintaining current intensity
            Relax       // Breather/rest period
        }

        [Header("References")]
        [Tooltip("Reference to the player's Health component. Auto-found if not set.")]
        [SerializeField] private Health playerHealth;

        [Header("Intensity Settings")]
        [Tooltip("Duration of peak intensity phase in seconds.")]
        [SerializeField] private float peakDuration = 30f;

        [Tooltip("Duration of relax/breather phase in seconds.")]
        [SerializeField] private float relaxDuration = 8f;

        [Tooltip("Minimum time between breathers in seconds.")]
        [SerializeField] private float minTimeBetweenBreathers = 45f;

        [Header("Kill Tracking")]
        [Tooltip("Time window to track recent kills.")]
        [SerializeField] private float killTrackingWindow = 10f;

        [Tooltip("Number of rapid kills to trigger a breather.")]
        [SerializeField] private int rapidKillThreshold = 10;

        [Tooltip("Time without kills before reducing intensity.")]
        [SerializeField] private float killDroughtThreshold = 15f;

        [Header("Health Thresholds")]
        [Tooltip("Health percentage below which player is considered 'low health'.")]
        [Range(0f, 1f)]
        [SerializeField] private float lowHealthThreshold = 0.3f;

        [Tooltip("Health percentage below which player is considered 'critical'.")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalHealthThreshold = 0.15f;

        [Header("Difficulty Modifiers")]
        [Tooltip("Difficulty multiplier when player is doing well.")]
        [SerializeField] private float highPerformanceMultiplier = 1.2f;

        [Tooltip("Difficulty multiplier when player is struggling.")]
        [SerializeField] private float lowPerformanceMultiplier = 0.7f;

        [Tooltip("Power-up chance bonus when player is at low health.")]
        [Range(0f, 1f)]
        [SerializeField] private float lowHealthPowerUpBonus = 0.2f;

        [Tooltip("Power-up chance bonus when player is at critical health.")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalHealthPowerUpBonus = 0.4f;

        [Header("Wave Duration Settings")]
        [Tooltip("Expected average wave duration in seconds.")]
        [SerializeField] private float expectedWaveDuration = 60f;

        [Tooltip("Multiplier applied when wave exceeds expected duration.")]
        [SerializeField] private float waveTooLongHelpMultiplier = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Runtime state
        private IntensityPhase currentPhase = IntensityPhase.BuildUp;
        private float phaseTimer = 0f;
        private float timeSinceLastKill = 0f;
        private float timeSinceLastDamage = 0f;
        private float lastBreatherTime = -999f;
        private float waveStartTime = 0f;
        private float currentIntensity = 0.5f;
        private int currentWaveNumber = 1;

        // Kill tracking
        private Queue<float> recentKillTimes = new Queue<float>();
        private int totalKillsThisWave = 0;

        // Cached values
        private float playerHealthPercent = 1f;
        private bool isPlayerLowHealth = false;
        private bool isPlayerCriticalHealth = false;

        // Events
        /// <summary>
        /// Fired when a breather period starts. Parameter is the duration.
        /// </summary>
        public event System.Action<float> OnBreatherStart;

        /// <summary>
        /// Fired when a breather period ends.
        /// </summary>
        public event System.Action OnBreatherEnd;

        /// <summary>
        /// Fired when the intensity phase changes.
        /// </summary>
        public event System.Action<IntensityPhase> OnPhaseChanged;

        /// <summary>
        /// Fired when player enters low health state.
        /// </summary>
        public event System.Action OnPlayerLowHealth;

        /// <summary>
        /// Fired when player recovers from low health state.
        /// </summary>
        public event System.Action OnPlayerRecovered;

        #region Properties

        /// <summary>
        /// Gets the current intensity phase.
        /// </summary>
        public IntensityPhase CurrentPhase => currentPhase;

        /// <summary>
        /// Gets the current intensity value (0-1).
        /// </summary>
        public float CurrentIntensity => currentIntensity;

        /// <summary>
        /// Gets the time since the last enemy kill.
        /// </summary>
        public float TimeSinceLastKill => timeSinceLastKill;

        /// <summary>
        /// Gets the time since the player last took damage.
        /// </summary>
        public float TimeSinceLastDamage => timeSinceLastDamage;

        /// <summary>
        /// Gets whether the player is at low health.
        /// </summary>
        public bool IsPlayerLowHealth => isPlayerLowHealth;

        /// <summary>
        /// Gets whether the player is at critical health.
        /// </summary>
        public bool IsPlayerCriticalHealth => isPlayerCriticalHealth;

        /// <summary>
        /// Gets the player's current health percentage (0-1).
        /// </summary>
        public float PlayerHealthPercent => playerHealthPercent;

        /// <summary>
        /// Gets the number of recent kills within the tracking window.
        /// </summary>
        public int RecentKillCount => recentKillTimes.Count;

        /// <summary>
        /// Gets whether the current wave is taking longer than expected.
        /// </summary>
        public bool IsWaveTakingTooLong => Time.time - waveStartTime > expectedWaveDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("WaveDirector: Duplicate instance found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-find player health if not assigned
            if (playerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<Health>();
                }
            }

            // Subscribe to events
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
            }

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveChanged += HandleWaveChanged;
                WaveManager.Instance.OnWaveCompleted += HandleWaveCompleted;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Unsubscribe from events
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveChanged -= HandleWaveChanged;
                WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
            }
        }

        private void Update()
        {
            // Update timers
            timeSinceLastKill += Time.deltaTime;
            timeSinceLastDamage += Time.deltaTime;
            phaseTimer += Time.deltaTime;

            // Clean up old kill times
            CleanupKillTimes();

            // Update intensity phase
            UpdateIntensityPhase();

            // Update intensity value
            UpdateIntensityValue();
        }

        #endregion

        #region Public Query Methods

        /// <summary>
        /// Gets the current difficulty multiplier based on game state.
        /// </summary>
        /// <returns>A multiplier to apply to difficulty (1.0 = normal).</returns>
        public float GetDifficultyMultiplier()
        {
            float multiplier = 1f;

            // Reduce difficulty if player is struggling
            if (isPlayerCriticalHealth)
            {
                multiplier *= lowPerformanceMultiplier * 0.8f;
            }
            else if (isPlayerLowHealth)
            {
                multiplier *= lowPerformanceMultiplier;
            }
            // Increase difficulty if player is doing very well
            else if (recentKillTimes.Count >= rapidKillThreshold / 2 && playerHealthPercent > 0.7f)
            {
                multiplier *= highPerformanceMultiplier;
            }

            // Reduce difficulty during relax phase
            if (currentPhase == IntensityPhase.Relax)
            {
                multiplier *= 0.6f;
            }

            // Reduce if wave is taking too long
            if (IsWaveTakingTooLong)
            {
                multiplier *= 0.8f;
            }

            return Mathf.Clamp(multiplier, 0.5f, 1.5f);
        }

        /// <summary>
        /// Gets the power-up spawn chance bonus based on game state.
        /// </summary>
        /// <returns>A bonus to add to power-up spawn chances (0-1).</returns>
        public float GetPowerUpChanceBonus()
        {
            float bonus = 0f;

            // Health-based bonuses
            if (isPlayerCriticalHealth)
            {
                bonus += criticalHealthPowerUpBonus;
            }
            else if (isPlayerLowHealth)
            {
                bonus += lowHealthPowerUpBonus;
            }

            // Bonus if wave is taking too long
            if (IsWaveTakingTooLong)
            {
                bonus += lowHealthPowerUpBonus * waveTooLongHelpMultiplier;
            }

            // Bonus during relax phase
            if (currentPhase == IntensityPhase.Relax)
            {
                bonus += 0.1f;
            }

            // Kill drought bonus
            if (timeSinceLastKill > killDroughtThreshold)
            {
                bonus += 0.1f;
            }

            return Mathf.Clamp01(bonus);
        }

        /// <summary>
        /// Determines if a breather/rest period should be triggered.
        /// </summary>
        public bool ShouldTriggerBreather()
        {
            // Don't trigger breathers too frequently
            if (Time.time - lastBreatherTime < minTimeBetweenBreathers)
            {
                return false;
            }

            // Trigger if player just had a kill streak
            if (recentKillTimes.Count >= rapidKillThreshold)
            {
                return true;
            }

            // Trigger if peak phase has lasted long enough
            if (currentPhase == IntensityPhase.Peak && phaseTimer >= peakDuration)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a help power-up should be spawned.
        /// </summary>
        public bool ShouldSpawnHelpPowerUp()
        {
            // Always help at critical health (cooldown managed by PowerUpSpawner)
            if (isPlayerCriticalHealth)
            {
                return true;
            }

            // Help if wave is taking way too long
            if (Time.time - waveStartTime > expectedWaveDuration * 1.5f)
            {
                return true;
            }

            // Help if there's been a kill drought at low health
            if (isPlayerLowHealth && timeSinceLastKill > killDroughtThreshold)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the recommended spawn interval multiplier.
        /// </summary>
        public float GetSpawnIntervalMultiplier()
        {
            float multiplier = 1f;

            // Slow down spawns during relax phase
            if (currentPhase == IntensityPhase.Relax)
            {
                multiplier = 1.5f;
            }
            // Slow down if player is struggling
            else if (isPlayerLowHealth)
            {
                multiplier = 1.25f;
            }
            // Speed up if player is doing well
            else if (currentPhase == IntensityPhase.Peak && playerHealthPercent > 0.7f)
            {
                multiplier = 0.85f;
            }

            return multiplier;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when an enemy is killed. Updates kill tracking.
        /// </summary>
        public void OnEnemyKilled()
        {
            timeSinceLastKill = 0f;
            recentKillTimes.Enqueue(Time.time);
            totalKillsThisWave++;

            if (debugMode)
            {
                Debug.Log($"WaveDirector: Enemy killed. Recent kills: {recentKillTimes.Count}, Total this wave: {totalKillsThisWave}");
            }

            // Check for breather trigger
            if (ShouldTriggerBreather())
            {
                TriggerBreather();
            }
        }

        /// <summary>
        /// Called when the player takes damage.
        /// </summary>
        public void OnPlayerDamaged()
        {
            timeSinceLastDamage = 0f;

            if (debugMode)
            {
                Debug.Log($"WaveDirector: Player damaged. Health: {playerHealthPercent:P0}");
            }
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            float previousPercent = playerHealthPercent;
            playerHealthPercent = max > 0 ? (float)current / max : 0f;

            bool wasLowHealth = isPlayerLowHealth;
            bool wasCriticalHealth = isPlayerCriticalHealth;

            isPlayerLowHealth = playerHealthPercent <= lowHealthThreshold;
            isPlayerCriticalHealth = playerHealthPercent <= criticalHealthThreshold;

            // Track damage events
            if (playerHealthPercent < previousPercent)
            {
                OnPlayerDamaged();
            }

            // Fire events for state changes
            if (isPlayerLowHealth && !wasLowHealth)
            {
                OnPlayerLowHealth?.Invoke();
                if (debugMode)
                {
                    Debug.Log("WaveDirector: Player entered low health state.");
                }
            }
            else if (!isPlayerLowHealth && wasLowHealth)
            {
                OnPlayerRecovered?.Invoke();
                if (debugMode)
                {
                    Debug.Log("WaveDirector: Player recovered from low health.");
                }
            }

            // Spawn help power-up if needed
            if (ShouldSpawnHelpPowerUp() && PowerUpSpawner.Instance != null)
            {
                if (PowerUpSpawner.Instance.CanSpawn)
                {
                    PowerUpSpawner.Instance.SpawnRandomPowerUp(PowerUpSpawner.SpawnTrigger.Manual);
                    if (debugMode)
                    {
                        Debug.Log("WaveDirector: Spawned help power-up for struggling player.");
                    }
                }
            }
        }

        private void HandleWaveChanged(int waveNumber)
        {
            currentWaveNumber = waveNumber;
            waveStartTime = Time.time;
            totalKillsThisWave = 0;
            recentKillTimes.Clear();

            // Reset to build-up phase at wave start
            SetPhase(IntensityPhase.BuildUp);

            if (debugMode)
            {
                Debug.Log($"WaveDirector: Wave {waveNumber} started.");
            }
        }

        private void HandleWaveCompleted(int waveNumber)
        {
            if (debugMode)
            {
                float waveDuration = Time.time - waveStartTime;
                Debug.Log($"WaveDirector: Wave {waveNumber} completed in {waveDuration:F1}s with {totalKillsThisWave} kills.");
            }
        }

        #endregion

        #region Private Methods

        private void CleanupKillTimes()
        {
            float cutoffTime = Time.time - killTrackingWindow;
            while (recentKillTimes.Count > 0 && recentKillTimes.Peek() < cutoffTime)
            {
                recentKillTimes.Dequeue();
            }
        }

        private void UpdateIntensityPhase()
        {
            switch (currentPhase)
            {
                case IntensityPhase.BuildUp:
                    // Transition to peak after build-up or if kills are high
                    if (phaseTimer >= 15f || recentKillTimes.Count >= rapidKillThreshold / 2)
                    {
                        SetPhase(IntensityPhase.Peak);
                    }
                    break;

                case IntensityPhase.Peak:
                    // Let breather check handle transition to relax
                    if (phaseTimer >= peakDuration && !ShouldTriggerBreather())
                    {
                        SetPhase(IntensityPhase.Sustain);
                    }
                    break;

                case IntensityPhase.Sustain:
                    // Can transition back to peak or to relax
                    if (ShouldTriggerBreather())
                    {
                        TriggerBreather();
                    }
                    else if (recentKillTimes.Count >= rapidKillThreshold / 2)
                    {
                        SetPhase(IntensityPhase.Peak);
                    }
                    break;

                case IntensityPhase.Relax:
                    // End relax phase after duration
                    if (phaseTimer >= relaxDuration)
                    {
                        EndBreather();
                    }
                    break;
            }
        }

        private void UpdateIntensityValue()
        {
            float targetIntensity = currentPhase switch
            {
                IntensityPhase.BuildUp => 0.3f + (phaseTimer / 15f) * 0.4f,
                IntensityPhase.Peak => 1f,
                IntensityPhase.Sustain => 0.7f,
                IntensityPhase.Relax => 0.2f,
                _ => 0.5f
            };

            // Smooth transition
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 2f);
        }

        private void SetPhase(IntensityPhase newPhase)
        {
            if (currentPhase == newPhase) return;

            currentPhase = newPhase;
            phaseTimer = 0f;
            OnPhaseChanged?.Invoke(newPhase);

            if (debugMode)
            {
                Debug.Log($"WaveDirector: Phase changed to {newPhase}");
            }
        }

        private void TriggerBreather()
        {
            SetPhase(IntensityPhase.Relax);
            lastBreatherTime = Time.time;
            OnBreatherStart?.Invoke(relaxDuration);

            if (debugMode)
            {
                Debug.Log($"WaveDirector: Breather triggered for {relaxDuration}s");
            }

            // Play audio cue
            SFX.Play(AudioEvent.WaveComplete);
        }

        private void EndBreather()
        {
            SetPhase(IntensityPhase.BuildUp);
            OnBreatherEnd?.Invoke();

            if (debugMode)
            {
                Debug.Log("WaveDirector: Breather ended");
            }
        }

        /// <summary>
        /// Resets the director state (call when restarting level).
        /// </summary>
        public void Reset()
        {
            currentPhase = IntensityPhase.BuildUp;
            phaseTimer = 0f;
            timeSinceLastKill = 0f;
            timeSinceLastDamage = 0f;
            lastBreatherTime = -999f;
            waveStartTime = Time.time;
            currentIntensity = 0.5f;
            currentWaveNumber = 1;
            totalKillsThisWave = 0;
            recentKillTimes.Clear();
            playerHealthPercent = 1f;
            isPlayerLowHealth = false;
            isPlayerCriticalHealth = false;
        }

        #endregion
    }
}
