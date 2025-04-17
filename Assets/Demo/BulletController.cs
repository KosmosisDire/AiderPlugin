using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 2f; // Time in seconds before the bullet destroys itself

    void Start()
    {
        // Destroy the bullet after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move the bullet forward along its local X-axis (transform.right)
        // This is because the prefab/spawn rotation is offset by 90 degrees Z
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    // Optional: Add collision detection if needed
    // void OnTriggerEnter2D(Collider2D other)
    // {
    //     // Example: Destroy bullet on hitting something tagged "Enemy"
    //     if (other.CompareTag("Enemy"))
    //     {
    //         Destroy(gameObject);
    //         // Optionally destroy the enemy too: Destroy(other.gameObject);
    //     }
    // }
}
