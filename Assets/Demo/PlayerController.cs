using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public KeyCode dashKey = KeyCode.LeftShift; // Or KeyCode.Space, etc.

    private bool isDashing = false;
    private bool canDash = true;
    private Vector2 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on the player. Please add one.");
            enabled = false; // Disable script if Rigidbody2D is missing
        }
    }

    void Update()
    {
        // --- Movement Input ---
        if (!isDashing) // Only take movement input if not dashing
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            moveInput.Normalize(); // Prevent faster diagonal movement
        }

        // --- Dash Input ---
        if (Input.GetKeyDown(dashKey) && canDash && moveInput != Vector2.zero) // Check for input, cooldown, and if moving
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        // --- Apply Movement ---
        if (!isDashing) // Only apply regular movement if not dashing
        {
            rb.velocity = moveInput * moveSpeed;
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        dashDirection = moveInput.normalized; // Dash in the current movement direction

        // Store original gravity scale if you use gravity (for top-down, usually 0)
        // float originalGravity = rb.gravityScale;
        // rb.gravityScale = 0f; // Temporarily disable gravity during dash if needed

        rb.velocity = dashDirection * dashSpeed; // Apply dash force

        yield return new WaitForSeconds(dashDuration); // Wait for dash duration

        isDashing = false;
        // rb.gravityScale = originalGravity; // Restore original gravity if changed
        rb.velocity = Vector2.zero; // Stop movement briefly after dash (optional)

        yield return new WaitForSeconds(dashCooldown); // Wait for cooldown

        canDash = true;
    }
}
