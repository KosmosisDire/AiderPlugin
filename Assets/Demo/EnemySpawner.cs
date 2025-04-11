using UnityEngine;
using System.Collections; // Required for IEnumerator

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject enemyPrefab; // Assign your Enemy prefab here
    [SerializeField] private float spawnInterval = 3.0f; // Time between spawns
    [SerializeField] private int maxEnemies = 10; // Maximum number of enemies allowed from this spawner
    [SerializeField] private float spawnDistance = 15.0f; // How far from the camera center to spawn enemies

    [Header("References")]
    [SerializeField] private Camera mainCamera; // Reference to the main camera

    private int currentEnemyCount = 0;
    private float spawnTimer;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("EnemySpawner: Main Camera not found! Please assign it in the Inspector.", this);
                enabled = false;
                return;
            }
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy Prefab not assigned! Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        // Initialize timer to allow immediate first spawn if desired, or wait full interval
        spawnTimer = spawnInterval; // Or set to 0f for immediate first spawn attempt
    }

    void Update()
    {
        // Simple spawner follows the camera's position directly
        // More complex logic could keep it offset or only update periodically
        transform.position = mainCamera.transform.position;

        // Countdown timer
        spawnTimer -= Time.deltaTime;

        // Check if it's time to spawn and if we haven't reached the max enemy count
        if (spawnTimer <= 0f && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval; // Reset timer
        }
    }

    void SpawnEnemy()
    {
        // Calculate a random direction (angle) around the camera
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // Calculate the spawn position based on the angle and distance from the camera center
        // We use the spawner's position which is tracking the camera
        Vector3 spawnDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        Vector3 spawnPosition = transform.position + spawnDirection * spawnDistance;
        // Ensure Z position is 0 for 2D
        spawnPosition.z = 0;

        // --- Optional: Check if spawn position is *outside* camera view ---
        // This prevents enemies popping directly into view if spawnDistance is too small
        // Vector3 viewportPos = mainCamera.WorldToViewportPoint(spawnPosition);
        // if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
        // {
        //     // Position is inside view, try again next frame or recalculate
        //     Debug.LogWarning("Spawn position was inside camera view, delaying spawn.");
        //      spawnTimer = 0.1f; // Try again soon
        //     return;
        // }
        // --------------------------------------------------------------------


        // Instantiate the enemy at the calculated position
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Increment enemy count
        currentEnemyCount++;

        // Optional: Subscribe to the enemy's death event to decrement count
        HealthSystem enemyHealth = newEnemy.GetComponent<HealthSystem>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDied.AddListener(HandleEnemyDeath);
        }
        else
        {
            Debug.LogWarning($"Spawned enemy '{newEnemy.name}' does not have a HealthSystem component. Max enemy count may become inaccurate.", newEnemy);
        }

        Debug.Log($"Spawned enemy at {spawnPosition}. Current count: {currentEnemyCount}");
    }

    // Method to be called when an enemy spawned by this spawner dies
    void HandleEnemyDeath()
    {
        currentEnemyCount--;
        currentEnemyCount = Mathf.Max(0, currentEnemyCount); // Ensure count doesn't go below zero
        Debug.Log($"Enemy died. Current count: {currentEnemyCount}");
    }

    // Gizmo to visualize the spawn radius in the editor
    void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnDistance); // Use transform.position as it follows camera
        }
    }
}
