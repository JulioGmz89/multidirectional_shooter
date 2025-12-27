using UnityEngine;

/// <summary>
/// A simple component that stores a damage value with optional multiplier.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Tooltip("The base amount of damage this object deals on collision.")]
    [SerializeField] private int baseDamage = 1;
    
    private float damageMultiplier = 1f;

    public int GetDamage()
    {
        return Mathf.RoundToInt(baseDamage * damageMultiplier);
    }
    
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }
    
    public void ResetDamageMultiplier()
    {
        damageMultiplier = 1f;
    }
}
