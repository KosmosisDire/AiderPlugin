using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // Event to notify other components when health changes (e.g., update UI)
    public UnityEvent<int, int> OnHealthChanged;
    // Event to notify when the object dies
    public UnityEvent OnDied;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Invoke the event initially to set UI, etc.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (IsDead) return; // Already dead

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Ensure health doesn't go below 0 or above max

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");

        // Invoke the health changed event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (IsDead) return; // Cannot heal if dead

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Ensure health doesn't go above max

        Debug.Log($"{gameObject.name} healed {healAmount}. Current health: {currentHealth}/{maxHealth}");

        // Invoke the health changed event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        // Invoke the death event
        OnDied?.Invoke();

        // Example: Destroy the game object upon death
        // You might want different behavior (e.g., play animation, disable components)
        Destroy(gameObject, 0.1f); // Small delay to allow other scripts to react
    }
}
