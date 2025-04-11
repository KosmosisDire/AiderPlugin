using UnityEngine;

public class ShootingSystem : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab; // Assign your projectile prefab here
    [SerializeField] private Transform firePoint;       // Assign a child GameObject representing the muzzle
    [Tooltip("Number of shots fired per second.")]
    [SerializeField] private float fireRate = 2.0f;     // Shots per second (e.g., 2 = 2 shots/sec = 0.5s delay)
    [SerializeField] private string projectileTargetTag = "Enemy"; // Default target tag for projectiles

    private float nextFireTime = 0f; // Timestamp when the system can fire again

    // Property to check if ready to fire
    public bool CanShoot => Time.time >= nextFireTime;

    void Update()
    {
        // This script itself doesn't handle input; it just provides the Shoot method.
        // Input handling should be done in the controller (Player or AI).
    }

    public void Shoot()
    {
        if (CanShoot && projectilePrefab != null && firePoint != null)
        {
            // Update the time when the next shot can be fired
            nextFireTime = Time.time + 1f / fireRate;

            // Instantiate the projectile at the fire point's position and rotation
            GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // Get the Projectile component and set its target tag
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetTargetTag(projectileTargetTag);
            }
            else
            {
                Debug.LogError("Projectile prefab is missing the Projectile script!", projectilePrefab);
            }

            // Optional: Add muzzle flash effect, sound, etc. here
        }
    }

    // Call this if you need to change who the projectiles should target
    public void SetProjectileTargetTag(string tag)
    {
        projectileTargetTag = tag;
    }
}
