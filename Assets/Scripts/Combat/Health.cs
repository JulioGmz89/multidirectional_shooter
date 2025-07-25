using System;
using UnityEngine;

/// <summary>
/// Manages the health of a game object and handles taking damage.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The maximum health of the object.")]
    [SerializeField] private int maxHealth = 10;

    // Event invoked when health reaches zero.
    public event Action OnDeath;

    private int currentHealth;

    private void OnEnable()
    {
        // Reset health every time the object is enabled (works well with object pooling).
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces the object's health by a specified amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to take.</param>
    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damageAmount;
        // Debug.Log($"{gameObject.name} took {damageAmount} damage, has {currentHealth} health left.");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Notify any subscribers that this object has died.
        OnDeath?.Invoke();
    }
}
