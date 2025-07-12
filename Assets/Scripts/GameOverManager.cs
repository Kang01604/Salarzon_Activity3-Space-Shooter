// GameOverManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using TMPro; // Assuming you are using TextMeshPro for UI text
using System.Collections; // NEW: Required for IEnumerator and Coroutines

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI finalScoreText; // Assign your TextMeshProUGUI for score display

    [Header("Scene Names")]
    [Tooltip("The name of your main gameplay scene (e.g., 'GameplayScene').")]
    public string gameplaySceneName = "Gameplay"; // Set this to "Gameplay"
    [Tooltip("The name of your Main Menu scene (e.g., 'Start').")]
    public string mainMenuSceneName = "Start"; // Set this to "Start"

    // NEW: Delay duration for scene transitions
    [Header("Transition Settings")]
    public float sceneTransitionDelay = 0.5f;

    void Start()
    {
        // Ensure the game is not paused when entering the game over screen
        Time.timeScale = 1f;

        // Ensure the cursor is visible and unlocked for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Display the final score from GameManager (if GameManager persists)
        if (GameManager.Instance != null && finalScoreText != null)
        {
            finalScoreText.text = "Final Score: " + GameManager.Instance.score.ToString();
        }
        else if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score: N/A"; // Fallback if GameManager is not found
            Debug.LogWarning("GameManager instance not found in GameOverManager. Score cannot be displayed.");
        }
    }

    /// <summary>
    /// Call this method when the "Restart Game" button is clicked.
    /// It now starts a coroutine to add a delay.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Restarting Game with delay...");
        StartCoroutine(RestartGameWithDelay());
    }

    /// <summary>
    /// Coroutine to handle the delay before restarting the game.
    /// </summary>
    private IEnumerator RestartGameWithDelay()
    {
        yield return new WaitForSeconds(sceneTransitionDelay); // Wait for the specified delay

        // Reset any necessary game state *before* loading the scene if GameManager doesn't handle it
        // e.g., GameManager.Instance.ResetGame(); // You'd need to add this method in GameManager
        SceneManager.LoadScene(gameplaySceneName); // Reload the main gameplay scene
    }

    /// <summary>
    /// Call this method when the "Main Menu" button is clicked.
    /// It now starts a coroutine to add a delay.
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log("Returning to Main Menu with delay...");
        StartCoroutine(GoToMainMenuWithDelay());
    }

    /// <summary>
    /// Coroutine to handle the delay before going to the main menu.
    /// </summary>
    private IEnumerator GoToMainMenuWithDelay()
    {
        yield return new WaitForSeconds(sceneTransitionDelay); // Wait for the specified delay

        SceneManager.LoadScene(mainMenuSceneName); // Load the main menu scene
    }
}