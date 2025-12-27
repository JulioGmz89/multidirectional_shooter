using UnityEngine;
using ProjectMayhem.Audio;
using ProjectMayhem.UI.Indicators;

public enum PowerUpType
{
    RapidFire,
    Shield
}

/// <summary>
/// Handles the behavior of a collectible power-up.
/// </summary>
public class PowerUp : MonoBehaviour, ITrackable
{
    [Header("Power-up Settings")]
    [Tooltip("The type of this power-up.")]
    [SerializeField] private PowerUpType powerUpType = PowerUpType.RapidFire;

    [Tooltip("How long the power-up effect lasts in seconds.")]
    [SerializeField] private float duration = 5f;

    [Header("Rapid Fire Settings")]
    [Tooltip("The multiplier to apply to the player's fire rate.")]
    [SerializeField] private float fireRateMultiplier = 2f;
    
    [Tooltip("The multiplier to apply to the player's damage.")]
    [SerializeField] private float damageMultiplier = 2f;

    #region ITrackable Implementation
    public Transform TrackableTransform => transform;
    public IndicatorType IndicatorType => powerUpType == PowerUpType.RapidFire 
        ? IndicatorType.RapidFire 
        : IndicatorType.Shield;
    public bool IsTrackingEnabled => gameObject.activeInHierarchy;
    public int TrackingPriority => 10; // Higher priority than enemies - power-ups are valuable
    #endregion

    private void OnEnable()
    {
        // Play a spawn sound when the power-up appears
        SFX.Play(AudioEvent.PickupSpawn, transform.position);

        // Register with off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.RegisterTarget(this);
        }
    }

    private void OnDisable()
    {
        // Unregister from off-screen indicator system
        if (OffScreenIndicatorManager.Instance != null)
        {
            OffScreenIndicatorManager.Instance.UnregisterTarget(this);
        }
    }

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
                        playerController.ActivateRapidFire(fireRateMultiplier, damageMultiplier, duration);
                        break;
                    case PowerUpType.Shield:
                        // If player already has shield, heal 1 HP instead
                        Health playerHealth = other.GetComponent<Health>();
                        if (playerHealth != null && playerHealth.IsShielded)
                        {
                            playerHealth.Heal(1);
                        }
                        else
                        {
                            playerController.ActivateShield();
                        }
                        break;
                }

                // Play collect sound
                SFX.Play(AudioEvent.PickupCollect, transform.position);

                // Destroy the power-up object after collection.
                Destroy(gameObject);
            }
        }
    }
}
