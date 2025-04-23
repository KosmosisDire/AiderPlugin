using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Assign the Enemy prefab in the Inspector
    public float spawnRate = 2.0f; // Time between spawns
    public float spawnRadius = 10.0f; // Distance from spawner to spawn enemies
    public int maxEnemies = 10; // Maximum number of enemies allowed at once
    public string enemyTag = "Enemy"; // Tag to count existing enemies

    private float nextSpawnTime;

    void Update()
    {
        // Count existing enemies
        int currentEnemyCount = GameObject.FindGameObjectsWithTag(enemyTag).Length;

        // Check if it's time to spawn and if we haven't reached the max enemy count
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is not assigned in the EnemySpawner!");
            return;
        }

        // Calculate a random spawn position within the radius around the spawner
        Vector2 spawnPositionOffset = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(spawnPositionOffset.x, spawnPositionOffset.y, 0);

        // Instantiate the enemy at the calculated position
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        // Debug.Log($"Spawned enemy at {spawnPosition}. Current count: {GameObject.FindGameObjectsWithTag(enemyTag).Length + 1}");
    }
}