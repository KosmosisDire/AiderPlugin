using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text and Slider

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; } // Singleton instance

    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Slider healthSlider;
    // Add references for other UI elements here if needed (e.g., game over screen)

    [Header("Score Settings")]
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private int scorePerEnemy = 10;

    private int currentScore = 0;

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate UIManager found. Destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Keep the UI Manager persistent across scenes
            // DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        // Initialize UI elements
        if (scoreText == null)
        {
            Debug.LogError("UIManager: Score Text reference not set in Inspector!", this);
        }
        if (healthSlider == null)
        {
            Debug.LogError("UIManager: Health Slider reference not set in Inspector!", this);
        }

        // Set initial score text
        UpdateScoreText();
    }

    // Call this method to increase the score
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreText();
    }

    // Call this method when an enemy is defeated
    public void EnemyDefeated()
    {
        AddScore(scorePerEnemy);
    }

    // Updates the score text display
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + currentScore;
        }
    }

    // Call this method to update the player's health bar
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            // Ensure slider max value matches player's max health
            if (healthSlider.maxValue != maxHealth)
            {
                healthSlider.maxValue = maxHealth;
            }
            // Update the slider's current value
            healthSlider.value = currentHealth;
        }
    }

    // --- Add methods for other UI updates below (e.g., ShowGameOverScreen) ---

}
