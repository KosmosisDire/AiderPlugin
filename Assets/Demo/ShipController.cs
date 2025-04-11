using UnityEngine;

// Add requirements for the new components
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(ShootingSystem))]
public class ShipController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 200.0f; // Degrees per second for smoothing (optional)

    // --- Component References ---
    private Camera mainCamera;
    private HealthSystem healthSystem;
    private ShootingSystem shootingSystem;
    // --- Public Fields ---
    // You might want to expose these if needed, or keep them private
    // public HealthSystem Health => healthSystem;
    // public ShootingSystem Shooter => shootingSystem;


    void Awake() // Use Awake for getting components
    {
        mainCamera = Camera.main;
        healthSystem = GetComponent<HealthSystem>();
        shootingSystem = GetComponent<ShootingSystem>();

        // Ensure components are found
        if (mainCamera == null) Debug.LogError("ShipController requires a MainCamera tagged GameObject.", this);
        if (healthSystem == null) Debug.LogError("ShipController requires a HealthSystem component.", this);
        if (shootingSystem == null) Debug.LogError("ShipController requires a ShootingSystem component.", this);

        // Disable script if essential components are missing
        if (mainCamera == null || healthSystem == null || shootingSystem == null)
        {
            enabled = false;
            return;
        }

        // Set the player's projectiles to target enemies
        shootingSystem.SetProjectileTargetTag("Enemy"); // Make sure enemies have the "Enemy" tag

        // Optional: Subscribe to health events if needed (e.g., game over screen)
        // healthSystem.OnDied.AddListener(HandlePlayerDeath);
    }


    void Update()
    {
        // Don't allow control if dead
        if (healthSystem != null && healthSystem.IsDead)
        {
            // Optional: Add visual feedback for death (e.g., disable renderer)
            return;
        }

        HandleMovement();
        HandleRotation();
        HandleShootingInput(); // Add shooting input handling
    }

    // Optional: Method to handle player death
    // void HandlePlayerDeath()
    // {
    //     Debug.Log("Player Died! Game Over?");
    //     // Add game over logic here
    // }

    void HandleMovement()
    {
        // Get input axes
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrows
        float moveVertical = Input.GetAxis("Vertical");   // W/S or Up/Down Arrows

        // Calculate movement vector
        Vector3 movement = new Vector3(moveHorizontal, moveVertical, 0f);

        // Apply movement
        // Normalize to prevent faster diagonal movement, then scale by speed and time
        transform.position += movement.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        if (mainCamera == null) return; // Don't rotate if camera is missing

        // Get mouse position in screen coordinates
        Vector3 mouseScreenPosition = Input.mousePosition;

        // Convert mouse position to world coordinates
        // We need to provide a Z distance from the camera. Since it's 2D,
        // we can use the distance from the camera to the object's Z plane.
        mouseScreenPosition.z = mainCamera.transform.position.z - transform.position.z;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // Calculate direction from ship to mouse
        Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;

        // Calculate the angle needed to point towards the mouse
        // Atan2 gives the angle in radians between the positive X axis and the point (x, y)
        // We want the angle relative to the positive Y axis (up), so we use (direction.x, direction.y)
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        // Adjust angle because sprites often face upwards (positive Y) by default,
        // while angle 0 is typically the positive X axis. Subtract 90 degrees.
        targetAngle -= 90f;

        // Create the target rotation
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        // Smoothly rotate towards the target angle (optional, but looks nicer)
        // If you want instant rotation, use: transform.rotation = targetRotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // --- Alternative: Instant Rotation ---
        // float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;
        // transform.rotation = Quaternion.Euler(0f, 0f, angle);
        // ------------------------------------
    }

    void HandleShootingInput()
    {
        // Check for left mouse button click (or hold)
        if (Input.GetMouseButton(0)) // 0 is the left mouse button
        {
            if (shootingSystem != null)
            {
                shootingSystem.Shoot();
            }
        }
    }
}
