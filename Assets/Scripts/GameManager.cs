// GameManager.cs
using UnityEngine;
using System.Collections; // Required for Coroutines
using System; // For Action event
using UnityEngine.SceneManagement; // Required for scene management

public class GameManager : MonoBehaviour
{
    // Singleton pattern for easy access from other scripts
    public static GameManager Instance { get; private set; }

    [Header("Scene Management")]
    public string gameplaySceneName = "Gameplay";
    public string gameOverSceneName = "GameOver";
    public string startSceneName = "Start";

    [Header("Difficulty System")]
    public static int CurrentDifficultyLevel { get; private set; }
    private int nextDifficultyIncreaseThreshold = 10000;

    [Header("Score System")]
    public int score = 0;
    public static event Action<int> OnScoreChanged;

    [Header("Build-Up Meter")]
    private int currentBuildUp = 0;
    public int maxBuildUp = 100;
    private int initialMaxBuildUpIncrement = 100;
    public static event Action<int, int> OnBuildUpChanged;
    public static event Action OnBuildUpFull;

    // --- Score Values ---
    [Header("Score Values")]
    public int smallMeteorScore = 50;
    public int mediumMeteorScore = 100;
    public int bigMeteorScore = 250;
    public int enemyShipScore = 500;
    public int mothershipScore = 1000;

    // --- Build-Up Values ---
    [Header("Build-Up Values")]
    public int smallMeteorBuildUp = 5;
    public int mediumMeteorBuildUp = 10;
    public int bigMeteorBuildUp = 15;
    public int enemyShipBuildUp = 50;

    // --- Wave Management ---
    public static event Action OnWaveCleared;
    private int specialWaveEnemyCount = 0;

    // NEW: Pause Events
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    private bool isGamePaused = false; // NEW: Track pause state

    void Awake()
    {
        // Implement singleton pattern
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

    void Start()
    {
        OnScoreChanged?.Invoke(score);
        OnBuildUpChanged?.Invoke(currentBuildUp, maxBuildUp);
    }

    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
        Debug.Log($"Score: {score}");
        while (score >= nextDifficultyIncreaseThreshold)
        {
            CurrentDifficultyLevel++;
            Debug.Log($"Difficulty Increased! Current Level: {CurrentDifficultyLevel}");
            nextDifficultyIncreaseThreshold += 10000;
        }
    }

    public void IncreaseDifficultyManually()
    {
        CurrentDifficultyLevel++;
        Debug.Log($"Difficulty manually increased by upgrade! New Level: {CurrentDifficultyLevel}");
    }

    public void PlayerKilledEnemy(string type)
    {
        int buildUpAmount = 0;
        int scoreAmount = 0;
        bool isSpecialWaveEnemy = false;

        switch (type)
        {
            case "SmallMeteor":
                buildUpAmount = smallMeteorBuildUp;
                scoreAmount = smallMeteorScore;
                break;
            case "MediumMeteor":
                buildUpAmount = mediumMeteorBuildUp;
                scoreAmount = mediumMeteorScore;
                break;
            case "BigMeteor":
                buildUpAmount = bigMeteorBuildUp;
                scoreAmount = bigMeteorScore;
                break;
            case "EnemyShip":
                buildUpAmount = enemyShipBuildUp;
                scoreAmount = enemyShipScore;
                isSpecialWaveEnemy = true;
                break;
            case "Mothership":
                buildUpAmount = enemyShipBuildUp * 2;
                scoreAmount = mothershipScore;
                isSpecialWaveEnemy = true;
                break;
            default:
                Debug.LogWarning($"GameManager: Unknown enemy type '{type}' killed by player. No build-up or score added.");
                return;
        }

        if (isSpecialWaveEnemy && specialWaveEnemyCount > 0)
        {
            specialWaveEnemyCount--;
            Debug.Log($"Special wave enemy defeated. Remaining: {specialWaveEnemyCount}");
            if (specialWaveEnemyCount <= 0)
            {
                Debug.Log("Wave Cleared! Notifying spawner.");
                OnWaveCleared?.Invoke();
            }
        }

        currentBuildUp += buildUpAmount;
        currentBuildUp = Mathf.Min(currentBuildUp, maxBuildUp);

        OnBuildUpChanged?.Invoke(currentBuildUp, maxBuildUp);
        AddScore(scoreAmount);

        if (currentBuildUp >= maxBuildUp)
        {
            Debug.Log("Build-up meter is full! Triggering upgrade prompt and healing.");
            OnBuildUpFull?.Invoke();
            ResetBuildUpMeter();
        }
    }

    public void RegisterSpecialWave(int enemyCount)
    {
        specialWaveEnemyCount = enemyCount;
        Debug.Log($"New wave registered with {enemyCount} enemies.");
    }

    private void ResetBuildUpMeter()
    {
        currentBuildUp = 0;
        maxBuildUp += initialMaxBuildUpIncrement;
        Debug.Log($"Build-up meter reset. Next target: {maxBuildUp}");
        OnBuildUpChanged?.Invoke(currentBuildUp, maxBuildUp);
    }

    public void PauseGame()
    {
        if (!isGamePaused) // Only pause if not already paused
        {
            Time.timeScale = 0f;
            isGamePaused = true;
            Debug.Log("Game Paused.");
            OnGamePaused?.Invoke(); // Notify subscribers that the game is paused
        }
    }

    public void ResumeGame()
    {
        if (isGamePaused) // Only resume if currently paused
        {
            Time.timeScale = 1f;
            isGamePaused = false;
            Debug.Log("Game Resumed.");
            OnGameResumed?.Invoke(); // Notify subscribers that the game is resumed
        }
    }

    public void EndGame()
    {
        StartCoroutine(EndGameSequence());
    }

    private IEnumerator EndGameSequence()
    {
        Debug.Log("Game Over! Final Score: " + score);

        yield return new WaitForSeconds(3f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverSceneName);
    }

    public void ResetGame()
    {
        score = 0;
        CurrentDifficultyLevel = 0;
        nextDifficultyIncreaseThreshold = 10000;
        currentBuildUp = 0;
        maxBuildUp = 100;
        specialWaveEnemyCount = 0;
        Time.timeScale = 1f;
        Debug.Log("Game state reset for new game.");
        OnScoreChanged?.Invoke(score);
        OnBuildUpChanged?.Invoke(currentBuildUp, maxBuildUp);
        isGamePaused = false; // Ensure pause state is reset
    }
    
    public void LoadStartScene()
    {
        Time.timeScale = 1f; // Ensure time is unpaused before loading
        SceneManager.LoadScene(startSceneName);
        Destroy(gameObject); // Destroy the current GameManager instance
    }
}