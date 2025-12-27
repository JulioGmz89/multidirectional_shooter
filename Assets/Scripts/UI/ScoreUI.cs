using UnityEngine;
using TMPro;

/// <summary>
/// Updates a TextMeshPro component to display the current score from the ScoreManager.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI scoreText;

    private void Awake()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Subscribe to the score changed event.
        ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }

    private void UpdateScoreText(int score)
    {
        // Update the text display.
        scoreText.text = score.ToString();
    }
}
