using UnityEngine;

/// <summary>
/// A simple component that stores a damage value.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Tooltip("The amount of damage this object deals on collision.")]
    [SerializeField] private int damage = 1;

    public int GetDamage()
    {
        return damage;
    }
}
