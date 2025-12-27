using UnityEngine;
using TMPro;

/// <summary>
/// Manages the wave counter UI, displaying the current wave number as text.
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("Text Display")]
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
        // Ensure text container is active
        if (waveTextContainer != null)
        {
            waveTextContainer.SetActive(true);
        }
        
        // Display wave number as text
        if (waveText != null)
        {
            waveText.text = currentWave.ToString();
        }
    }
}
