using System.Collections.Generic;
using UnityEngine;

// Add requirements for the new components
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(ShootingSystem))]
[RequireComponent(typeof(Rigidbody2D))] // <<< Add Rigidbody2D requirement
public class ShipController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float accelerationForce = 10.0f; // Force applied for movement
    [SerializeField] private float maxSpeed = 7.0f;         // Maximum velocity magnitude
    [SerializeField] private float linearDrag = 1.0f;       // Drag when moving straight
    [SerializeField] private float angularDrag = 2.0f;      // Drag for rotation (helps stabilize)

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360.0f; // Degrees per second for smoothing

    public List<GameObject> testing;

    // --- Component References ---
    private Rigidbody2D rb; // <<< Add Rigidbody2D reference
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
        rb = GetComponent<Rigidbody2D>(); // <<< Get Rigidbody2D component
        healthSystem = GetComponent<HealthSystem>();
        shootingSystem = GetComponent<ShootingSystem>();

        // Ensure components are found
        if (mainCamera == null) Debug.LogError("ShipController requires a MainCamera tagged GameObject.", this);
        if (rb == null) Debug.LogError("ShipController requires a Rigidbody2D component.", this); // <<< Check Rigidbody2D
        if (healthSystem == null) Debug.LogError("ShipController requires a HealthSystem component.", this);
        if (shootingSystem == null) Debug.LogError("ShipController requires a ShootingSystem component.", this);

        // Disable script if essential components are missing
        if (mainCamera == null || rb == null || healthSystem == null || shootingSystem == null) // <<< Add rb check
        {
            enabled = false;
            return;
        }

        // Configure Rigidbody2D
        rb.gravityScale = 0; // Ensure no gravity in 2D space game
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

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

        // Calculate movement direction vector
        Vector2 moveDirection = new Vector2(moveHorizontal, moveVertical).normalized;

        // Apply force in the direction of input
        if (moveDirection != Vector2.zero)
        {
            rb.AddForce(moveDirection * accelerationForce);
        }

        // Clamp velocity to maxSpeed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
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
