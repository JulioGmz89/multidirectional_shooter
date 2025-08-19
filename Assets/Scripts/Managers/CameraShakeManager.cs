using UnityEngine;

/// <summary>
/// Centralized manager for camera shake effects throughout the game.
/// Provides a singleton interface for triggering shakes from any script.
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Camera Shake Reference")]
    [Tooltip("Reference to the CameraShake component. Will auto-find if not assigned.")]
    [SerializeField] private CameraShake cameraShake;

    [Header("Shake Intensity Settings")]
    [Tooltip("Intensity multiplier for enemy death shakes.")]
    [Range(0f, 1f)]
    [SerializeField] private float enemyDeathIntensity = 0.15f;
    
    [Tooltip("Duration for enemy death shakes.")]
    [SerializeField] private float enemyDeathDuration = 0.2f;
    
    [Tooltip("Intensity multiplier for player death shakes.")]
    [Range(0f, 1f)]
    [SerializeField] private float playerDeathIntensity = 0.8f;
    
    [Tooltip("Duration for player death shakes.")]
    [SerializeField] private float playerDeathDuration = 1.5f;
    
    [Tooltip("Intensity multiplier for player damage shakes.")]
    [Range(0f, 1f)]
    [SerializeField] private float playerDamageIntensity = 0.25f;
    
    [Tooltip("Duration for player damage shakes.")]
    [SerializeField] private float playerDamageDuration = 0.3f;
    
    [Tooltip("Intensity multiplier for explosion shakes.")]
    [Range(0f, 1f)]
    [SerializeField] private float explosionIntensity = 0.5f;
    
    [Tooltip("Duration for explosion shakes.")]
    [SerializeField] private float explosionDuration = 0.8f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCameraShake();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCameraShake()
    {
        // Auto-find CameraShake component if not assigned
        if (cameraShake == null)
        {
            // First try to find it on the main camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraShake = mainCamera.GetComponent<CameraShake>();
            }
            
            // If not found, search in the scene
            if (cameraShake == null)
            {
                cameraShake = FindObjectOfType<CameraShake>();
            }
            
            // If still not found, log warning
            if (cameraShake == null)
            {
                Debug.LogWarning("CameraShakeManager: No CameraShake component found in the scene. Camera shake effects will not work.");
            }
        }
    }

    /// <summary>
    /// Triggers a camera shake with custom intensity and duration.
    /// </summary>
    /// <param name="intensity">Shake intensity (0-1 range)</param>
    /// <param name="duration">Shake duration in seconds</param>
    public void TriggerShake(float intensity, float duration)
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(intensity, duration);
        }
    }

    /// <summary>
    /// Triggers a shake effect when an enemy dies.
    /// </summary>
    public void TriggerEnemyDeathShake()
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(enemyDeathIntensity, enemyDeathDuration);
        }
    }

    /// <summary>
    /// Triggers a shake effect when the player dies.
    /// </summary>
    public void TriggerPlayerDeathShake()
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(playerDeathIntensity, playerDeathDuration);
        }
    }

    /// <summary>
    /// Triggers a shake effect when the player takes damage.
    /// </summary>
    public void TriggerPlayerDamageShake()
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(playerDamageIntensity, playerDamageDuration);
        }
    }

    /// <summary>
    /// Triggers a shake effect for explosions.
    /// </summary>
    public void TriggerExplosionShake()
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(explosionIntensity, explosionDuration);
        }
    }

    /// <summary>
    /// Stops any current shake effect.
    /// </summary>
    public void StopShake()
    {
        if (cameraShake != null)
        {
            cameraShake.StopShake();
        }
    }

    /// <summary>
    /// Updates the camera shake reference. Useful if the camera changes during gameplay.
    /// </summary>
    public void RefreshCameraShakeReference()
    {
        InitializeCameraShake();
    }

    private void OnValidate()
    {
        // Ensure all intensity values are within valid range
        enemyDeathIntensity = Mathf.Clamp01(enemyDeathIntensity);
        playerDeathIntensity = Mathf.Clamp01(playerDeathIntensity);
        playerDamageIntensity = Mathf.Clamp01(playerDamageIntensity);
        explosionIntensity = Mathf.Clamp01(explosionIntensity);
        
        // Ensure all duration values are positive
        enemyDeathDuration = Mathf.Max(0.1f, enemyDeathDuration);
        playerDeathDuration = Mathf.Max(0.1f, playerDeathDuration);
        playerDamageDuration = Mathf.Max(0.1f, playerDamageDuration);
        explosionDuration = Mathf.Max(0.1f, explosionDuration);
    }
}
