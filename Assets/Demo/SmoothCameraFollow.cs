using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player"; // Tag of the object to follow
    [SerializeField] private float smoothSpeed = 0.125f; // Lower values make the camera follow slower/smoother
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // Default Z offset for 2D cameras

    private Transform target; // Reference to the target's transform

    void Start()
    {
        // Find the target GameObject by tag
        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            target = targetObject.transform;
            // Optional: Calculate initial offset if you want to maintain the starting distance/angle
            // offset = transform.position - target.position;
        }
        else
        {
            Debug.LogError($"SmoothCameraFollow: Could not find GameObject with tag '{targetTag}'. Camera will not follow.", this);
            enabled = false; // Disable the script if target not found
        }

        // Ensure the camera has the correct initial Z position if not using calculated offset
        if (transform.position.z != offset.z)
        {
             Vector3 initialPos = transform.position;
             initialPos.z = offset.z;
             transform.position = initialPos;
        }
    }

    void LateUpdate() // Use LateUpdate for camera movement to ensure the target has moved first
    {
        if (target == null) return; // Don't do anything if the target is missing (e.g., destroyed)

        // Calculate the desired position for the camera
        // Target's X and Y, plus the fixed Z offset
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, offset.z);

        // Smoothly interpolate between the camera's current position and the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime); // Using Lerp with Time.deltaTime for frame rate independence

        // Apply the smoothed position to the camera
        transform.position = smoothedPosition;

        // --- Alternative using SmoothDamp (provides velocity control) ---
        // private Vector3 velocity = Vector3.zero; // Add this field to the class if using SmoothDamp
        // Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        // transform.position = smoothedPosition;
        // -----------------------------------------------------------------
    }

    // Optional: Allow setting the target dynamically
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
            // Recalculate offset if needed, or assume the standard offset
            // offset = transform.position - target.position;
            enabled = true; // Ensure script is enabled if it was disabled
        }
        else
        {
            Debug.LogWarning("SmoothCameraFollow: SetTarget called with null target.", this);
            target = null;
            // Optionally disable the script or keep the camera stationary
            // enabled = false;
        }
    }
}
