// MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using UnityEngine.UI; // Required for UI elements
using System.Collections; // Required for Coroutines (for Invoke)

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("The name of your main gameplay scene (e.g., 'Gameplay').")]
    public string gameplaySceneName = "Gameplay"; // Set this in the Inspector
    [Tooltip("The name of your Game Over scene (e.g., 'GameOver').")]
    public string gameOverSceneName = "GameOver"; // Set this in the Inspector

    // --- All Audio-related variables and methods have been removed ---

    void Start()
    {
        // Ensure the game is not paused when entering the main menu
        Time.timeScale = 1f;

        // Ensure the cursor is visible and unlocked for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Call this method when the "Start Game" button is clicked.
    /// This will load the main gameplay scene after a delay.
    /// </summary>
    public void StartGame()
    {
        // Debug.Log("MainMenuManager: Starting New Game..."); // Uncomment if you want to confirm call
        // Changed delay from 0.1f to 2.0f for loading gameplay scene
        Invoke("LoadGameplaySceneDelayed", 0.5f); 
    }

    /// <summary>
    /// Call this method when the "Quit" button is clicked.
    /// This will exit the application.
    /// </summary>
    public void QuitGame()
    {
        // Debug.Log("MainMenuManager: Quitting Game..."); // Uncomment if you want to confirm call
        Invoke("PerformQuitDelayed", 0.1f); // This delay remains short for quitting
    }

    // Helper method for delayed loading of gameplay scene
    void LoadGameplaySceneDelayed()
    {
        if (GameManager.Instance != null)
        {
            // If GameManager exists and has a gameplaySceneName, use that.
            SceneManager.LoadScene(GameManager.Instance.gameplaySceneName);
        }
        else
        {
            // Fallback if GameManager isn't initialized or properly linked
            Debug.LogWarning("GameManager.Instance not found for scene name, loading default 'Gameplay'.");
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    // Helper method for delayed quitting
    void PerformQuitDelayed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the Unity Editor
#else
        Application.Quit(); // Quits the application in a build
#endif
    }
}