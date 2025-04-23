using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target; // The target to follow (the player)
    public float smoothTime = 0.3f; // Approximate time for the camera to reach the target
    public Vector3 offset; // Offset distance from the target

    private Vector3 velocity = Vector3.zero; // Velocity reference for SmoothDamp

    void Start()
    {
        // If offset is not set in the inspector, calculate initial offset
        if (offset == Vector3.zero && target != null)
        {
            offset = transform.position - target.position;
        }
        // Ensure the camera maintains its initial Z position if offset wasn't set manually
        if (Mathf.Approximately(offset.z, 0f)) {
             offset.z = transform.position.z - (target != null ? target.position.z : 0f);
        }
    }

    void LateUpdate()
    {
        // Only follow if the target exists
        if (target != null)
        {
            // Calculate the desired position based on the target's position and the offset
            Vector3 targetPosition = target.position + offset;

            // Smoothly move the camera towards the desired position
            // We only change X and Y, keeping the original Z unless specified in the offset
            Vector3 currentPosition = transform.position;
            currentPosition.x = Mathf.SmoothDamp(currentPosition.x, targetPosition.x, ref velocity.x, smoothTime);
            currentPosition.y = Mathf.SmoothDamp(currentPosition.y, targetPosition.y, ref velocity.y, smoothTime);
            // Keep the camera's Z position unless the offset explicitly changes it.
            // If you want the Z to follow too, use this line instead:
            // transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            transform.position = currentPosition;

        }
    }
}
