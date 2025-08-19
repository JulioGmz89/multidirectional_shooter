using UnityEngine;

/// <summary>
/// ScriptableObject that defines trail renderer configurations for different object types
/// </summary>
[CreateAssetMenu(fileName = "TrailRendererConfig", menuName = "Project Mayhem/Trail Renderer Config")]
public class TrailRendererConfig : ScriptableObject
{
    [System.Serializable]
    public class ObjectTrailConfig
    {
        [Header("Object Type")]
        public string objectType;
        
        [Header("Trail Settings")]
        public TrailRendererController.TrailSettings trailSettings;
        
        public ObjectTrailConfig(string type)
        {
            objectType = type;
            trailSettings = new TrailRendererController.TrailSettings();
        }
    }

    [Header("Default Trail Configurations")]
    [SerializeField] private ObjectTrailConfig[] trailConfigs = new ObjectTrailConfig[]
    {
        new ObjectTrailConfig("PlayerShip"),
        new ObjectTrailConfig("PlayerProjectile"),
        new ObjectTrailConfig("EnemyProjectile")
    };

    /// <summary>
    /// Gets the trail configuration for a specific object type
    /// </summary>
    /// <param name="objectType">The type of object to get configuration for</param>
    /// <returns>The trail configuration, or null if not found</returns>
    public TrailRendererController.TrailSettings GetTrailConfig(string objectType)
    {
        foreach (var config in trailConfigs)
        {
            if (config.objectType == objectType)
            {
                return config.trailSettings;
            }
        }
        
        Debug.LogWarning($"No trail configuration found for object type: {objectType}");
        return new TrailRendererController.TrailSettings();
    }

    /// <summary>
    /// Gets all available object types
    /// </summary>
    /// <returns>Array of object type names</returns>
    public string[] GetAvailableObjectTypes()
    {
        string[] types = new string[trailConfigs.Length];
        for (int i = 0; i < trailConfigs.Length; i++)
        {
            types[i] = trailConfigs[i].objectType;
        }
        return types;
    }

    private void OnValidate()
    {
        // Set up default configurations if they don't exist
        if (trailConfigs == null || trailConfigs.Length == 0)
        {
            trailConfigs = new ObjectTrailConfig[]
            {
                CreatePlayerShipConfig(),
                CreatePlayerProjectileConfig(),
                CreateEnemyProjectileConfig()
            };
        }
    }

    private ObjectTrailConfig CreatePlayerShipConfig()
    {
        var config = new ObjectTrailConfig("PlayerShip");
        config.trailSettings.startWidth = 0.3f;
        config.trailSettings.endWidth = 0f;
        config.trailSettings.time = 0.5f;
        config.trailSettings.minVelocityThreshold = 1f;
        config.trailSettings.enabledByDefault = true;
        config.trailSettings.sortingLayerName = "Default";
        config.trailSettings.orderInLayer = -1; // Behind the player ship
        
        // Blue to cyan gradient for player ship
        var gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f); // Light blue
        colorKeys[1] = new GradientColorKey(new Color(0f, 1f, 1f), 1f);     // Cyan
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.8f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        config.trailSettings.colorGradient = gradient;
        
        return config;
    }

    private ObjectTrailConfig CreatePlayerProjectileConfig()
    {
        var config = new ObjectTrailConfig("PlayerProjectile");
        config.trailSettings.startWidth = 0.15f;
        config.trailSettings.endWidth = 0f;
        config.trailSettings.time = 0.3f;
        config.trailSettings.minVelocityThreshold = 0.1f;
        config.trailSettings.enabledByDefault = true;
        config.trailSettings.sortingLayerName = "Default";
        config.trailSettings.orderInLayer = -2; // Behind projectiles
        
        // Bright white to blue gradient for player projectiles
        var gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(Color.white, 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.8f, 1f), 1f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        config.trailSettings.colorGradient = gradient;
        
        return config;
    }

    private ObjectTrailConfig CreateEnemyProjectileConfig()
    {
        var config = new ObjectTrailConfig("EnemyProjectile");
        config.trailSettings.startWidth = 0.12f;
        config.trailSettings.endWidth = 0f;
        config.trailSettings.time = 0.25f;
        config.trailSettings.minVelocityThreshold = 0.1f;
        config.trailSettings.enabledByDefault = true;
        config.trailSettings.sortingLayerName = "Default";
        config.trailSettings.orderInLayer = -2; // Behind projectiles
        
        // Red to orange gradient for enemy projectiles
        var gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.3f, 0.2f), 0f); // Red
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0f), 1f);   // Orange
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.9f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        config.trailSettings.colorGradient = gradient;
        
        return config;
    }
}
