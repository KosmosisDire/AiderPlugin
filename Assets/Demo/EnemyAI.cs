using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(ShootingSystem))]
[RequireComponent(typeof(Rigidbody2D))] // For movement control
public class EnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string playerTag = "Player";
    private Transform playerTransform;

    [Header("Movement")]
    [SerializeField] private float accelerationForce = 8.0f; // Force applied for movement
    [SerializeField] private float maxSpeed = 5.0f;         // Maximum velocity magnitude
    [SerializeField] private float linearDrag = 1.0f;       // Drag when moving straight
    [SerializeField] private float angularDrag = 2.0f;      // Drag for rotation
    [SerializeField] private float rotationSpeed = 180.0f; // Degrees per second
    [SerializeField] private float stoppingDistance = 5.0f; // How close to get to the player
    [SerializeField] private float retreatDistance = 3.0f; // If closer than this, back away

    [Header("Shooting")]
    [SerializeField] private float shootingRange = 7.0f; // How close the player needs to be to start shooting

    // Component References
    private Rigidbody2D rb;
    private ShootingSystem shootingSystem;
    private HealthSystem healthSystem; // Optional: AI could react to taking damage

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shootingSystem = GetComponent<ShootingSystem>();
        healthSystem = GetComponent<HealthSystem>(); // Get reference if needed

        // Configure Rigidbody2D
        rb.gravityScale = 0;
        rb.drag = linearDrag;
        rb.angularDrag = angularDrag;
        // We handle rotation manually, but keep freezeRotation in case of physics glitches
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Find the player GameObject by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"EnemyAI on {gameObject.name} could not find GameObject with tag '{playerTag}'. Disabling AI.", this);
            enabled = false; // Disable AI if player not found
        }

        // Set the enemy's projectiles to target the player
        shootingSystem.SetProjectileTargetTag(playerTag);
    }

    void Update()
    {
        if (playerTransform == null || healthSystem.IsDead)
        {
            // Stop moving if player is gone or enemy is dead
            rb.linearVelocity = Vector2.zero;
            return;
        }

        HandleMovementAndRotation();
        HandleShooting();
    }

    void HandleMovementAndRotation()
    {
        Vector2 directionToPlayer = (playerTransform.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;

        // --- Rotation ---
        // Calculate the target angle to face the player
        float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f; // Adjust for sprite orientation
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        // Smoothly rotate towards the player
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // --- Movement ---
        Vector2 moveDirection = Vector2.zero;

        if (distanceToPlayer > stoppingDistance)
        {
            // Move towards player if too far
            moveDirection = directionToPlayer.normalized;
        }
        else if (distanceToPlayer < retreatDistance)
        {
            // Move away from player if too close
            moveDirection = -directionToPlayer.normalized;
        }
        // Else: Stay at the desired distance (moveDirection remains zero)

        // Apply force for movement
        if (moveDirection != Vector2.zero)
        {
            rb.AddForce(moveDirection * accelerationForce);
        }

        // Clamp velocity to maxSpeed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
        // If within stopping distance and not retreating, gradually reduce velocity
        else if (moveDirection == Vector2.zero && rb.velocity.magnitude > 0.1f)
        {
            // Rely on drag, or optionally add counter-force for faster stops
            // rb.AddForce(-rb.velocity.normalized * accelerationForce * 0.5f); // Example counter-force
        }
    }

    void HandleShooting()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Check if player is within shooting range and the shooting system is ready
        if (distanceToPlayer <= shootingRange && shootingSystem.CanShoot)
        {
            // Optional: Add a check to ensure the enemy is somewhat facing the player before shooting
            Vector2 forward = transform.up; // Assuming sprite's forward is up
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float angleToPlayer = Vector2.Angle(forward, directionToPlayer);

            // Only shoot if the player is roughly in front (e.g., within 30 degrees)
            if (angleToPlayer < 30f)
            {
                shootingSystem.Shoot();
            }
        }
    }

    // Optional: Draw gizmos in the editor for visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}
