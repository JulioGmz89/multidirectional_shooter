using UnityEngine;
using ProjectMayhem.Audio;

/// <summary>
/// Controls a simple enemy that moves directly towards the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour, IPooledObject
{
    public string PoolTag { get; set; }
    [Header("Chaser Settings")]
    [Tooltip("The speed at which the enemy moves towards the player.")]
    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private Health health;
    private PointsOnDeath pointsOnDeath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        health = GetComponent<Health>();
        pointsOnDeath = GetComponent<PointsOnDeath>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += Defeat;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= Defeat;
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
        Vector2 direction = (playerTransform.position - transform.position).normalized;

        // Set velocity to move towards the player
        rb.linearVelocity = direction * moveSpeed;
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

        // Notify the WaveManager that this enemy is defeated.
        WaveManager.Instance.OnEnemyDefeated();

        // Return this object to the pool.
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }
}
