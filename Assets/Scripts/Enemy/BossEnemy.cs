using UnityEngine;
using System.Collections;
using ProjectMayhem.Audio;
using ProjectMayhem.UI.Indicators;

/// <summary>
/// Boss enemy with equilateral triangle shape and phase-based attack patterns.
/// Phase 1 (100%-66%): Tri-Shot, Vertex Snipe, Orbit Spawn
/// Phase 2 (66%-33%): Charge Attack, Spiral Shot, Triangle Shield
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class BossEnemy : MonoBehaviour, IPooledObject, ITrackable
{
    public string PoolTag { get; set; }

    #region ITrackable Implementation
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => IndicatorType.ShooterEnemy; // Reuse existing indicator
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 10; // Highest priority - it's the boss!
    #endregion

    #region Enums
    private enum BossPhase { Phase1, Phase2 }
    private enum AttackType { TriShot, VertexSnipe, OrbitSpawn, ChargeAttack, SpiralShot, TriangleShield }
    #endregion

    #region Serialized Fields
    [Header("Boss Settings")]
    [Tooltip("The speed at which the boss moves.")]
    [SerializeField] private float moveSpeed = 2f;
    
    [Tooltip("The ideal distance to keep from the player.")]
    [SerializeField] private float desiredRange = 10f;
    
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Attack Timing")]
    [Tooltip("Time between attacks.")]
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("Fire Points (Triangle Vertices)")]
    [Tooltip("Fire points at each vertex of the triangle (should be 3).")]
    [SerializeField] private Transform[] firePoints;

    [Header("Projectile Settings")]
    [Tooltip("Pool tag for boss projectiles (used by Tri-Shot, Spiral Shot).")]
    [SerializeField] private string projectilePoolTag = "BossProjectile";

    [Header("Tri-Shot Settings")]
    [Tooltip("Number of projectiles per vertex in tri-shot.")]
    [SerializeField] private int triShotCount = 1;

    [Header("Vertex Snipe Settings")]
    [Tooltip("Pool tag for the high-damage snipe projectile.")]
    [SerializeField] private string snipeProjectilePoolTag = "BossSnipe";
    
    [Tooltip("Time to aim before firing vertex snipe.")]
    [SerializeField] private float vertexSnipeAimTime = 0.5f;

    [Header("Orbit Spawn Settings")]
    [Tooltip("Pool tag for spawned minions.")]
    [SerializeField] private string minionPoolTag = "ChaserEnemy";

    [Header("Charge Attack Settings")]
    [Tooltip("Speed of the charge attack.")]
    [SerializeField] private float chargeSpeed = 15f;
    
    [Tooltip("Duration of the charge attack.")]
    [SerializeField] private float chargeDuration = 0.6f;
    
    [Tooltip("Pause before charge begins.")]
    [SerializeField] private float chargeWindupTime = 0.3f;

    [Header("Spiral Shot Settings")]
    [Tooltip("Duration of the spiral shot attack.")]
    [SerializeField] private float spiralShotDuration = 3f;
    
    [Tooltip("Fire rate during spiral shot (shots per second).")]
    [SerializeField] private float spiralShotFireRate = 5f;
    
    [Tooltip("Rotation speed during spiral shot.")]
    [SerializeField] private float spiralRotationSpeed = 180f;

    [Header("Triangle Shield Settings")]
    [Tooltip("Duration of the shield.")]
    [SerializeField] private float shieldDuration = 3f;

    [Header("Separation Settings")]
    [Tooltip("How strongly boss pushes away from other enemies.")]
    [SerializeField] private float separationStrength = 3f;
    
    [Tooltip("Radius for separation detection.")]
    [SerializeField] private float separationRadius = 2f;
    
    [SerializeField] private LayerMask enemyLayerMask = -1;
    #endregion

    #region Private Fields
    private Rigidbody2D rb;
    private Health health;
    private PointsOnDeath pointsOnDeath;
    private Transform playerTransform;
    
    private BossPhase currentPhase = BossPhase.Phase1;
    private float nextAttackTime;
    private bool isAttacking;
    private bool isCharging;
    
    private int lastAttackIndex = -1;
    
    // Phase thresholds
    private const float PHASE2_THRESHOLD = 0.66f;
    
    // Default rotation (facing up)
    private const float DEFAULT_ROTATION = 0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        health = GetComponent<Health>();
        pointsOnDeath = GetComponent<PointsOnDeath>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += Defeat;
            health.OnHealthChanged += OnHealthChanged;
        }

        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.RegisterTarget(this);
        }

        currentPhase = BossPhase.Phase1;
        isAttacking = false;
        isCharging = false;
        rb.rotation = DEFAULT_ROTATION;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= Defeat;
            health.OnHealthChanged -= OnHealthChanged;
        }

        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.UnregisterTarget(this);
        }

        StopAllCoroutines();
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
            else
            {
                Debug.LogError("BossEnemy: Could not find GameObject with 'Player' tag.", this);
            }
        }
        
        nextAttackTime = Time.time + attackCooldown;
        SFX.Play(AudioEvent.EnemySpawn, transform.position);
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!isAttacking && !isCharging)
        {
            HandleMovement();
        }

        HandleAttackSelection();
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        Vector2 directionToPlayer = playerTransform.position - transform.position;
        float distance = directionToPlayer.magnitude;

        Vector2 moveDirection = Vector2.zero;

        if (distance > desiredRange)
        {
            moveDirection = directionToPlayer.normalized;
        }
        else if (distance < desiredRange * 0.7f)
        {
            moveDirection = -directionToPlayer.normalized;
        }

        Vector2 separationForce = CalculateSeparation();
        Vector2 finalVelocity = (moveDirection * moveSpeed) + separationForce;
        rb.linearVelocity = finalVelocity;

        // Keep facing up (default orientation)
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, DEFAULT_ROTATION, rotationSpeed * Time.fixedDeltaTime);
    }

    private Vector2 CalculateSeparation()
    {
        Vector2 separationForce = Vector2.zero;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayerMask);

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject == gameObject) continue;
            if (!enemyCollider.CompareTag("Enemy")) continue;

            Vector2 directionAway = (Vector2)transform.position - (Vector2)enemyCollider.transform.position;
            float dist = directionAway.magnitude;

            if (dist > 0f && dist < separationRadius)
            {
                float strength = (separationRadius - dist) / separationRadius;
                separationForce += directionAway.normalized * strength * separationStrength;
            }
        }

        return separationForce;
    }
    #endregion

    #region Phase Management
    private void OnHealthChanged(int currentHP, int maxHP)
    {
        float healthPercent = (float)currentHP / maxHP;

        if (healthPercent <= PHASE2_THRESHOLD && currentPhase == BossPhase.Phase1)
        {
            currentPhase = BossPhase.Phase2;
            Debug.Log("Boss entered Phase 2!");
        }
    }
    #endregion

    #region Attack Selection
    private void HandleAttackSelection()
    {
        if (isAttacking || Time.time < nextAttackTime) return;

        AttackType selectedAttack = SelectRandomAttack();
        StartCoroutine(ExecuteAttack(selectedAttack));
    }

    private AttackType SelectRandomAttack()
    {
        AttackType[] availableAttacks;

        if (currentPhase == BossPhase.Phase1)
        {
            availableAttacks = new[] { AttackType.TriShot, AttackType.VertexSnipe, AttackType.OrbitSpawn };
        }
        else
        {
            availableAttacks = new[] { AttackType.ChargeAttack, AttackType.SpiralShot, AttackType.TriangleShield };
        }

        // Avoid repeating the same attack twice
        int attackIndex;
        do
        {
            attackIndex = Random.Range(0, availableAttacks.Length);
        } while (attackIndex == lastAttackIndex && availableAttacks.Length > 1);

        lastAttackIndex = attackIndex;
        return availableAttacks[attackIndex];
    }

    private IEnumerator ExecuteAttack(AttackType attack)
    {
        isAttacking = true;

        switch (attack)
        {
            case AttackType.TriShot:
                yield return StartCoroutine(PerformTriShot());
                break;
            case AttackType.VertexSnipe:
                yield return StartCoroutine(PerformVertexSnipe());
                break;
            case AttackType.OrbitSpawn:
                yield return StartCoroutine(PerformOrbitSpawn());
                break;
            case AttackType.ChargeAttack:
                yield return StartCoroutine(PerformChargeAttack());
                break;
            case AttackType.SpiralShot:
                yield return StartCoroutine(PerformSpiralShot());
                break;
            case AttackType.TriangleShield:
                yield return StartCoroutine(PerformTriangleShield());
                break;
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }
    #endregion

    #region Phase 1 Attacks
    /// <summary>
    /// Fires projectiles simultaneously from all 3 vertices, each firing outward.
    /// </summary>
    private IEnumerator PerformTriShot()
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("BossEnemy: No fire points assigned for Tri-Shot!");
            yield break;
        }

        for (int i = 0; i < triShotCount; i++)
        {
            foreach (Transform firePoint in firePoints)
            {
                if (firePoint == null) continue;
                
                // Calculate direction from boss center to fire point (outward from each vertex)
                Vector2 outwardDirection = (firePoint.position - transform.position).normalized;
                FireProjectile(firePoint.position, outwardDirection);
            }
            
            SFX.Play(AudioEvent.EnemyShoot, transform.position);
            
            if (i < triShotCount - 1)
            {
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    /// <summary>
    /// Rotates to point a vertex at the player, then fires a high-damage shot.
    /// </summary>
    private IEnumerator PerformVertexSnipe()
    {
        if (playerTransform == null || firePoints == null || firePoints.Length == 0) yield break;

        // Choose the fire point closest to aiming at the player
        Transform bestFirePoint = firePoints[0];
        
        // Aim phase - rotate to point at player
        float aimTimer = 0f;
        while (aimTimer < vertexSnipeAimTime)
        {
            if (playerTransform == null) yield break;
            
            Vector2 directionToPlayer = playerTransform.position - transform.position;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = rb.rotation;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * 2f * Time.fixedDeltaTime);
            rb.rotation = newAngle;
            
            aimTimer += Time.deltaTime;
            yield return null;
        }

        // Fire from the top vertex (assumed to be first fire point)
        if (bestFirePoint != null)
        {
            // Use the high-damage snipe projectile
            FireProjectile(bestFirePoint.position, bestFirePoint.up, snipeProjectilePoolTag);
            SFX.Play(AudioEvent.EnemyShoot, transform.position);
        }
        
        // Return to default rotation (facing up)
        float returnTimer = 0f;
        float returnDuration = 0.3f;
        while (returnTimer < returnDuration)
        {
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, DEFAULT_ROTATION, rotationSpeed * 2f * Time.fixedDeltaTime);
            rb.rotation = newAngle;
            returnTimer += Time.deltaTime;
            yield return null;
        }
        rb.rotation = DEFAULT_ROTATION;
    }

    /// <summary>
    /// Spawns chaser minions from each vertex.
    /// </summary>
    private IEnumerator PerformOrbitSpawn()
    {
        if (firePoints == null || firePoints.Length == 0) yield break;

        foreach (Transform firePoint in firePoints)
        {
            if (firePoint == null) continue;

            if (ObjectPoolManager.Instance != null)
            {
                GameObject minion = ObjectPoolManager.Instance.SpawnFromPool(minionPoolTag, firePoint.position, Quaternion.identity);
                if (minion != null)
                {
                    // Mark as boss-spawned so it doesn't count towards wave completion
                    ChaserEnemy chaserEnemy = minion.GetComponent<ChaserEnemy>();
                    if (chaserEnemy != null)
                    {
                        chaserEnemy.IsSpawnedByBoss = true;
                    }
                    
                    SFX.Play(AudioEvent.EnemySpawn, firePoint.position);
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
    #endregion

    #region Phase 2 Attacks
    /// <summary>
    /// Points a vertex at the player and rapidly charges forward.
    /// </summary>
    private IEnumerator PerformChargeAttack()
    {
        if (playerTransform == null) yield break;

        isCharging = true;
        rb.linearVelocity = Vector2.zero;

        // Windup - aim at player
        Vector2 chargeDirection = (playerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = targetAngle;

        yield return new WaitForSeconds(chargeWindupTime);

        // Charge!
        float chargeTimer = 0f;
        while (chargeTimer < chargeDuration)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            chargeTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        isCharging = false;
    }

    /// <summary>
    /// Slowly rotates while continuously firing from all vertices, creating a bullet spiral.
    /// </summary>
    private IEnumerator PerformSpiralShot()
    {
        if (firePoints == null || firePoints.Length == 0) yield break;

        float timer = 0f;
        float fireInterval = 1f / spiralShotFireRate;
        float nextFireTime = 0f;

        while (timer < spiralShotDuration)
        {
            // Rotate
            rb.rotation += spiralRotationSpeed * Time.fixedDeltaTime;

            // Fire at intervals
            if (timer >= nextFireTime)
            {
                foreach (Transform firePoint in firePoints)
                {
                    if (firePoint == null) continue;
                    FireProjectile(firePoint.position, firePoint.up);
                }
                SFX.Play(AudioEvent.EnemyShoot, transform.position);
                nextFireTime += fireInterval;
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// Activates a temporary shield that absorbs damage.
    /// </summary>
    private IEnumerator PerformTriangleShield()
    {
        if (health != null)
        {
            health.ActivateShield();
        }

        yield return new WaitForSeconds(shieldDuration);
    }
    #endregion

    #region Utility Methods
    private void FireProjectile(Vector3 position, Vector3 direction, string poolTag = null)
    {
        if (ObjectPoolManager.Instance == null) return;

        // Use provided pool tag or default to projectilePoolTag
        string tagToUse = poolTag ?? projectilePoolTag;
        
        // Calculate rotation to face the direction of travel
        // Assumes projectile sprite faces "up" (positive Y) by default
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        
        GameObject projectileGO = ObjectPoolManager.Instance.SpawnFromPool(tagToUse, position, rotation);
        if (projectileGO != null)
        {
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetVelocity(direction);
            }
        }
    }
    #endregion

    #region Collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Health playerHealth = collision.gameObject.GetComponent<Health>();
            DamageDealer damageDealer = GetComponent<DamageDealer>();

            if (playerHealth != null && damageDealer != null)
            {
                // Deal extra damage if charging
                int damage = damageDealer.GetDamage();
                if (isCharging)
                {
                    damage = Mathf.RoundToInt(damage * 1.5f);
                }
                playerHealth.TakeDamage(damage, gameObject);
            }
        }
    }
    #endregion

    #region Death
    private void Defeat()
    {
        SFX.Play(AudioEvent.EnemyDeath, transform.position);
        
        if (CameraShakeManager.Instance != null)
        {
            // Bigger shake for boss death
            CameraShakeManager.Instance.TriggerEnemyDeathShake();
            CameraShakeManager.Instance.TriggerEnemyDeathShake(); // Double shake for impact
        }

        if (pointsOnDeath != null)
        {
            ScoreManager.Instance.AddScore(pointsOnDeath.GetPoints());
        }

        WaveManager.Instance.OnEnemyDefeated();
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }
    #endregion
}
