using UnityEngine;

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
        rb.drag = 1.0f; // Linear drag for floaty movement
        rb.angularDrag = 1.5f; // Angular drag for floaty rotation
    }

    void Update()
    {
        // Get input for movement (Left Stick)
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // Get input for look direction (Right Stick)
        lookInput.x = Input.GetAxis("HorizontalLook");
        lookInput.y = Input.GetAxis("VerticalLook");
    }

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
