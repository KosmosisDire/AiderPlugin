using UnityEngine;
using UnityEngine.InputSystem;

using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 100.0f;

    [Header("Shooting")]
    public GameObject bulletPrefab; // Assign the Bullet prefab in the Inspector
    public Transform firePoint; // Assign an empty GameObject child as the fire point

    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction fireAction;

    private Rigidbody2D rb; // Add reference for Rigidbody2D

    private Vector2 moveInput;
    private Vector2 rotateInput; // Using Vector2 for stick input, often only X is used for rotation Z

    void Awake()
    {
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

        // --- Fire Action ---
        fireAction = new InputAction("PlayerFire", binding: "<Gamepad>/buttonSouth"); // A button on Xbox
        fireAction.performed += OnFire; // Register callback

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
             rb.angularDamping = 0.8f; // Angular drag (resistance to rotation) - often higher than linear
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
        fireAction.performed -= OnFire;
    }

    // Update is fine for reading input, but physics should be in FixedUpdate
    void Update()
    {
        // Input reading remains here as it's tied to frame rate
    }

    // Apply physics forces in FixedUpdate for consistent physics simulation
    void FixedUpdate()
    {
        if (rb == null) return; // Don't try to use rb if it wasn't found

        // --- Apply Movement Force ---
        // Use the vertical axis of the left stick (moveInput.y) to apply force forward/backward
        // Use transform.up because in 2D, the "forward" direction is often the green Y axis
        Vector2 moveForce = (Vector2)transform.up * moveInput.y * moveSpeed;
        rb.AddForce(moveForce);

        // --- Apply Rotational Torque ---
        // Use the horizontal axis of the right stick (rotateInput.x) to apply torque for rotation
        // Negative sign often makes right stick right = clockwise rotation
        float torque = -rotateInput.x * rotateSpeed;
        rb.AddTorque(torque);
    }


    private void OnFire(InputAction.CallbackContext context)
    {
        // This method is called when the fire action is performed (button pressed)
        Debug.Log("Fire button pressed!");

        if (bulletPrefab != null)
        {
            // Use firePoint if assigned, otherwise use the ship's position/rotation
            Transform spawnTransform = (firePoint != null) ? firePoint : transform;
            // Calculate the desired rotation: spawn point's rotation plus 90 degrees on Z
            Quaternion spawnRotation = spawnTransform.rotation * Quaternion.Euler(0, 0, 90);
            Instantiate(bulletPrefab, spawnTransform.position, spawnRotation);
        }
        else
        {
            Debug.LogWarning("Bullet Prefab not assigned in SpaceshipController.");
        }
    }
}
