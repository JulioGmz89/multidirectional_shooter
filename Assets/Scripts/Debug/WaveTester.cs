using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Provides debug controls for testing the WaveManager.
/// </summary>
public class WaveTester : MonoBehaviour
{
    [Header("Controls")]
    [Tooltip("Press this key to instantly kill all enemies in the current wave.")]
    [SerializeField] private Key killAllEnemiesKey = Key.K;

    void Update()
    {
        // This debug tool should only be active in the editor.
#if UNITY_EDITOR
                // Check if the keyboard is present before trying to read from it.
        if (Keyboard.current != null && Keyboard.current[killAllEnemiesKey].wasPressedThisFrame)
        {
            KillAllEnemies();
        }
#endif
    }

    private void KillAllEnemies()
    {
        Debug.LogWarning($"DEBUG: Killing all enemies with key '{killAllEnemiesKey}'.");
        GameObject[] enemies;
        try
        {
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
        }
        catch (UnityException)
        {
            Debug.LogError("DEBUG ERROR: The 'Enemy' tag is not defined. Please go to Edit > Project Settings > Tags and Layers and add a new tag named 'Enemy'.");
            return;
        }

        if (enemies.Length == 0)
        {
            Debug.Log("DEBUG: No enemies found to kill.");
            return;
        }

        // Iterate backwards because dealing damage might remove items from the collection.
        foreach (GameObject enemy in enemies)
        {
            // Add a null check in case the enemy was destroyed by another's death event.
            if (enemy == null) continue;

            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                // Deal enough damage to ensure death, which will trigger OnDeath events correctly.
                health.TakeDamage(9999, gameObject);
            }
            else
            {
                // Fallback for enemies without a health component.
                Destroy(enemy);
            }
        }
        Debug.Log($"DEBUG: {enemies.Length} enemies were destroyed.");
    }
}
