using UnityEngine;
using UnityEngine.InputSystem;

using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    private UIManager uiManager; // Reference to the UI Manager
    private int maxHealth; // Store initial health for UI calculation

    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 100.0f;

    [Header("Shooting")]
    public GameObject bulletPrefab; // Assign the Player Bullet prefab in the Inspector
    public Transform firePoint; // Assign an empty GameObject child as the fire point
    public float fireRate = 5f; // Bullets per second for auto-fire
    public string bulletTag = "Bullet"; // Tag for player bullets

    [Header("Stats")]
    public int health = 100; // Player's health

    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction fireAction; // Renamed for clarity, will handle both triggers

    private Rigidbody2D rb; // Add reference for Rigidbody2D

    private Vector2 moveInput;
    private Vector2 rotateInput; // Using Vector2 for stick input, often only X is used for rotation Z
    private float nextFireTime = 0f; // Time when the next shot can be fired

    void Awake()
    {
        // Get the UI Manager instance
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("UIManager instance not found!");
        }

        // Store the initial health as max health
        maxHealth = health;

        // --- Movement Action ---
        moveAction = new InputAction("PlayerMove", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad") // Optional: Add D-pad support
            .With("Up", "<Gamepad>/dpad/up")
            .With("Down", "<Gamepad>/dpad/down")
            .With("Left", "<Gamepad>/dpad/left")
            .With("Right", "<Gamepad>/dpad/right");
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        // --- Rotation Action ---
        // Typically, only horizontal axis (left/right on stick) is used for Z rotation in 2D/top-down
        rotateAction = new InputAction("PlayerRotate", binding: "<Gamepad>/rightStick");
        rotateAction.performed += ctx => rotateInput = ctx.ReadValue<Vector2>();
        rotateAction.canceled += ctx => rotateInput = Vector2.zero;

        // --- Fire Action (Triggers) ---
        // Initialize the action without a specific binding initially
        fireAction = new InputAction("PlayerFire");
        // Add bindings for both left and right triggers
        fireAction.AddBinding("<Gamepad>/leftTrigger");
        fireAction.AddBinding("<Gamepad>/rightTrigger");
        // We will check the button state in Update for auto-fire, so no callback needed here.

        // Get the Rigidbody2D component attached to this GameObject
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on the spaceship!", this);
            // Optionally disable the script if Rigidbody2D is essential
            // this.enabled = false;
        }
        // Ensure Rigidbody2D settings are appropriate for space physics
        if (rb != null)
        {
             rb.gravityScale = 0f;
             rb.linearDamping = 0.5f; // Linear drag (resistance to movement)
            // rb.angularDamping = 0.8f; // Angular drag removed as we now set rotation directly
        }
    }

    void OnEnable()
    {
        moveAction.Enable();
        rotateAction.Enable();
        fireAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        rotateAction.Disable();
        fireAction.Disable();

        // Clean up callbacks to prevent memory leaks
        moveAction.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled -= ctx => moveInput = Vector2.zero;
        rotateAction.performed -= ctx => rotateInput = ctx.ReadValue<Vector2>();
        rotateAction.canceled -= ctx => rotateInput = Vector2.zero;
        // No callback was registered, so nothing to unregister for fireAction
    }

    // Called once after Awake
    void Start()
    {
        // Initialize the health bar display
        if (uiManager != null)
        {
            uiManager.UpdateHealthBar(health, maxHealth);
        }
    }

    // Update is fine for reading input, but physics should be in FixedUpdate
    void Update()
    {
        // --- Handle Auto-Fire ---
        // Check if the fire button is currently held down and if enough time has passed since the last shot
        if (fireAction.IsPressed() && Time.time >= nextFireTime)
        {
            // Update the time when the next shot can be fired
            nextFireTime = Time.time + 1f / fireRate;
            // Call the method to fire the bullet
            FireBullet();
        }
    }

    // Apply physics forces in FixedUpdate for consistent physics simulation
    void FixedUpdate()
    {
        if (rb == null) return; // Don't try to use rb if it wasn't found

        // --- Apply Movement Force ---
        // Use the full left stick input vector for world-space movement (strafing)
        Vector2 moveForce = moveInput * moveSpeed;
        rb.AddForce(moveForce);

        // --- Set Rotation based on Right Stick ---
        // Check if the right stick is being moved significantly
        if (rotateInput.sqrMagnitude > 0.01f) // Use square magnitude for efficiency
        {
            // Calculate the angle the stick is pointing in world space
            // Atan2 gives angle in radians relative to positive X axis
            // Convert to degrees and subtract 90 because Unity's 0 rotation is 'up' (0,1)
            float targetAngle = Mathf.Atan2(rotateInput.y, rotateInput.x) * Mathf.Rad2Deg - 90f;

            // Smoothly rotate towards the target angle
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotateSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle); // Use MoveRotation for physics-based rotation
        }
        // If the stick is near center, the ship maintains its last rotation.
    }

    // Renamed from OnFire and removed context parameter as it's called directly now
    private void FireBullet()
    {
        // Debug.Log("Firing bullet!"); // Optional: Keep for debugging auto-fire

        if (bulletPrefab != null && firePoint != null)
        {
            // Calculate the desired rotation: fire point's rotation plus 90 degrees on Z
            Quaternion spawnRotation = firePoint.rotation * Quaternion.Euler(0, 0, 90);

            // Instantiate the player bullet with the adjusted rotation
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, spawnRotation);
            // Ensure the player's bullet has the correct tag
            bullet.tag = bulletTag; // Tag the instantiated bullet
        }
        else
        {
            if(bulletPrefab == null) Debug.LogWarning("Bullet Prefab not assigned in SpaceshipController.");
            if(firePoint == null) Debug.LogWarning("Fire Point not assigned in SpaceshipController.");
        }
    }

    // Method called by enemy bullets when hitting the player
    public void TakeDamage(int damage)
    {
        health -= damage;
        // Ensure health doesn't go below zero visually
        if (health < 0) health = 0;
        Debug.Log($"Player hit! Current health: {health}");

        // Update the health bar via UIManager
        if (uiManager != null)
        {
            uiManager.UpdateHealthBar(health, maxHealth);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has been destroyed!");
        // Call the UIManager to show the Game Over screen
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
        else
        {
             Debug.LogWarning("UIManager instance not found! Cannot show Game Over screen.");
        }
        // Deactivate the player object instead of destroying it immediately,
        // so other scripts can potentially reference it if needed before scene reload.
        gameObject.SetActive(false);
        // Destroy(gameObject); // Optionally destroy if no other references are needed
    }
}
