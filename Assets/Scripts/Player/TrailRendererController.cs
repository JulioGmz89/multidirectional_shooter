using UnityEngine;

/// <summary>
/// Manages Trail Renderer components for game objects, providing configurable trail effects
/// for different object types (player ship, projectiles, etc.)
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class TrailRendererController : MonoBehaviour
{
    [System.Serializable]
    public class TrailSettings
    {
        [Header("Trail Appearance")]
        [Tooltip("The width of the trail at its start")]
        public float startWidth = 0.5f;
        [Tooltip("The width of the trail at its end")]
        public float endWidth = 0f;
        [Tooltip("How long the trail lasts in seconds")]
        public float time = 1f;
        [Tooltip("The color gradient of the trail")]
        public Gradient colorGradient = new Gradient();
        [Tooltip("The material used for the trail")]
        public Material trailMaterial;
        
        [Header("Trail Behavior")]
        [Tooltip("Whether the trail should be enabled by default")]
        public bool enabledByDefault = true;
        [Tooltip("Minimum velocity required to show trail")]
        public float minVelocityThreshold = 0.1f;
        
        [Header("Rendering Order")]
        [Tooltip("Sorting layer name for the trail (e.g., 'Background', 'Default', 'Foreground')")]
        public string sortingLayerName = "Default";
        [Tooltip("Order in layer - lower values render behind higher values")]
        public int orderInLayer = -1;
        
        public TrailSettings()
        {
            // Set up default gradient (white to transparent)
            colorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.white, 0f);
            colorKeys[1] = new GradientColorKey(Color.white, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);
            
            colorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    [Header("Trail Configuration")]
    [SerializeField] private TrailSettings trailSettings = new TrailSettings();
    
    [Header("Performance")]
    [Tooltip("Whether to automatically disable trail when velocity is too low")]
    [SerializeField] private bool useVelocityThreshold = true;
    [Tooltip("How often to check velocity (in seconds)")]
    [SerializeField] private float velocityCheckInterval = 0.1f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for trail events")]
    [SerializeField] private bool enableDebugLogging = false;
    [Tooltip("Show trail status in scene view")]
    [SerializeField] private bool showDebugInfo = false;

    private TrailRenderer trailRenderer;
    private Rigidbody2D rb;
    private float lastVelocityCheck;
    private bool isTrailActive;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        ApplyTrailSettings();
    }

    private void Start()
    {
        isTrailActive = trailSettings.enabledByDefault;
        trailRenderer.enabled = isTrailActive;
    }

    private void Update()
    {
        if (useVelocityThreshold && rb != null && Time.time >= lastVelocityCheck + velocityCheckInterval)
        {
            UpdateTrailBasedOnVelocity();
            lastVelocityCheck = Time.time;
        }
    }

    /// <summary>
    /// Applies the current trail settings to the TrailRenderer component
    /// </summary>
    private void ApplyTrailSettings()
    {
        if (trailRenderer == null) return;

        trailRenderer.startWidth = trailSettings.startWidth;
        trailRenderer.endWidth = trailSettings.endWidth;
        trailRenderer.time = trailSettings.time;
        trailRenderer.colorGradient = trailSettings.colorGradient;
        
        // Set sorting layer and order for 2D rendering control
        trailRenderer.sortingLayerName = trailSettings.sortingLayerName;
        trailRenderer.sortingOrder = trailSettings.orderInLayer;
        
        if (trailSettings.trailMaterial != null)
        {
            trailRenderer.material = trailSettings.trailMaterial;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"[TrailRenderer] Applied settings to {gameObject.name}: Layer={trailSettings.sortingLayerName}, Order={trailSettings.orderInLayer}, Width={trailSettings.startWidth}->{trailSettings.endWidth}");
        }
    }

    /// <summary>
    /// Updates trail visibility based on object velocity
    /// </summary>
    private void UpdateTrailBasedOnVelocity()
    {
        if (rb == null) return;

        float currentVelocity = rb.linearVelocity.magnitude;
        bool shouldShowTrail = currentVelocity >= trailSettings.minVelocityThreshold;
        
        if (shouldShowTrail != isTrailActive)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[TrailRenderer] {gameObject.name} velocity: {currentVelocity:F2}, threshold: {trailSettings.minVelocityThreshold:F2}, trail: {(shouldShowTrail ? "ON" : "OFF")}");
            }
            SetTrailEnabled(shouldShowTrail);
        }
    }

    /// <summary>
    /// Enables or disables the trail effect
    /// </summary>
    /// <param name="enabled">Whether to enable the trail</param>
    public void SetTrailEnabled(bool enabled)
    {
        isTrailActive = enabled;
        if (trailRenderer != null)
        {
            trailRenderer.enabled = enabled;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TrailRenderer] {gameObject.name} trail {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }
    }

    /// <summary>
    /// Clears the current trail immediately
    /// </summary>
    public void ClearTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TrailRenderer] {gameObject.name} trail CLEARED");
            }
        }
    }

    /// <summary>
    /// Updates the trail settings at runtime
    /// </summary>
    /// <param name="newSettings">The new trail settings to apply</param>
    public void UpdateTrailSettings(TrailSettings newSettings)
    {
        trailSettings = newSettings;
        ApplyTrailSettings();
    }

    /// <summary>
    /// Called when the object is returned to the pool or disabled
    /// </summary>
    public void OnObjectDisable()
    {
        ClearTrail();
        SetTrailEnabled(false);
    }

    /// <summary>
    /// Called when the object is spawned from the pool or enabled
    /// </summary>
    public void OnObjectEnable()
    {
        SetTrailEnabled(trailSettings.enabledByDefault);
    }

    /// <summary>
    /// Sets the trail color gradient
    /// </summary>
    /// <param name="gradient">The new color gradient</param>
    public void SetTrailColor(Gradient gradient)
    {
        trailSettings.colorGradient = gradient;
        if (trailRenderer != null)
        {
            trailRenderer.colorGradient = gradient;
        }
    }

    /// <summary>
    /// Sets the trail width
    /// </summary>
    /// <param name="startWidth">Width at the start of the trail</param>
    /// <param name="endWidth">Width at the end of the trail</param>
    public void SetTrailWidth(float startWidth, float endWidth)
    {
        trailSettings.startWidth = startWidth;
        trailSettings.endWidth = endWidth;
        
        if (trailRenderer != null)
        {
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;
        }
    }

    /// <summary>
    /// Sets the trail lifetime
    /// </summary>
    /// <param name="time">How long the trail should last in seconds</param>
    public void SetTrailTime(float time)
    {
        trailSettings.time = time;
        if (trailRenderer != null)
        {
            trailRenderer.time = time;
        }
    }

    /// <summary>
    /// Gets current trail status for debugging
    /// </summary>
    /// <returns>Debug information about the trail</returns>
    public string GetTrailDebugInfo()
    {
        if (trailRenderer == null) return "TrailRenderer: NULL";
        
        float velocity = rb != null ? rb.linearVelocity.magnitude : 0f;
        return $"Trail: {(isTrailActive ? "ON" : "OFF")} | Velocity: {velocity:F2} | Threshold: {trailSettings.minVelocityThreshold:F2} | Layer: {trailRenderer.sortingLayerName}({trailRenderer.sortingOrder})";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
        
        if (trailRenderer != null && Application.isPlaying)
        {
            ApplyTrailSettings();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Draw velocity vector
        if (rb != null)
        {
            UnityEditor.Handles.color = Color.yellow;
            Vector3 velocityEnd = transform.position + (Vector3)rb.linearVelocity;
            UnityEditor.Handles.DrawLine(transform.position, velocityEnd);
            UnityEditor.Handles.Label(velocityEnd, $"Vel: {rb.linearVelocity.magnitude:F2}");
        }
        
        // Draw trail info
        if (trailRenderer != null)
        {
            UnityEditor.Handles.color = isTrailActive ? Color.green : Color.red;
            Vector3 labelPos = transform.position + Vector3.up * 1f;
            UnityEditor.Handles.Label(labelPos, GetTrailDebugInfo());
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw trail threshold circle
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.2f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, trailSettings.minVelocityThreshold);
    }
#endif
}
