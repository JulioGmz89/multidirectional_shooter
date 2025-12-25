using UnityEngine;
using ProjectMayhem.Audio;
using ProjectMayhem.UI.Indicators;

/// <summary>
/// Controls an enemy that attempts to maintain a specific range from the player and fire projectiles.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class ShooterEnemy : MonoBehaviour, IPooledObject, ITrackable
{
    public string PoolTag { get; set; }

    #region ITrackable Implementation
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => IndicatorType.ShooterEnemy;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 2; // Higher priority than ChaserEnemy (more dangerous)
    #endregion
    [Header("AI Settings")]
    [Tooltip("The ideal distance to keep from the player.")]
    [SerializeField] private float desiredRange = 8f;
    [Tooltip("How close the enemy can be before it starts backing away.")]
    [SerializeField] private float deadZone = 1f;
    [Tooltip("The speed at which the enemy moves.")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Weapon Settings")]
    [Tooltip("The projectile prefab the enemy fires.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The point from which projectiles are fired.")]
    [SerializeField] private Transform firePoint;
    [Tooltip("The number of shots the enemy can fire per second.")]
    [SerializeField] private float fireRate = 1f;
    [Tooltip("The tag used for spawning this enemy's projectile from the object pool.")]
    [SerializeField] private string projectilePoolTag = "EnemyProjectile";

    [Header("Separation Settings")]
    [Tooltip("How strongly enemies push away from each other.")]
    [SerializeField] private float separationStrength = 2f;
    
    [Tooltip("The radius within which enemies will try to separate from each other.")]
    [SerializeField] private float separationRadius = 1.5f;
    
    [Tooltip("Layer mask for detecting other enemies.")]
    [SerializeField] private LayerMask enemyLayerMask = -1;

    private Rigidbody2D rb;
    private Health health;
    private Transform playerTransform;
    private float nextFireTime;
    private PointsOnDeath pointsOnDeath;

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
        health.OnDeath += Defeat;

        // Register with off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.RegisterTarget(this);
        }
    }

    private void OnDisable()
    {
        health.OnDeath -= Defeat;

        // Unregister from off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.UnregisterTarget(this);
        }
    }

    public void OnObjectSpawn()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
        SFX.Play(AudioEvent.EnemySpawn, transform.position);
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        HandleMovement();
        HandleFiring();
    }

    private void HandleMovement()
    {
        Vector2 directionToPlayer = playerTransform.position - transform.position;
        float distance = directionToPlayer.magnitude;

        Vector2 moveDirection = Vector2.zero;

        if (distance > desiredRange)
        {
            // Too far, move closer
            moveDirection = directionToPlayer.normalized;
        }
        else if (distance < desiredRange - deadZone)
        {
            // Too close, move away
            moveDirection = -directionToPlayer.normalized;
        }
        // else: in range, don't move

        // Calculate separation force to avoid stacking with other enemies
        Vector2 separationForce = CalculateSeparation();

        // Combine movement with separation
        Vector2 finalVelocity = (moveDirection * moveSpeed) + separationForce;
        rb.linearVelocity = finalVelocity;

        // Always face the player (not movement direction, since shooter needs to aim)
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
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
            float dist = directionAway.magnitude;
            
            if (dist > 0f && dist < separationRadius)
            {
                // Stronger push when closer (inverse relationship)
                float strength = (separationRadius - dist) / separationRadius;
                separationForce += directionAway.normalized * strength * separationStrength;
            }
        }
        
        return separationForce;
    }

    private void HandleFiring()
    {
        if (Time.time >= nextFireTime)
        {
            GameObject projectileGO = ObjectPoolManager.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, firePoint.rotation);
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                // The enemy is already rotated to face the player, so firePoint.up is the correct direction.
                projectile.SetVelocity(firePoint.up);
            }
            SFX.Play(AudioEvent.EnemyShoot, transform.position);
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    private void Defeat()
    {
        SFX.Play(AudioEvent.EnemyDeath, transform.position);
        // Trigger camera shake for enemy death
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.TriggerEnemyDeathShake();
        }
        
        if (pointsOnDeath != null)
        {
            ScoreManager.Instance.AddScore(pointsOnDeath.GetPoints());
        }

        WaveManager.Instance.OnEnemyDefeated();
        ObjectPoolManager.Instance.ReturnToPool(gameObject.name, gameObject);
    }
}
