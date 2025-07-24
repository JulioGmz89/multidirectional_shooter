using System;
using UnityEngine;

/// <summary>
/// Defines the possible states of the game.
/// </summary>
public enum GameState
{
    MainMenu,
    Gameplay,
    Pause,
    Victory,
    Defeat
}

/// <summary>
/// Manages the overall state of the game using a finite state machine.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // Singleton instance
    public static GameStateManager Instance { get; private set; }

    // Event for state changes
    public static event Action<GameState> OnStateChanged;

    [Header("State Configuration")]
    [Tooltip("The state the game will start in.")]
    [SerializeField] private GameState startingState = GameState.Gameplay;

#if UNITY_EDITOR
    [Header("Debug Settings")]
    [Tooltip("Use this to manually change the state in the Inspector during Play Mode.")]
    [SerializeField] private GameState debug_SetState;
#endif

    // Current state property
    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Start the game in the MainMenu state
        ChangeState(startingState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // This allows changing the state from the Inspector in Play Mode
        if (Application.isPlaying && debug_SetState != CurrentState)
        {
            ChangeState(debug_SetState);
        }
    }
#endif

    /// <summary>
    /// Changes the current game state and triggers associated logic.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    public void ChangeState(GameState newState)
    {
        if (newState == CurrentState) return;

        CurrentState = newState;
#if UNITY_EDITOR
        debug_SetState = CurrentState;
#endif

        switch (CurrentState)
        {
            case GameState.MainMenu:
                HandleMainMenu();
                break;
            case GameState.Gameplay:
                HandleGameplay();
                break;
            case GameState.Pause:
                HandlePause();
                break;
            case GameState.Victory:
                HandleVictory();
                break;
            case GameState.Defeat:
                HandleDefeat();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        // Notify other systems about the state change
        OnStateChanged?.Invoke(newState);
    }

    private void HandleMainMenu()
    {
        Time.timeScale = 1f; // Ensure time is running normally in the menu
        // Future logic: Load main menu scene, show menu UI, etc.
        Debug.Log("Game State changed to: MainMenu");
    }

    private void HandleGameplay()
    {
        Time.timeScale = 1f; // Ensure time is running for gameplay
        // Future logic: Load game scene, hide menus, enable player input, etc.
        Debug.Log("Game State changed to: Gameplay");
    }

    private void HandlePause()
    {
        Time.timeScale = 0f; // Pause the game
        // Future logic: Show pause menu UI, etc.
        Debug.Log("Game State changed to: Pause");
    }

    private void HandleVictory()
    {
        Time.timeScale = 0f; // Or 1f, depending on if you want background animations
        // Future logic: Show victory screen, save score, etc.
        Debug.Log("Game State changed to: Victory");
    }

    private void HandleDefeat()
    {
        Time.timeScale = 0f; // Or 1f
        // Future logic: Show defeat screen, offer retry, etc.
        Debug.Log("Game State changed to: Defeat");
    }
}
