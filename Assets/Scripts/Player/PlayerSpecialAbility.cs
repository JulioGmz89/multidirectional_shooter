using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectMayhem.Audio;

/// <summary>
/// Player special ability that wipes all enemies in a radius.
/// Has 2 charges with auto-recharge functionality.
/// </summary>
public class PlayerSpecialAbility : MonoBehaviour
{
    [Header("Ability Settings")]
    [Tooltip("Maximum number of charges.")]
    [SerializeField] private int maxCharges = 2;
    
    [Tooltip("Time to recharge one charge (in seconds).")]
    [SerializeField] private float rechargeTime = 10f;
    
    [Tooltip("Radius of the ability effect (should match visible screen size).")]
    [SerializeField] private float abilityRadius = 15f;
    
    [Tooltip("Layer mask for detecting enemies.")]
    [SerializeField] private LayerMask enemyLayerMask = -1;
    
    [Tooltip("Damage to deal to enemies (set very high for one-shot).")]
    [SerializeField] private int damage = 9999;

    // Current state
    private int currentCharges;
    private float[] chargeProgress; // 0 to 1 for each charge slot
    private Coroutine rechargeCoroutine;

    /// <summary>
    /// Event fired when charges or progress changes.
    /// Parameters: currentCharges, chargeProgress array
    /// </summary>
    public event Action<int, float[]> OnChargesChanged;

    /// <summary>
    /// Event fired when ability is activated.
    /// </summary>
    public event Action OnAbilityUsed;

    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;
    public float[] ChargeProgress => chargeProgress;

    private void Awake()
    {
        currentCharges = maxCharges;
        chargeProgress = new float[maxCharges];
        
        // Initialize all charges as full
        for (int i = 0; i < maxCharges; i++)
        {
            chargeProgress[i] = 1f;
        }
    }

    private void OnEnable()
    {
        // Notify UI of initial state
        OnChargesChanged?.Invoke(currentCharges, chargeProgress);
    }

    /// <summary>
    /// Called by PlayerInput when the SpecialAbility action is triggered.
    /// </summary>
    public void OnSpecialAbility(InputValue value)
    {
        if (!value.isPressed) return;
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay) return;
        
        UseAbility();
    }

    /// <summary>
    /// Attempts to use the special ability.
    /// </summary>
    public void UseAbility()
    {
        if (currentCharges <= 0)
        {
            // No charges available - could play a "not ready" sound
            return;
        }

        // Find the last full slot (highest index with progress >= 1) and use it
        int slotToUse = -1;
        for (int i = maxCharges - 1; i >= 0; i--)
        {
            if (chargeProgress[i] >= 1f)
            {
                slotToUse = i;
                break;
            }
        }
        
        if (slotToUse == -1) return; // Safety check
        
        // Use the charge
        chargeProgress[slotToUse] = 0f;
        currentCharges = CountFullCharges();
        
        // Execute the ability effect
        DamageEnemiesInRadius();
        
        // Fire events
        OnAbilityUsed?.Invoke();
        OnChargesChanged?.Invoke(currentCharges, chargeProgress);
        
        // Start recharging if not already
        if (rechargeCoroutine == null)
        {
            rechargeCoroutine = StartCoroutine(RechargeCoroutine());
        }
    }

    /// <summary>
    /// Finds and damages all enemies within the ability radius.
    /// </summary>
    private void DamageEnemiesInRadius()
    {
        // Find all enemies in radius
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, abilityRadius, enemyLayerMask);
        
        int enemiesHit = 0;
        
        foreach (Collider2D enemyCollider in enemies)
        {
            // Skip non-enemies
            if (!enemyCollider.CompareTag("Enemy")) continue;
            
            // Get Health component and deal damage
            Health health = enemyCollider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, gameObject);
                enemiesHit++;
            }
        }
        
        Debug.Log($"PlayerSpecialAbility: Hit {enemiesHit} enemies in radius {abilityRadius}");
        
        // Play ability sound effect
        SFX.Play(AudioEvent.ExplosionSmall, transform.position);
        
        // Trigger camera shake
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.TriggerPlayerDeathShake(); // Strong shake for ability
        }
    }

    /// <summary>
    /// Coroutine that handles recharging charges one at a time.
    /// Always recharges from slot 0 first, then slot 1, etc.
    /// </summary>
    private IEnumerator RechargeCoroutine()
    {
        while (currentCharges < maxCharges)
        {
            // Find the first slot that needs recharging (progress < 1)
            int chargingSlot = -1;
            for (int i = 0; i < maxCharges; i++)
            {
                if (chargeProgress[i] < 1f)
                {
                    chargingSlot = i;
                    break;
                }
            }
            
            // No slot to charge (shouldn't happen, but safety check)
            if (chargingSlot == -1) break;
            
            // Store the starting progress in case we're resuming a partial charge
            float startProgress = chargeProgress[chargingSlot];
            float elapsed = startProgress * rechargeTime;
            
            while (elapsed < rechargeTime)
            {
                // Check if this slot was already filled (e.g., by another system)
                if (chargeProgress[chargingSlot] >= 1f) break;
                
                // Only recharge during gameplay
                if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
                {
                    elapsed += Time.deltaTime;
                    chargeProgress[chargingSlot] = Mathf.Min(elapsed / rechargeTime, 1f);
                    OnChargesChanged?.Invoke(currentCharges, chargeProgress);
                }
                
                yield return null;
            }
            
            // Charge complete
            chargeProgress[chargingSlot] = 1f;
            currentCharges = CountFullCharges();
            OnChargesChanged?.Invoke(currentCharges, chargeProgress);
        }
        
        rechargeCoroutine = null;
    }
    
    /// <summary>
    /// Counts how many charges are fully charged.
    /// </summary>
    private int CountFullCharges()
    {
        int count = 0;
        for (int i = 0; i < maxCharges; i++)
        {
            if (chargeProgress[i] >= 1f)
            {
                count++;
            }
        }
        return count;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the ability radius in editor
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, abilityRadius);
    }
}
