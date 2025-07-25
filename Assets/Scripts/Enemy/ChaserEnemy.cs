using UnityEngine;

/// <summary>
/// Controls a simple enemy that moves directly towards the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour, IPooledObject
{
    [Header("Chaser Settings")]
    [Tooltip("The speed at which the enemy moves towards the player.")]
    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody2D rb;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // For now, the enemy is defeated if it hits a player's projectile.
        if (collision.gameObject.CompareTag("PlayerProjectile"))
        {
            Defeat();
        }
    }

    /// <summary>
    /// Handles the defeat of the enemy.
    /// </summary>
    private void Defeat()
    {
        // Notify the WaveManager that this enemy is defeated.
        WaveManager.Instance.OnEnemyDefeated();

        // Return this object to the pool.
        // The tag must match the one in the ObjectPoolManager.
        ObjectPoolManager.Instance.ReturnToPool(gameObject.name, gameObject);
    }
}
