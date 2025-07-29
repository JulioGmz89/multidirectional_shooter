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
    // Event invoked when the shield breaks.
    public event Action OnShieldBroken;

    private int currentHealth;
    private bool isShielded;

    private void OnEnable()
    {
        // Reset health and shield every time the object is enabled.
        currentHealth = maxHealth;
        isShielded = false;
    }

    /// <summary>
    /// Reduces the object's health by a specified amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to take.</param>
    public void ActivateShield()
    {
        isShielded = true;
    }

    public void TakeDamage(int damageAmount)
    {
        // If shielded, absorb the damage and break the shield.
        if (isShielded)
        {
            isShielded = false;
            OnShieldBroken?.Invoke();
            return;
        }

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
