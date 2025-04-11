using UnityEngine;

public class ShipController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 200.0f; // Degrees per second for smoothing (optional)

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ShipController requires a MainCamera tagged GameObject in the scene.");
            enabled = false; // Disable script if no camera found
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

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
}
