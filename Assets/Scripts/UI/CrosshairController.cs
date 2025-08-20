using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the crosshair UI element, making it follow the mouse cursor with screen boundary clamping.
/// Manages system cursor visibility based on game state.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CrosshairController : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Tooltip("Enable crosshair tracking (can be toggled at runtime)")]
    [SerializeField] private bool enableTracking = true;
    
    [Tooltip("Offset from screen edges to prevent crosshair from going completely off-screen")]
    [SerializeField] private float screenEdgeOffset = 10f;
    
    [Tooltip("Hide the system mouse cursor during gameplay")]
    [SerializeField] private bool hideSystemCursor = true;

    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    [SerializeField] private bool showDebugInfo = false;

    // Cached components
    private RectTransform crosshairRect;
    private Canvas parentCanvas;
    private Camera uiCamera;
    
    // Screen boundary cache
    private Vector2 screenBounds;
    private Vector2 crosshairHalfSize;
    private bool boundsInitialized = false;
    
    // Mouse tracking
    private Vector2 mouseScreenPosition;
    private Vector2 clampedCanvasPosition;
    
    // State management
    private bool wasSystemCursorVisible;
    private bool isInitialized = false;

    private void Awake()
    {
        // Cache components
        crosshairRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        if (parentCanvas == null)
        {
            Debug.LogError("CrosshairController: No Canvas found in parent hierarchy!", this);
            enabled = false;
            return;
        }
        
        // Get UI camera (null for Screen Space - Overlay)
        uiCamera = parentCanvas.worldCamera;
        
        // Store original cursor state
        wasSystemCursorVisible = Cursor.visible;
    }

    private void Start()
    {
        InitializeBounds();
        
        if (hideSystemCursor)
        {
            SetSystemCursorVisibility(false);
        }
        
        isInitialized = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"CrosshairController: Initialized with canvas render mode: {parentCanvas.renderMode}", this);
        }
    }

    private void Update()
    {
        if (!enableTracking || !isInitialized) return;

        // Only update during gameplay
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            return;
        }

        UpdateCrosshairPosition();
    }

    private void OnEnable()
    {
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.OnStateChanged += HandleGameStateChange;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.OnStateChanged -= HandleGameStateChange;
        }
    }

    private void OnDestroy()
    {
        // Restore original cursor state when destroyed
        if (hideSystemCursor)
        {
            SetSystemCursorVisibility(wasSystemCursorVisible);
        }
    }

    private void InitializeBounds()
    {
        // Calculate screen bounds based on canvas settings
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenBounds = new Vector2(Screen.width, Screen.height);
        }
        else if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCamera != null)
        {
            screenBounds = new Vector2(Screen.width, Screen.height);
        }
        else
        {
            // Fallback for other render modes
            screenBounds = new Vector2(Screen.width, Screen.height);
        }
        
        // Calculate crosshair half size for clamping
        crosshairHalfSize = crosshairRect.sizeDelta * 0.5f;
        boundsInitialized = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"CrosshairController: Screen bounds: {screenBounds}, Crosshair size: {crosshairRect.sizeDelta}", this);
        }
    }

    private void UpdateCrosshairPosition()
    {
        // Get mouse position using Input System
        if (Mouse.current == null) return;
        
        mouseScreenPosition = Mouse.current.position.ReadValue();
        
        // Convert screen position to canvas position
        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, 
            mouseScreenPosition, 
            uiCamera, 
            out canvasPosition))
        {
            // Apply screen boundary clamping
            clampedCanvasPosition = ClampToScreenBounds(canvasPosition);
            
            // Update crosshair position
            crosshairRect.localPosition = clampedCanvasPosition;
        }
    }

    private Vector2 ClampToScreenBounds(Vector2 canvasPosition)
    {
        if (!boundsInitialized) return canvasPosition;
        
        // Get canvas rect for proper clamping
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Rect canvasBounds = canvasRect.rect;
        
        // Calculate clamping bounds with offset
        float minX = canvasBounds.xMin + crosshairHalfSize.x + screenEdgeOffset;
        float maxX = canvasBounds.xMax - crosshairHalfSize.x - screenEdgeOffset;
        float minY = canvasBounds.yMin + crosshairHalfSize.y + screenEdgeOffset;
        float maxY = canvasBounds.yMax - crosshairHalfSize.y - screenEdgeOffset;
        
        // Clamp position
        float clampedX = Mathf.Clamp(canvasPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(canvasPosition.y, minY, maxY);
        
        return new Vector2(clampedX, clampedY);
    }

    private void HandleGameStateChange(GameState newState)
    {
        if (!hideSystemCursor) return;
        
        switch (newState)
        {
            case GameState.Gameplay:
                SetSystemCursorVisibility(false);
                break;
            case GameState.Pause:
            case GameState.Defeat:
            case GameState.MainMenu:
            case GameState.Victory:
                SetSystemCursorVisibility(true);
                break;
        }
    }

    private void SetSystemCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        
        if (showDebugInfo)
        {
            Debug.Log($"CrosshairController: System cursor visibility set to: {visible}", this);
        }
    }

    // Public methods for runtime control
    
    /// <summary>
    /// Enable or disable crosshair tracking at runtime.
    /// </summary>
    public void SetTrackingEnabled(bool enabled)
    {
        enableTracking = enabled;
        
        if (showDebugInfo)
        {
            Debug.Log($"CrosshairController: Tracking enabled: {enabled}", this);
        }
    }

    /// <summary>
    /// Get the current crosshair position in canvas coordinates.
    /// </summary>
    public Vector2 GetCrosshairCanvasPosition()
    {
        return crosshairRect.localPosition;
    }

    /// <summary>
    /// Get the current crosshair position in screen coordinates.
    /// </summary>
    public Vector2 GetCrosshairScreenPosition()
    {
        return mouseScreenPosition;
    }

    /// <summary>
    /// Manually set the crosshair position (useful for gamepad input).
    /// </summary>
    public void SetCrosshairPosition(Vector2 screenPosition)
    {
        mouseScreenPosition = screenPosition;
        
        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, 
            screenPosition, 
            uiCamera, 
            out canvasPosition))
        {
            clampedCanvasPosition = ClampToScreenBounds(canvasPosition);
            crosshairRect.localPosition = clampedCanvasPosition;
        }
    }

    /// <summary>
    /// Refresh screen bounds (call when screen resolution changes).
    /// </summary>
    public void RefreshBounds()
    {
        InitializeBounds();
    }

    /// <summary>
    /// Check if the crosshair is currently being clamped to screen bounds.
    /// </summary>
    public bool IsClamped()
    {
        if (!boundsInitialized) return false;
        
        Vector2 unclampedPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, 
            mouseScreenPosition, 
            uiCamera, 
            out unclampedPosition);
            
        return Vector2.Distance(unclampedPosition, clampedCanvasPosition) > 0.1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure positive values
        screenEdgeOffset = Mathf.Max(0f, screenEdgeOffset);
        
        // Refresh bounds if changed in editor
        if (Application.isPlaying && boundsInitialized)
        {
            InitializeBounds();
        }
    }
#endif

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying || !boundsInitialized) return;
        
        // Draw screen bounds in Scene view
        Gizmos.color = Color.yellow;
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        Vector3 screenSize = new Vector3(Screen.width - screenEdgeOffset * 2, Screen.height - screenEdgeOffset * 2, 0);
        
        // Note: This is a simplified visualization - actual bounds depend on canvas settings
        Gizmos.DrawWireCube(screenCenter, screenSize);
    }
}
