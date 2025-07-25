using System;
using UnityEngine;

/// <summary>
/// Manages the player's score and provides an event for UI updates.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public event Action<int> OnScoreChanged;

    private int currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        // Reset the score whenever the gameplay state starts.
        if (newState == GameState.Gameplay)
        {
            ResetScore();
        }
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;

        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    private void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
    }
}
