using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player's pupil movement to follow the mouse/aim direction.
/// The pupil moves within the eye boundary like a real eye looking around.
/// </summary>
public class PlayerPupil : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum distance the pupil can move from the center of the eye.")]
    [SerializeField] private float maxOffset = 0.3f;
    
    [Tooltip("How quickly the pupil moves to the target position.")]
    [SerializeField] private float smoothSpeed = 15f;
    
    [Tooltip("Minimum distance from mouse to player before pupil starts moving.")]
    [SerializeField] private float minMouseDistance = 0.5f;

    [Header("References")]
    [Tooltip("Reference to the fire point transform (child of pupil).")]
    [SerializeField] private Transform firePoint;

    private Camera mainCamera;
    private Vector3 centerPosition;
    private Vector2 currentAimDirection;
    private PlayerInput playerInput;

    /// <summary>
    /// The current aim direction normalized. Used by PlayerController for firing.
    /// </summary>
    public Vector2 AimDirection => currentAimDirection;

    /// <summary>
    /// The world position where projectiles should spawn.
    /// </summary>
    public Vector3 FirePointPosition => firePoint != null ? firePoint.position : transform.position;

    /// <summary>
    /// The rotation for projectiles to use (facing the aim direction).
    /// </summary>
    public Quaternion FirePointRotation => firePoint != null ? firePoint.rotation : transform.rotation;

    private void Awake()
    {
        mainCamera = Camera.main;
        // Store the local center position (relative to parent)
        centerPosition = transform.localPosition;
        playerInput = GetComponentInParent<PlayerInput>();
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && 
            GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            return;
        }

        UpdatePupilPosition();
    }

    private void UpdatePupilPosition()
    {
        Vector2 aimDirection = GetAimDirection();
        
        // Only move if we have significant input
        if (aimDirection.sqrMagnitude > 0.01f)
        {
            currentAimDirection = aimDirection.normalized;
            
            // Calculate target position for the pupil
            Vector3 targetLocalPosition = centerPosition + (Vector3)(currentAimDirection * maxOffset);
            
            // Smoothly move the pupil to the target position
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, 
                targetLocalPosition, 
                smoothSpeed * Time.deltaTime
            );
            
            // Rotate the pupil to face the aim direction (for the fire point)
            float angle = Mathf.Atan2(currentAimDirection.y, currentAimDirection.x) * Mathf.Rad2Deg - 90f;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private Vector2 GetAimDirection()
    {
        // Check if using gamepad
        if (playerInput != null && playerInput.currentControlScheme == "Gamepad")
        {
            return GetGamepadAimDirection();
        }
        else
        {
            return GetMouseAimDirection();
        }
    }

    private Vector2 GetMouseAimDirection()
    {
        if (Mouse.current == null) return currentAimDirection;
        
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane)
        );
        
        Vector2 playerPos = transform.parent.position;
        Vector2 direction = (Vector2)mouseWorldPos - playerPos;
        
        // Only return direction if mouse is far enough from player
        if (direction.magnitude < minMouseDistance)
        {
            return currentAimDirection; // Keep previous direction
        }
        
        return direction.normalized;
    }

    private Vector2 GetGamepadAimDirection()
    {
        if (Gamepad.current == null) return currentAimDirection;
        
        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
        
        if (rightStick.sqrMagnitude > 0.1f)
        {
            return rightStick.normalized;
        }
        
        return currentAimDirection; // Keep previous direction
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw the maximum movement range
        Vector3 center = Application.isPlaying ? 
            transform.parent.position + centerPosition : 
            transform.position;
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(center, maxOffset);
        
        // Draw current aim direction
        if (Application.isPlaying && currentAimDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.parent.position, currentAimDirection * 2f);
        }
    }
#endif
}
