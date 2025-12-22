using System;
using UnityEngine;
using ProjectMayhem.Audio;

/// <summary>
/// Manages the health of a game object and handles taking damage.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The maximum health of the object.")]
    [SerializeField] private int maxHealth = 10;

    [Tooltip("The current health of the object.")]
    [SerializeField] private int currentHealth;
    
    // Event invoked when health reaches zero.
    public event Action OnDeath;
    // Event invoked when the shield breaks.
    public event Action OnShieldActivated;
    public event Action OnShieldBroken;
    public event Action<int, int> OnHealthChanged;

    private bool isShielded;

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    private void OnEnable()
    {
        // Reset health and shield every time the object is enabled.
        currentHealth = maxHealth;
        isShielded = false;
        // Invoke event on enable to set initial UI state
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reduces the object's health by a specified amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to take.</param>
    public void ActivateShield()
    {
        isShielded = true;
        OnShieldActivated?.Invoke();
        SFX.Play(AudioEvent.PowerUpActivate, transform.position);
    }

    public void TakeDamage(int damageAmount, GameObject attacker)
    {
        // If shielded, absorb the damage and break the shield.
        if (isShielded)
        {
            isShielded = false;
            OnShieldBroken?.Invoke();
            // Play a shield break / hit feedback (use player hit for simplicity)
            if (gameObject.CompareTag("Player"))
            {
                SFX.Play(AudioEvent.PlayerHit, transform.position);
            }
            return; // Absorb the damage
        }

        if (currentHealth <= 0) return; // Already dead

        // --- DEBUG LOG ---
        Debug.Log($"{gameObject.name} took {damageAmount} damage from {attacker.name} (Tag: {attacker.tag})");

        currentHealth -= damageAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Play hit feedback for player/enemy when taking damage
        if (gameObject.CompareTag("Player"))
        {
            SFX.Play(AudioEvent.PlayerHit, transform.position);
        }
        else
        {
            SFX.Play(AudioEvent.EnemyHit, transform.position);
        }
        
        // Trigger camera shake for player damage
        if (gameObject.CompareTag("Player") && CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.TriggerPlayerDamageShake();
        }

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
