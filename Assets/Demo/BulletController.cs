using UnityEngine;

using UnityEngine; // Ensure this is present

public class BulletController : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 2f; // Time in seconds before the bullet destroys itself
    public int damageAmount = 10; // Damage this bullet deals
    public string playerTag = "Player"; // Tag for the player
    public string enemyTag = "Enemy"; // Tag for enemies

    private Rigidbody2D rb; // Add Rigidbody2D reference

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Bullet needs a Rigidbody2D component!", this);
        }
        else
        {
            // Ensure bullet doesn't fall due to gravity
            rb.gravityScale = 0f;
        }
         // Ensure the bullet collider is present
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
             Debug.LogError("Bullet needs a Collider2D component!", this);
        }
        // Note: We'll use OnCollisionEnter2D, so 'Is Trigger' is not required.
    }

    void Update()
    {
        // Movement is now handled by setting velocity once in Start
    }

    void Start() // Renamed from Update, setting velocity once is better for bullets
    {
        // Destroy the bullet after 'lifetime' seconds
        Destroy(gameObject, lifetime);

        // Use Rigidbody velocity for consistent physics movement
        if (rb != null)
        {
            // Move the bullet forward along its local RIGHT axis (transform.right)
            // This aligns with the visual orientation because the bullet is spawned
            // with a 90-degree offset, making its visual 'forward' its local X-axis.
            rb.linearVelocity = transform.right * speed;
        }
        else // Fallback if no Rigidbody2D (shouldn't happen with the Awake check)
        {
             Debug.LogWarning("Bullet has no Rigidbody2D, using Translate fallback.", this);
             // Keep the old translate logic as a fallback, though physics is preferred
             InvokeRepeating("ManualTranslate", 0f, Time.fixedDeltaTime);
        }
    }

    // Fallback movement if no Rigidbody2D
    void ManualTranslate()
    {
        // Use Vector3.up for the fallback as well
        transform.Translate(Vector3.up * speed * Time.fixedDeltaTime, Space.Self);
    }


    // Using OnCollisionEnter2D for physics-based collisions
    void OnCollisionEnter2D(Collision2D collision)
    {
        // --- Player Bullet hitting Enemy ---
        // Check if this bullet is tagged "Bullet" (Player's bullet tag)
        // AND if it hit something tagged "Enemy"
        if (gameObject.CompareTag("Bullet") && collision.gameObject.CompareTag(enemyTag))
        {
            // Try to get the EnemyController component from the collided object
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Call the TakeDamage method on the enemy
                enemy.TakeDamage(damageAmount);
            }
            Destroy(gameObject); // Destroy the bullet itself
        }
        // --- Enemy Bullet hitting Player ---
        // Check if this bullet is tagged "EnemyBullet" (we'll create this tag later)
        // AND if it hit something tagged "Player"
        else if (gameObject.CompareTag("EnemyBullet") && collision.gameObject.CompareTag(playerTag))
        {
             // Try to get the SpaceshipController component from the collided object
             SpaceshipController player = collision.gameObject.GetComponent<SpaceshipController>();
             if (player != null)
             {
                 // Call TakeDamage on the player (we'll add this method next)
                 player.TakeDamage(damageAmount);
             }
             Destroy(gameObject); // Destroy the bullet itself
        }
        // --- Optional: Bullet hitting other things (like walls) ---
        // else if (collision.gameObject.CompareTag("Wall")) // Example
        // {
        //     Destroy(gameObject);
        // }

        // Note: We removed the Destroy(collision.gameObject) from EnemyController's collision handler,
        // because the bullet should destroy itself here after dealing damage.
    }
}
