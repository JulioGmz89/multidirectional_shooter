using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dynamically creates and manages a segmented, full-width health bar UI.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("UI Configuration")]
    [Tooltip("The prefab for a single health segment. Must have an Image component.")]
    [SerializeField] private GameObject healthSegmentPrefab;
    [Tooltip("The container with a Horizontal Layout Group that will hold the segments.")]
    [SerializeField] private RectTransform segmentsContainer;

    [Header("Health Colors")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = new Color(1.0f, 0.5f, 0.0f); // Orange
    [SerializeField] private Color criticalHealthColor = Color.red;
    [SerializeField] private Color lostHealthColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Power-Up Colors")]
    [SerializeField] private Color shieldActiveColor = new Color(0.0f, 0.8f, 1.0f); // Bright Blue

    private List<Image> segmentImages = new List<Image>();
    private Health playerHealth;
    private bool isShielded;

    private void Start()
    {
        // Find the player's Health component.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                InitializeHealthBar(playerHealth.GetMaxHealth());
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("Health component not found on Player object.");
            }
        }
        else
        {
            Debug.LogError("Player object not found. Make sure the player has the 'Player' tag.");
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnShieldActivated += HandleShieldActivated;
            playerHealth.OnShieldBroken += HandleShieldBroken;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnShieldActivated -= HandleShieldActivated;
            playerHealth.OnShieldBroken -= HandleShieldBroken;
        }
    }

    private void InitializeHealthBar(int maxHealth)
    {
        // Clear any old segments
        foreach (Transform child in segmentsContainer)
        {
            Destroy(child.gameObject);
        }
        segmentImages.Clear();

        // Create new segments
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject segment = Instantiate(healthSegmentPrefab, segmentsContainer);
            segmentImages.Add(segment.GetComponent<Image>());
        }

        HandleHealthChanged(playerHealth.GetCurrentHealth(), maxHealth);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // Use shield color if active, otherwise use health-based color
        Color activeColor = isShielded ? shieldActiveColor : GetHealthColor(currentHealth, maxHealth);
        UpdateBarColors(activeColor);
    }

    private void HandleShieldActivated()
    {
        isShielded = true;
        UpdateBarColors(shieldActiveColor);
    }

    private void HandleShieldBroken()
    {
        isShielded = false;
        // Restore normal health colors by recalculating the health color
        Color activeColor = GetHealthColor(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        UpdateBarColors(activeColor);
    }

    private void UpdateBarColors(Color activeColor)
    {
        int currentHealth = playerHealth.GetCurrentHealth();
        for (int i = 0; i < segmentImages.Count; i++)
        {
            segmentImages[i].color = i < currentHealth ? activeColor : lostHealthColor;
        }
    }

    private Color GetHealthColor(int currentHealth, int maxHealth)
    {
        float healthPercentage = (float)currentHealth / maxHealth;
        if (healthPercentage > 0.75f) return highHealthColor;
        if (healthPercentage > 0.5f) return mediumHealthColor;
        if (healthPercentage > 0.25f) return lowHealthColor;
        return criticalHealthColor;
    }
}
