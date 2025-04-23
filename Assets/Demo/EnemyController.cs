using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int health = 100; // Default health
    public int damageFromPlayerBullet = 10; // Damage taken from a player bullet
    public string playerBulletTag = "Bullet"; // Tag to identify player bullets

    [Header("Movement")]
    public float moveSpeed = 3f; // Speed at which the enemy moves
    public float stoppingDistance = 5f; // How far away the enemy stops from the player

    [Header("Shooting")]
    public GameObject bulletPrefab; // Assign the Enemy Bullet prefab
    public Transform firePoint; // Assign an empty GameObject child as the fire point
    public float fireRate = 2f; // Seconds between shots
    public string playerTag = "Player"; // Tag of the player GameObject

    private Transform playerTransform;
    private float timeSinceLastShot = 0f;
    private Rigidbody2D rb; // Reference to the Rigidbody2D

    void Start()
    {
        // Find the player GameObject
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"Player GameObject with tag '{playerTag}' not found! Enemy cannot aim or shoot.", this);
        }

        // Ensure firePoint is assigned
        if (firePoint == null)
        {
             Debug.LogError("Fire Point not assigned in EnemyController! Enemy cannot shoot.", this);
        }
        // Ensure bulletPrefab is assigned
        if (bulletPrefab == null)
        {
             Debug.LogError("Bullet Prefab not assigned in EnemyController! Enemy cannot shoot.", this);
        }

        // Get the Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on the enemy!", this);
        }
        else
        {
            // Ensure gravity doesn't affect the enemy
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        // Aiming and Shooting logic
        // Aiming and shooting logic remains in Update
        if (playerTransform != null && bulletPrefab != null && firePoint != null)
        {
            AimAtPlayer(); // Keep aiming logic here
            HandleShooting(); // Keep shooting logic here
        }
    }

    // Physics-related movement should be in FixedUpdate
    void FixedUpdate()
    {
        if (playerTransform == null || rb == null)
        {
            // If no player or rigidbody, do nothing
            if(rb != null) rb.linearVelocity = Vector2.zero; // Stop moving if player disappears
            return;
        }

        // Calculate distance to player
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // Check if we should move
        if (distance > stoppingDistance)
        {
            // Calculate direction towards the player
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            // Set velocity to move towards the player
            rb.linearVelocity = direction * moveSpeed;

            // Optional: Keep aiming logic in FixedUpdate if jitter occurs
            // AimAtPlayer(); // Uncomment if rotation seems jerky in Update
        }
        else
        {
            // Stop moving if close enough
            rb.linearVelocity = Vector2.zero;
        }
    }

    void AimAtPlayer()
    {
        // Calculate direction to player
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        // Calculate angle to face the player (assuming enemy sprite's 'up' is forward)
        // Subtract 90 degrees because Unity's 0 rotation is 'up' (0,1)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        // Apply rotation directly (can be smoothed later if needed)
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void HandleShooting()
    {
        timeSinceLastShot += Time.deltaTime;
        if (timeSinceLastShot >= fireRate)
        {
            Shoot();
            timeSinceLastShot = 0f; // Reset timer
        }
    }

    void Shoot()
    {
        // Calculate the desired rotation: fire point's rotation plus 90 degrees on Z
        Quaternion spawnRotation = firePoint.rotation * Quaternion.Euler(0, 0, 90);

        // Instantiate the bullet at the fire point's position with the adjusted rotation
        // Ensure the bullet prefab is correctly configured (tag, components) later
        Instantiate(bulletPrefab, firePoint.position, spawnRotation);
        // Debug.Log("Enemy fired!"); // Optional debug message
    }


    // Use OnCollisionEnter2D for 2D physics
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the colliding object has the specified player bullet tag
        if (collision.gameObject.CompareTag(playerBulletTag))
        {
            // Reduce health by the damage amount
            TakeDamage(damageFromPlayerBullet);
            Debug.Log($"Enemy hit by {playerBulletTag}. Current health: {health}");

            // Destroy the bullet that hit the enemy
            Destroy(collision.gameObject);

            // Check if health has dropped to zero or below
            if (health <= 0)
            {
                Die();
            }
        }
    }

    // Method for taking damage (can be called by bullets)
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    // Method to handle enemy death
    void Die()
    {
        Debug.Log("Enemy died.");
        // Destroy the enemy GameObject
        Destroy(gameObject);
        // Optional: Add explosion effects, score updates, etc. here
    }
}
