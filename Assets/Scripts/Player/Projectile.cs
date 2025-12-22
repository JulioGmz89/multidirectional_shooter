using UnityEngine;
using ProjectMayhem.Audio;

/// <summary>
/// Manages the behavior of a projectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IPooledObject
{
    // IPooledObject implementation
    public string PoolTag { get; set; }
    [Header("Projectile Settings")]
    [Tooltip("The speed at which the projectile travels.")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D rb;
    private TrailRendererController trailController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetupTrailRenderer();
    }

    /// <summary>
    /// Sets up the trail renderer for this projectile based on its tag
    /// </summary>
    private void SetupTrailRenderer()
    {
        if (TrailManager.Instance != null)
        {
            string objectType = gameObject.CompareTag("PlayerProjectile") ? "PlayerProjectile" : "EnemyProjectile";
            trailController = TrailManager.Instance.SetupTrailForObject(gameObject, objectType);
        }
    }

    /// <summary>
    /// Called by the ObjectPoolManager when the object is spawned from the pool.
    /// </summary>
    public void OnObjectSpawn()
    {
        // Initialize trail when spawned from pool
        if (TrailManager.Instance != null)
        {
            TrailManager.Instance.InitializeTrailForPooledObject(gameObject);
        }
        
        // Automatically return the projectile to the pool after its lifetime expires.
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    /// <summary>
    /// Sets the projectile's velocity, overriding any inherited velocity.
    /// </summary>
    /// <param name="direction">The direction the projectile should travel in.</param>
    public void SetVelocity(Vector2 direction)
    {
        rb.linearVelocity = direction * moveSpeed;
    }

    private void ReturnToPool()
    {
        // Clean up trail before returning to pool
        if (TrailManager.Instance != null)
        {
            TrailManager.Instance.CleanupTrailForPooledObject(gameObject);
        }
        
        if (string.IsNullOrEmpty(PoolTag))
        {
            Debug.LogError($"PoolTag not set on {gameObject.name}. Cannot return to pool.");
            Destroy(gameObject); // Fallback to destroying the object
            return;
        }
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If this is a player's projectile, do not process collisions with the player.
        if (gameObject.CompareTag("PlayerProjectile") && collision.gameObject.CompareTag("Player"))
        {
            // We don't return to the pool here, allowing the projectile to continue its path.
            // This prevents projectiles from disappearing if the player runs into them.
            return;
        }

        // Try to find a Health component on the object we collided with.
        Health health = collision.gameObject.GetComponent<Health>();
        DamageDealer damageDealer = GetComponent<DamageDealer>();

        // If the object has health and this projectile has a damage dealer, deal damage.
        if (health != null && damageDealer != null)
        {
            health.TakeDamage(damageDealer.GetDamage(), gameObject);
        }

        // Play a small explosion/impact at the collision point
        SFX.Play(AudioEvent.ExplosionSmall, transform.position);

        // Cancel the timed return and return to the pool immediately after any collision.
        CancelInvoke(nameof(ReturnToPool));
        ReturnToPool();
    }
}
