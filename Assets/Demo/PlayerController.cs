using UnityEngine;
using UnityEngine.InputSystem; // Add this using statement

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveForce = 10.0f; // Force applied for movement
    public float rotationSpeed = 200.0f; // Speed of rotation towards look direction
    public float lookDeadzone = 0.1f; // Ignore small inputs for look direction

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lookInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure Rigidbody settings are appropriate for top-down 2D space
        rb.gravityScale = 0;
        rb.linearDamping = 1.0f; // Linear drag for floaty movement
        rb.angularDamping = 1.5f; // Angular drag for floaty rotation
    }

    // --- Input System Message Callbacks ---

    // Called by PlayerInput component when Move action is triggered
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Called by PlayerInput component when Look action is triggered
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();

        // If using mouse delta for look, you might need different handling
        // For example, accumulating delta or directly setting rotation.
        // The current setup assumes joystick-like Vector2 input for look direction.
        // If mouse delta feels too jerky, consider adjusting sensitivity in the Input Actions asset
        // or implementing different rotation logic here based on device (Gamepad vs Mouse).
    }

    // --- Physics Update ---

    void FixedUpdate()
    {
        // Apply movement force
        if (moveInput.magnitude > 0)
        {
            rb.AddForce(moveInput.normalized * moveForce);
        }

        // Apply rotation based on look input
        if (lookInput.magnitude > lookDeadzone)
        {
            // Calculate the angle the ship should face
            float targetAngle = Mathf.Atan2(lookInput.y, lookInput.x) * Mathf.Rad2Deg - 90f; // Subtract 90 because sprite's 'up' is likely forward

            // Smoothly rotate towards the target angle
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            // --- Alternative: Apply Torque (more physics-based rotation feel) ---
            // float angleDifference = Mathf.DeltaAngle(rb.rotation, targetAngle);
            // rb.AddTorque(angleDifference * rotationTorque * Time.fixedDeltaTime); // Adjust rotationTorque value
            // --------------------------------------------------------------------
        }
    }
}
