using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 100.0f;

    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction fireAction;

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

    void Update()
    {
        // --- Apply Movement ---
        // Assuming movement in X and Y plane (for 2D or top-down 3D)
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World); // Move relative to world space

        // --- Apply Rotation ---
        // Using the horizontal input (X) of the right stick to rotate around the Z axis
        float rotationAmount = -rotateInput.x * rotateSpeed * Time.deltaTime; // Negative for intuitive control
        transform.Rotate(0, 0, rotationAmount);
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        // This method is called when the fire action is performed (button pressed)
        Debug.Log("Fire button pressed!");
        // Add your firing logic here (e.g., instantiate a bullet prefab)
    }
}
