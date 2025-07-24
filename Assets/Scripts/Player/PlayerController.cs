using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player movement and input.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The force applied to the ship to make it accelerate.")]
    [SerializeField] private float acceleration = 50f;
    [Tooltip("The maximum speed the ship can reach.")]
    [SerializeField] private float maxSpeed = 10f;
    [Tooltip("The drag applied to the ship to make it decelerate.")]
    [SerializeField] private float linearDrag = 2.5f;

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
    private bool isFiring;

    private void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        // We use Awake to ensure the reference is set before any other script tries to access it.
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = linearDrag;
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
    /// Updates the firing state based on whether the button is pressed or released.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    public void OnFire(InputValue value)
    {
        isFiring = value.isPressed;
    }

    private void FixedUpdate()
    {
        // Apply force for movement
        rb.AddForce(moveInput * acceleration);

        // Clamp velocity to max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
        }

        // Handle rotation and firing
        HandleRotation();
        HandleFiring();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        rb.linearDamping = linearDrag;
    }
#endif

    private void HandleFiring()
    {
        if (isFiring && Time.time >= nextFireTime)
        {
            if (projectilePrefab != null && firePoint != null)
            {
                Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
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
