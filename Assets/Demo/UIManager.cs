using UnityEngine;
using UnityEngine.UI; // Required for Slider
using TMPro; 
using UnityEngine.SceneManagement;
using UnityEditor.Events;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; } // Singleton instance

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public Slider healthSlider;
    public GameObject gameOverPanel; // Reference to the Game Over Panel
    public TextMeshProUGUI finalScoreText; // Reference to the text displaying the final score

    private int currentScore = 0;

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // Keep UI across scenes if needed
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }

        // Initial UI setup
        UpdateScoreText();
        // Health bar will be initialized by the player script when it Awakes/Starts

        // Ensure the Game Over panel is initially hidden
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("GameOver Panel reference not set in UIManager.");
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreText();
    }

    // Updates the health slider based on current and max health
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            if (maxHealth > 0) // Avoid division by zero
            {
                // Calculate health percentage and set slider value
                healthSlider.value = currentHealth / maxHealth;
            }
            else
            {
                healthSlider.value = 0; // Set to empty if max health is zero or less
            }
        }
        else
        {
            Debug.LogWarning("Health Slider reference not set in UIManager.");
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
        else
        {
            Debug.LogWarning("Score Text reference not set in UIManager.");
        }
    }

    // Optional: Method to reset UI (e.g., on game restart)
    public void ResetUI()
    {
        currentScore = 0;
        UpdateScoreText();
        // Health bar reset would typically be handled by player respawn logic
        if (healthSlider != null) healthSlider.value = 1; // Reset to full
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // Hide panel on reset
    }

    // Call this method when the player dies
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); // Show the panel
            if (finalScoreText != null)
            {
                finalScoreText.text = "Final Score: " + currentScore; // Display final score
            }
            else
            {
                 Debug.LogWarning("Final Score Text reference not set in UIManager.");
            }
            Time.timeScale = 0f; // Pause the game
        }
         else
        {
            Debug.LogWarning("GameOver Panel reference not set in UIManager, cannot show Game Over screen.");
        }
    }

    // Call this method from the Restart Button's OnClick event
    public void RestartGame()
    {
        Time.timeScale = 1f; // Resume game time
        // Reload the current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
