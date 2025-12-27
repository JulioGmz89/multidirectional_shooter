using UnityEngine;

/// <summary>
/// Makes a projectile rotate continuously while traveling.
/// Attach this to projectiles that should spin (like the boss snipe projectile).
/// </summary>
public class RotatingProjectile : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 360f;
    
    [Tooltip("If true, rotation direction is randomized on spawn.")]
    [SerializeField] private bool randomizeDirection = true;
    
    private float direction = 1f;

    private void OnEnable()
    {
        // Randomize rotation direction when spawned
        if (randomizeDirection)
        {
            direction = Random.value > 0.5f ? 1f : -1f;
        }
    }

    private void Update()
    {
        // Only rotate while game is playing
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            return;
        }
        
        transform.Rotate(0f, 0f, rotationSpeed * direction * Time.deltaTime);
    }
}
