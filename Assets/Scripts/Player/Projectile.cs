using UnityEngine;

/// <summary>
/// Manages the behavior of a projectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IPooledObject
{
    [Header("Projectile Settings")]
    [Tooltip("The speed at which the projectile travels.")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called by the ObjectPoolManager when the object is spawned from the pool.
    /// </summary>
    public void OnObjectSpawn()
    {
        // Set the velocity of the projectile to move it forward.
        rb.linearVelocity = transform.up * moveSpeed;
        // Automatically return the projectile to the pool after its lifetime expires.
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void ReturnToPool()
    {
        // The tag must match the one set in the ObjectPoolManager inspector
        ObjectPoolManager.Instance.ReturnToPool("PlayerProjectile", gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Cancel the invoke in case the projectile hits something before its lifetime expires
        CancelInvoke(nameof(ReturnToPool));
        ReturnToPool();
    }
}
