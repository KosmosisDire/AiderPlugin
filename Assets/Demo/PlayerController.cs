using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 100.0f;

    void Update()
    {
        // Get input for movement and rotation
        float verticalInput = Input.GetAxis("Vertical"); // Forward/Backward
        float horizontalInput = Input.GetAxis("Horizontal"); // Rotation

        // Move the spaceship forward/backward based on vertical input
        transform.Translate(Vector3.up * verticalInput * moveSpeed * Time.deltaTime);

        // Rotate the spaceship left/right based on horizontal input
        transform.Rotate(Vector3.forward * -horizontalInput * rotationSpeed * Time.deltaTime);
    }
}
