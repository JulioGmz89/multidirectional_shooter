using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// "Enter the Gungeon" style camera controller with dead zone and mouse look-ahead.
/// Provides smooth camera movement that follows the player while looking ahead based on mouse position.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SmartCameraController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Configuration asset for camera behavior settings")]
    [SerializeField] private SmartCameraConfig config;
    
    [Header("Target Settings")]
    [Tooltip("The player transform to follow")]
    [SerializeField] private Transform playerTransform;
    
    [Tooltip("Automatically find the player if not assigned")]
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Debug")]
    [Tooltip("Show debug information in Scene view")]
    [SerializeField] private bool showDebugInfo = false;

    // Private fields
    private Camera cameraComponent;
    private CameraShake cameraShake;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3 baseCameraPosition;
    private Vector2 lastMouseWorldPosition;
    private bool hasValidMouseInput;
    private float mouseInputTimeout = 0.1f;
    private float lastMouseInputTime;

    // Cache for performance
    private Vector3 playerPosition;
    private Vector2 mouseWorldPosition;
    private Vector2 deadZoneMin, deadZoneMax;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        cameraShake = GetComponent<CameraShake>();
        
        // Initialize target position to current camera position
        targetPosition = transform.localPosition;
        baseCameraPosition = targetPosition;
        
        // Find player if not assigned
        if (autoFindPlayer && playerTransform == null)
        {
            FindPlayer();
        }
    }

    private void Start()
    {
        // Validate configuration
        if (config == null)
        {
            Debug.LogError("SmartCameraController: No configuration assigned! Camera will not function properly.", this);
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("SmartCameraController: No player transform found! Camera will not function properly.", this);
            enabled = false;
            return;
        }

        // Warn if CameraShake component is missing
        if (cameraShake == null)
        {
            Debug.LogWarning("SmartCameraController: No CameraShake component found on camera. Camera shake effects may not work properly with smart camera movement.", this);
        }

        // Initialize camera position to player position
        Vector3 initialPos = playerTransform.position;
        initialPos.z = transform.localPosition.z; // Maintain camera's Z position
        transform.localPosition = initialPos;
        targetPosition = initialPos;
        baseCameraPosition = initialPos;
    }

    private void LateUpdate()
    {
        if (config == null) return;

        // Check if player reference is lost and try to re-find it
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            if (autoFindPlayer)
            {
                Debug.LogWarning("SmartCameraController: Player reference lost, attempting to re-find player...", this);
                FindPlayer();
                
                if (playerTransform != null)
                {
                    Debug.Log("SmartCameraController: Successfully re-found player!", this);
                }
            }
            
            // If still no player found, stop updating
            if (playerTransform == null)
            {
                return;
            }
        }

        // Only update during gameplay
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            return;
        }

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // Cache player position
        playerPosition = playerTransform.position;
        
        // Get mouse world position from PlayerController if available
        UpdateMouseWorldPosition();
        
        // Calculate target position based on dead zone and mouse look-ahead
        CalculateTargetPosition();
        
        // Apply boundaries if enabled
        if (config.UseBoundaries)
        {
            ApplyBoundaries();
        }
        
        // Smooth camera movement
        SmoothCameraMovement();
    }

    private void UpdateMouseWorldPosition()
    {
        // Check if mouse is available
        if (Mouse.current == null)
        {
            hasValidMouseInput = false;
            return;
        }
        
        // Get mouse input using the new Input System
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseWorldPosition = cameraComponent.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cameraComponent.nearClipPlane));
        
        // Check if mouse input is valid (mouse moved recently)
        if (Vector2.Distance(mouseWorldPosition, lastMouseWorldPosition) > 0.01f)
        {
            lastMouseInputTime = Time.time;
            hasValidMouseInput = true;
            lastMouseWorldPosition = mouseWorldPosition;
        }
        else if (Time.time - lastMouseInputTime > mouseInputTimeout)
        {
            hasValidMouseInput = false;
        }
    }

    private void CalculateTargetPosition()
    {
        // Start with player position
        Vector3 baseTarget = playerPosition;
        
        // Calculate dead zone boundaries
        deadZoneMin = new Vector2(playerPosition.x - config.DeadZoneSize.x * 0.5f, 
                                  playerPosition.y - config.DeadZoneSize.y * 0.5f);
        deadZoneMax = new Vector2(playerPosition.x + config.DeadZoneSize.x * 0.5f, 
                                  playerPosition.y + config.DeadZoneSize.y * 0.5f);
        
        // Check if camera is outside dead zone
        Vector2 currentCameraPos2D = new Vector2(transform.localPosition.x, transform.localPosition.y);
        bool outsideDeadZone = currentCameraPos2D.x < deadZoneMin.x || currentCameraPos2D.x > deadZoneMax.x ||
                               currentCameraPos2D.y < deadZoneMin.y || currentCameraPos2D.y > deadZoneMax.y;
        
        // If outside dead zone, move camera to keep player in frame
        if (outsideDeadZone)
        {
            // Clamp camera position to dead zone boundaries
            baseTarget.x = Mathf.Clamp(currentCameraPos2D.x, deadZoneMin.x, deadZoneMax.x);
            baseTarget.y = Mathf.Clamp(currentCameraPos2D.y, deadZoneMin.y, deadZoneMax.y);
        }
        else
        {
            // Inside dead zone, use current camera position as base
            baseTarget = transform.localPosition;
        }
        
        // Apply mouse look-ahead if we have valid mouse input
        if (hasValidMouseInput && config.MouseInfluence > 0f)
        {
            Vector2 mouseDirection = (mouseWorldPosition - (Vector2)playerPosition);
            float mouseDistance = mouseDirection.magnitude;
            
            // Only apply mouse influence if mouse is far enough from player
            if (mouseDistance > config.MinMouseDistance)
            {
                // Normalize and clamp the look-ahead distance
                mouseDirection = mouseDirection.normalized * Mathf.Min(mouseDistance, config.MaxLookAheadDistance);
                
                // Blend between base target and mouse look-ahead position
                Vector2 lookAheadTarget = (Vector2)playerPosition + mouseDirection * config.MouseInfluence;
                baseTarget.x = Mathf.Lerp(baseTarget.x, lookAheadTarget.x, config.MouseInfluence);
                baseTarget.y = Mathf.Lerp(baseTarget.y, lookAheadTarget.y, config.MouseInfluence);
            }
        }
        
        // Maintain camera's Z position
        baseTarget.z = transform.localPosition.z;
        targetPosition = baseTarget;
    }

    private void ApplyBoundaries()
    {
        // Get camera bounds
        float cameraHalfHeight = cameraComponent.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * cameraComponent.aspect;
        
        // Clamp target position to world boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, 
            config.WorldBoundaries.xMin + cameraHalfWidth, 
            config.WorldBoundaries.xMax - cameraHalfWidth);
        targetPosition.y = Mathf.Clamp(targetPosition.y, 
            config.WorldBoundaries.yMin + cameraHalfHeight, 
            config.WorldBoundaries.yMax - cameraHalfHeight);
    }

    private void SmoothCameraMovement()
    {
        // Store previous base position to detect movement
        Vector3 previousBasePosition = baseCameraPosition;
        
        // Choose appropriate speed based on situation
        float currentSpeed = config.FollowSpeed;
        
        if (hasValidMouseInput && config.MouseInfluence > 0f)
        {
            currentSpeed = config.LookAheadSpeed;
        }
        else if (!hasValidMouseInput)
        {
            currentSpeed = config.ReturnSpeed;
        }
        
        // Smooth base movement using SmoothDamp for natural feel
        baseCameraPosition = Vector3.SmoothDamp(baseCameraPosition, targetPosition, ref currentVelocity, 1f / currentSpeed);

        // Apply shake as an additive offset.
        Vector3 finalPosition = baseCameraPosition;
        if (cameraShake != null)
        {
            finalPosition += cameraShake.CurrentOffset;
        }

        transform.localPosition = finalPosition;

        // Keep legacy shake API calls harmless (CameraShake no longer owns base position).
        _ = previousBasePosition;
    }

    private void FindPlayer()
    {
        // Clear previous reference
        playerTransform = null;
        
        // Try to find player by tag first (only active objects)
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null && playerGO.activeInHierarchy)
        {
            playerTransform = playerGO.transform;
            Debug.Log("SmartCameraController: Found player by tag", this);
            return;
        }
        
        // Fallback: find by PlayerController component (only active objects)
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null && playerController.gameObject.activeInHierarchy)
        {
            playerTransform = playerController.transform;
            Debug.Log("SmartCameraController: Found player by PlayerController component", this);
            return;
        }
        
        // If no active player found, log warning
        Debug.LogWarning("SmartCameraController: No active player found in scene", this);
    }

    // Public methods for runtime configuration
    public void SetPlayer(Transform newPlayer)
    {
        playerTransform = newPlayer;
        Debug.Log("SmartCameraController: Player reference manually set", this);
    }

    public void SetConfig(SmartCameraConfig newConfig)
    {
        config = newConfig;
    }

    /// <summary>
    /// Manually refresh the player reference. Useful when player respawns or is reactivated.
    /// </summary>
    public void RefreshPlayerReference()
    {
        if (autoFindPlayer)
        {
            Debug.Log("SmartCameraController: Manually refreshing player reference...", this);
            FindPlayer();
        }
    }

    /// <summary>
    /// Check if the camera has a valid player reference.
    /// </summary>
    public bool HasValidPlayerReference()
    {
        return playerTransform != null && playerTransform.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Check if camera shake is currently active.
    /// </summary>
    private bool IsShakeActive()
    {
        if (cameraShake == null) return false;
        return cameraShake.IsShaking;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || config == null || playerTransform == null) return;

        // Draw dead zone
        if (config.ShowDeadZoneGizmo)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(playerTransform.position, config.DeadZoneSize);
        }

        // Draw mouse look-ahead
        if (hasValidMouseInput && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerTransform.position, mouseWorldPosition);
            Gizmos.DrawWireSphere(mouseWorldPosition, 0.2f);
        }

        // Draw target position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }

        // Draw world boundaries
        if (config.UseBoundaries)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(config.WorldBoundaries.center, config.WorldBoundaries.size);
        }
    }
}
