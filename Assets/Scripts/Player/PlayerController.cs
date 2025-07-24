using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player movement and input.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The movement speed of the player ship in units per second.")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Weapon Settings")]
    [Tooltip("The projectile prefab to be fired.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The point from which projectiles are fired.")]
    [SerializeField] private Transform firePoint;
    [Tooltip("The number of shots the player can fire per second.")]
    [SerializeField] private float fireRate = 5f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float nextFireTime;

    private void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        // We use Awake to ensure the reference is set before any other script tries to access it.
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Called by the PlayerInput component when the Move action is triggered.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    public void OnMove(InputValue value)
    {
        // Read the Vector2 value from the input action and normalize it.
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by the PlayerInput component when the Look action is triggered.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by the PlayerInput component when the Fire action is triggered.
    /// </summary>
    public void OnFire()
    {
        // Check if enough time has passed to fire again.
        if (Time.time >= nextFireTime)
        {
            // Instantiate the projectile at the fire point's position and rotation.
            if (projectilePrefab != null && firePoint != null)
            {
                Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

                // Calculate the time of the next allowed shot.
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    private void FixedUpdate()
    {
        // Apply physics-based movement and rotation in FixedUpdate for consistency.
        rb.linearVelocity = moveInput * moveSpeed;
        HandleRotation();
    }

    private void HandleRotation()
    {
        Vector2 aimDirection;
        // Check if the current control scheme is Gamepad
        if (GetComponent<PlayerInput>().currentControlScheme == "Gamepad")
        {
            // Use the raw look input as the direction vector for gamepad
            aimDirection = lookInput;
        }
        else
        {
            // For Mouse, convert screen position to world position
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(lookInput);
            aimDirection = (mousePosition - (Vector2)transform.position).normalized;
        }

        // Only rotate if there is significant input
        if (aimDirection.sqrMagnitude > 0.1f)
        {
            // Calculate the angle and set the rotation of the Rigidbody
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }
}
