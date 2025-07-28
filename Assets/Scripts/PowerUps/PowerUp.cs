using UnityEngine;

public enum PowerUpType
{
    RapidFire
    // Future types can be added here, e.g., Shield, SpeedBoost
}

/// <summary>
/// Handles the behavior of a collectible power-up.
/// </summary>
public class PowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    [Tooltip("The type of this power-up.")]
    [SerializeField] private PowerUpType powerUpType = PowerUpType.RapidFire;

    [Tooltip("How long the power-up effect lasts in seconds.")]
    [SerializeField] private float duration = 5f;

    [Header("Rapid Fire Settings")]
    [Tooltip("The multiplier to apply to the player's fire rate.")]
    [SerializeField] private float fireRateMultiplier = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered the trigger is the player.
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Apply the correct power-up effect based on the type.
                switch (powerUpType)
                {
                    case PowerUpType.RapidFire:
                        playerController.ActivateRapidFire(fireRateMultiplier, duration);
                        break;
                }

                // Destroy the power-up object after collection.
                Destroy(gameObject);
            }
        }
    }
}
