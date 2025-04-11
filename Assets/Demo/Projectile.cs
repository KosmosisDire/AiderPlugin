using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // Ensure Rigidbody2D is present
[RequireComponent(typeof(Collider2D))] // Ensure Collider2D is present
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float lifetime = 3f; // Time in seconds before the projectile destroys itself
    public string targetTag = "Enemy"; // Tag of objects this projectile can damage

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true; // Use trigger collider for detection
    }

    void Start()
    {
        // Propel the projectile forward (assuming it's oriented correctly)
        rb.velocity = transform.up * speed;

        // Destroy the projectile after its lifetime expires
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collided object has the target tag
        if (other.CompareTag(targetTag))
        {
            // Try to get the HealthSystem component from the collided object
            HealthSystem healthSystem = other.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                // Apply damage
                healthSystem.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"Projectile hit {other.name} with tag '{targetTag}' but it has no HealthSystem component.");
            }

            // Destroy the projectile upon hitting a valid target
            Destroy(gameObject);
        }
        // Optional: Add checks for hitting environment objects, etc.
        // else if (other.CompareTag("Environment")) { Destroy(gameObject); }
    }

    // Optional: Set target tag dynamically if needed
    public void SetTargetTag(string newTargetTag)
    {
        targetTag = newTargetTag;
    }
}
