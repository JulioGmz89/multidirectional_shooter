using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the wave counter UI, switching between icons and text.
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("Icon Display (Waves 1-5)")]
    [Tooltip("The parent object for the wave icons.")]
    [SerializeField] private GameObject waveIconContainer;
    [Tooltip("A list of GameObjects representing the icons for waves 1-5.")]
    [SerializeField] private List<GameObject> waveIcons;

    [Header("Text Display (Wave 6+)")]
    [Tooltip("The parent object for the wave text.")]
    [SerializeField] private GameObject waveTextContainer;
    [Tooltip("The TextMeshPro component to display the wave number.")]
    [SerializeField] private TextMeshProUGUI waveText;

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += UpdateWaveDisplay;
        }
        else
        {
            Debug.LogWarning("WaveManager instance not found. Wave UI may not update.");
        }
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged -= UpdateWaveDisplay;
        }
    }

    private void UpdateWaveDisplay(int currentWave)
    {
        if (currentWave <= 5)
        {
            // Use icons for waves 1-5
            waveIconContainer.SetActive(true);
            waveTextContainer.SetActive(false);

            for (int i = 0; i < waveIcons.Count; i++)
            {
                // Activate icons up to the current wave number (e.g., wave 3 activates icons 0, 1, 2)
                waveIcons[i].SetActive(i < currentWave);
            }
        }
        else
        {
            // Use text for waves 6 and above
            waveIconContainer.SetActive(false);
            waveTextContainer.SetActive(true);
            waveText.text = currentWave.ToString();
        }
    }
}
