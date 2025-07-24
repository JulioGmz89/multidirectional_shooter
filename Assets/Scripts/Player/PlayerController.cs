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

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        // We use Awake to ensure the reference is set before any other script tries to access it.
        rb = GetComponent<Rigidbody2D>();
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

    private void FixedUpdate()
    {
        // Apply physics-based movement in FixedUpdate for consistency.
        // This ensures the movement speed is not tied to the frame rate.
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
