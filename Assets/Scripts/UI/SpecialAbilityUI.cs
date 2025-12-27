using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for displaying special ability charges.
/// Uses 2 layers per charge: background (always visible) and fill (shows progress).
/// </summary>
public class SpecialAbilityUI : MonoBehaviour
{
    [System.Serializable]
    public class ChargeDisplay
    {
        [Tooltip("Background image (gray, always visible).")]
        public Image backgroundImage;
        
        [Tooltip("Fill image (colored, shows charge progress).")]
        public Image fillImage;
    }
    
    [Header("Charge Displays")]
    [Tooltip("Charge display pairs (should have 2).")]
    [SerializeField] private ChargeDisplay[] chargeDisplays;
    
    [Header("Colors")]
    [Tooltip("Color for the fill image when charging.")]
    [SerializeField] private Color chargingColor = Color.white;

    private PlayerSpecialAbility playerAbility;

    private void Start()
    {
        // Find the player's special ability component
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerAbility = player.GetComponent<PlayerSpecialAbility>();
            if (playerAbility != null)
            {
                playerAbility.OnChargesChanged += UpdateChargeDisplay;
                
                // Initialize display
                UpdateChargeDisplay(playerAbility.CurrentCharges, playerAbility.ChargeProgress);
            }
        }
        
        if (playerAbility == null)
        {
            Debug.LogWarning("SpecialAbilityUI: Could not find PlayerSpecialAbility on player.");
        }
        
        // Setup fill images
        foreach (var display in chargeDisplays)
        {
            if (display.fillImage != null)
            {
                display.fillImage.type = Image.Type.Filled;
                display.fillImage.fillMethod = Image.FillMethod.Vertical;
                display.fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerAbility != null)
        {
            playerAbility.OnChargesChanged -= UpdateChargeDisplay;
        }
    }

    /// <summary>
    /// Updates the charge display based on current charges and progress.
    /// </summary>
    private void UpdateChargeDisplay(int currentCharges, float[] chargeProgress)
    {
        if (chargeDisplays == null) return;
        
        for (int i = 0; i < chargeDisplays.Length && i < chargeProgress.Length; i++)
        {
            var display = chargeDisplays[i];
            if (display.fillImage == null) continue;
            
            // Set fill amount based on progress (0 = empty, 1 = full)
            display.fillImage.fillAmount = chargeProgress[i];
            display.fillImage.color = chargingColor;
        }
    }
}

