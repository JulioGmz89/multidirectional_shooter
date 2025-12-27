using UnityEngine;
using ProjectMayhem.Audio;
using ProjectMayhem.UI.Indicators;

/// <summary>
/// Controls a simple enemy that moves directly towards the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour, IPooledObject, ITrackable
{
    public string PoolTag { get; set; }

    #region ITrackable Implementation
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => IndicatorType.ChaserEnemy;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 1;
    #endregion
    [Header("Chaser Settings")]
    [Tooltip("The speed at which the enemy moves towards the player.")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Separation Settings")]
    [Tooltip("How strongly enemies push away from each other.")]
    [SerializeField] private float separationStrength = 2f;
    
    [Tooltip("The radius within which enemies will try to separate from each other.")]
    [SerializeField] private float separationRadius = 1.5f;
    
    [Tooltip("Layer mask for detecting other enemies.")]
    [SerializeField] private LayerMask enemyLayerMask = -1;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private Health health;
    private PointsOnDeath pointsOnDeath;
    
    /// <summary>
    /// If true, this enemy was spawned by a boss and won't count towards wave completion.
    /// </summary>
    public bool IsSpawnedByBoss { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Freeze rotation so projectiles don't spin the enemy
        rb.freezeRotation = true;
        health = GetComponent<Health>();
        pointsOnDeath = GetComponent<PointsOnDeath>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += Defeat;
        }

        // Register with off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.RegisterTarget(this);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= Defeat;
        }

        // Unregister from off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.UnregisterTarget(this);
        }
    }

    /// <summary>
    /// Called by the ObjectPoolManager when the object is spawned.
    /// </summary>
    public void OnObjectSpawn()
    {
        // Find the player's transform. Caching this is more performant than finding it every frame.
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("ChaserEnemy: Could not find GameObject with 'Player' tag.", this);
            }
        }
        SFX.Play(AudioEvent.EnemySpawn, transform.position);
    }

    private void FixedUpdate()
    {
        // Only move if we have a valid target and the game is in the Gameplay state.
        if (playerTransform == null || GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            rb.linearVelocity = Vector2.zero; // Stop moving if paused or no target
            return;
        }

        // Calculate direction towards the player
        Vector2 chaseDirection = (playerTransform.position - transform.position).normalized;

        // Calculate separation force to avoid stacking with other enemies
        Vector2 separationForce = CalculateSeparation();

        // Combine chase direction with separation (separation is added, not blended)
        Vector2 finalDirection = (chaseDirection * moveSpeed) + separationForce;
        
        // Set velocity
        rb.linearVelocity = finalDirection;

        // Rotate to face movement direction (or player if barely moving)
        Vector2 facingDirection = finalDirection.sqrMagnitude > 0.1f ? finalDirection.normalized : chaseDirection;
        float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
    }

    /// <summary>
    /// Calculates a separation force to push away from nearby enemies.
    /// </summary>
    private Vector2 CalculateSeparation()
    {
        Vector2 separationForce = Vector2.zero;
        
        // Find all nearby enemies
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayerMask);
        
        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Skip self
            if (enemyCollider.gameObject == gameObject) continue;
            
            // Skip non-enemy objects (in case layer mask catches other things)
            if (!enemyCollider.CompareTag("Enemy")) continue;
            
            Vector2 directionAway = (Vector2)transform.position - (Vector2)enemyCollider.transform.position;
            float distance = directionAway.magnitude;
            
            if (distance > 0f && distance < separationRadius)
            {
                // Stronger push when closer (inverse relationship)
                float strength = (separationRadius - distance) / separationRadius;
                separationForce += directionAway.normalized * strength * separationStrength;
            }
        }
        
        return separationForce;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the enemy collides with the player, deal damage to the player.
        if (collision.gameObject.CompareTag("Player"))
        {
            Health playerHealth = collision.gameObject.GetComponent<Health>();
            DamageDealer damageDealer = GetComponent<DamageDealer>();

            if (playerHealth != null && damageDealer != null)
            {
                playerHealth.TakeDamage(damageDealer.GetDamage(), gameObject);
            }
        }
    }

    /// <summary>
    /// Handles the defeat of the enemy.
    /// </summary>
    private void Defeat()
    {
        SFX.Play(AudioEvent.EnemyDeath, transform.position);
        // Trigger camera shake for enemy death
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.TriggerEnemyDeathShake();
        }
        
        // Add points to the score if the component exists.
        if (pointsOnDeath != null)
        {
            ScoreManager.Instance.AddScore(pointsOnDeath.GetPoints());
        }

        // Only notify WaveManager if this wasn't spawned by a boss
        // (boss-spawned minions don't count towards wave completion)
        if (!IsSpawnedByBoss)
        {
            WaveManager.Instance.OnEnemyDefeated();
        }

        // Reset the flag for when this enemy is reused from the pool
        IsSpawnedByBoss = false;

        // Return this object to the pool.
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }
}
