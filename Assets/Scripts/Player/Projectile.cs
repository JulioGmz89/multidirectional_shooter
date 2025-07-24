using UnityEngine;

/// <summary>
/// Manages the behavior of a projectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("The speed at which the projectile travels.")]
    [SerializeField] private float moveSpeed = 10f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Propel the projectile forward based on its initial rotation.
        // 'transform.up' points in the local Y direction, which we align with the ship's forward direction.
        rb.linearVelocity = transform.up * moveSpeed;
    }
}
