using UnityEngine;

/// <summary>
/// Manages trail renderer setup and configuration for all game objects
/// </summary>
public class TrailManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The trail renderer configuration asset")]
    [SerializeField] private TrailRendererConfig trailConfig;
    
    [Header("Default Materials")]
    [Tooltip("Default material for player ship trails")]
    [SerializeField] private Material playerShipTrailMaterial;
    [Tooltip("Default material for player projectile trails")]
    [SerializeField] private Material playerProjectileTrailMaterial;
    [Tooltip("Default material for enemy projectile trails")]
    [SerializeField] private Material enemyProjectileTrailMaterial;

    public static TrailManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets up a trail renderer for a specific object type
    /// </summary>
    /// <param name="gameObject">The game object to add trail to</param>
    /// <param name="objectType">The type of object (PlayerShip, PlayerProjectile, EnemyProjectile)</param>
    /// <returns>The configured TrailRendererController component</returns>
    public TrailRendererController SetupTrailForObject(GameObject gameObject, string objectType)
    {
        if (trailConfig == null)
        {
            Debug.LogWarning("TrailManager: No trail configuration assigned!");
            return null;
        }

        // Get or add TrailRenderer component
        TrailRenderer trailRenderer = gameObject.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        // Get or add TrailRendererController component
        TrailRendererController controller = gameObject.GetComponent<TrailRendererController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<TrailRendererController>();
        }

        // Get configuration for this object type
        var settings = trailConfig.GetTrailConfig(objectType);
        
        // Apply default material if none is set
        if (settings.trailMaterial == null)
        {
            settings.trailMaterial = GetDefaultMaterialForType(objectType);
        }

        // Update the controller with the settings
        controller.UpdateTrailSettings(settings);

        return controller;
    }

    /// <summary>
    /// Gets the default trail material for a specific object type
    /// </summary>
    /// <param name="objectType">The object type</param>
    /// <returns>The default material for that type</returns>
    private Material GetDefaultMaterialForType(string objectType)
    {
        switch (objectType)
        {
            case "PlayerShip":
                return playerShipTrailMaterial;
            case "PlayerProjectile":
                return playerProjectileTrailMaterial;
            case "EnemyProjectile":
                return enemyProjectileTrailMaterial;
            default:
                Debug.LogWarning($"No default material defined for object type: {objectType}");
                return null;
        }
    }

    /// <summary>
    /// Sets up trails for all projectiles in a pool
    /// </summary>
    /// <param name="poolTag">The pool tag to setup trails for</param>
    /// <param name="objectType">The object type for configuration</param>
    public void SetupTrailsForPool(string poolTag, string objectType)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning("TrailManager: ObjectPoolManager not found!");
            return;
        }

        // This would be called during pool initialization
        // The actual implementation would depend on ObjectPoolManager's API
        Debug.Log($"Setting up trails for pool: {poolTag} with type: {objectType}");
    }

    /// <summary>
    /// Cleans up trail for pooled objects
    /// </summary>
    /// <param name="gameObject">The game object being returned to pool</param>
    public void CleanupTrailForPooledObject(GameObject gameObject)
    {
        TrailRendererController controller = gameObject.GetComponent<TrailRendererController>();
        if (controller != null)
        {
            controller.OnObjectDisable();
        }
    }

    /// <summary>
    /// Initializes trail for pooled objects when spawned
    /// </summary>
    /// <param name="gameObject">The game object being spawned from pool</param>
    public void InitializeTrailForPooledObject(GameObject gameObject)
    {
        TrailRendererController controller = gameObject.GetComponent<TrailRendererController>();
        if (controller != null)
        {
            controller.OnObjectEnable();
        }
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField] private bool setupTrailsOnValidate = false;
    
    private void OnValidate()
    {
        if (setupTrailsOnValidate && Application.isPlaying)
        {
            // Setup trails for existing objects in the scene
            SetupExistingTrails();
        }
    }

    private void SetupExistingTrails()
    {
        // Find and setup trails for player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SetupTrailForObject(player, "PlayerShip");
        }

        // Find and setup trails for projectiles
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach (var projectile in projectiles)
        {
            string objectType = projectile.gameObject.CompareTag("PlayerProjectile") ? "PlayerProjectile" : "EnemyProjectile";
            SetupTrailForObject(projectile.gameObject, objectType);
        }
    }
#endif
}
