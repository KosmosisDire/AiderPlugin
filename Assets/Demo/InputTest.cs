using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    // Assign an Input Action in the Inspector (e.g., create one via Assets > Create > Input Actions)
    public InputAction testAction;

    void OnEnable()
    {
        // Enable the action when the component is enabled
        if (testAction != null)
        {
            testAction.Enable();
            // Register a callback for when the action is performed
            testAction.performed += OnTestActionPerformed;
            Debug.Log("Input Action enabled and callback registered.");
        }
        else
        {
            Debug.LogError("InputAction 'testAction' is not assigned in the Inspector!");
        }
    }

    void OnDisable()
    {
        // Disable the action and unregister the callback when the component is disabled
        if (testAction != null)
        {
            testAction.performed -= OnTestActionPerformed;
            testAction.Disable();
            Debug.Log("Input Action disabled and callback unregistered.");
        }
    }

    // This method is called when the action is performed
    private void OnTestActionPerformed(InputAction.CallbackContext context)
    {
        // Read the value (e.g., float for axis, bool for button press)
        var value = context.ReadValueAsObject();
        Debug.Log($"Test Action Performed! Value: {value} (Type: {value?.GetType()})");

        // Example: Check if it's a button press
        if (context.ReadValue<float>() > 0.5f) // For button-like actions
        {
            Debug.Log("Button Pressed!");
        }
    }

    void Update()
    {
        // You can also poll the action's state directly in Update if needed
        // if (testAction != null && testAction.IsPressed())
        // {
        //     Debug.Log("Test Action is currently pressed (polled in Update).");
        // }
    }
}
