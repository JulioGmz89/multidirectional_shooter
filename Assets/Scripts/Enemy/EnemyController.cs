using UnityEngine;

/// <summary>
/// A simple placeholder controller for an enemy.
/// </summary>
public class EnemyController : MonoBehaviour, IPooledObject
{
    [Tooltip("The lifetime of the enemy in seconds before it is automatically defeated.")]
    [SerializeField] private float lifeTime = 5f;

    public void OnObjectSpawn()
    {
        // When spawned, automatically schedule its destruction.
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void ReturnToPool()
    {
        // Notify the WaveManager that this enemy has been defeated.
        WaveManager.Instance.OnEnemyDefeated();
        
        // The tag must match the one set in the ObjectPoolManager inspector.
        ObjectPoolManager.Instance.ReturnToPool(gameObject.name, gameObject);
    }

    // In a real game, you would call this from a Health script when the enemy dies.
    public void Defeat()
    {
        CancelInvoke(nameof(ReturnToPool)); // Cancel the timed self-destruct.
        ReturnToPool();
    }
}
