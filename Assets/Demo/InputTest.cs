using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    // Assign the Input Action Asset in the Inspector
    public InputActionAsset inputActions;

    // Actions will be found and assigned in Awake
    [Header("Test Action")]
    public InputAction testAction { get; private set; } // Example action

    [Header("Xbox Face Buttons")]
    public InputAction buttonA { get; private set; }
    public InputAction buttonB { get; private set; }
    public InputAction buttonX { get; private set; }
    public InputAction buttonY { get; private set; }

    [Header("Xbox Joysticks")]
    public InputAction leftStick { get; private set; } // Expects Vector2
    public InputAction rightStick { get; private set; } // Expects Vector2

    // Name of the Action Map in your Input Action Asset
    private const string ActionMapName = "Player"; // <<< ADJUST THIS IF YOUR MAP NAME IS DIFFERENT

    void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Action Asset is not assigned in the Inspector!", this);
            return;
        }

        // Find the action map
        var actionMap = inputActions.FindActionMap(ActionMapName, throwIfNotFound: true);
        if (actionMap == null)
        {
             Debug.LogError($"Action Map '{ActionMapName}' not found in the assigned Input Action Asset!", this);
             return;
        }


        // Find and assign each action by name
        // Using a helper function to reduce repetition
        testAction = FindActionOrFail(actionMap, "TestAction"); // <<< ADJUST ACTION NAME IF NEEDED
        buttonA = FindActionOrFail(actionMap, "ButtonA");       // <<< ADJUST ACTION NAME IF NEEDED
        buttonB = FindActionOrFail(actionMap, "ButtonB");       // <<< ADJUST ACTION NAME IF NEEDED
        buttonX = FindActionOrFail(actionMap, "ButtonX");       // <<< ADJUST ACTION NAME IF NEEDED
        buttonY = FindActionOrFail(actionMap, "ButtonY");       // <<< ADJUST ACTION NAME IF NEEDED
        leftStick = FindActionOrFail(actionMap, "LeftStick");   // <<< ADJUST ACTION NAME IF NEEDED
        rightStick = FindActionOrFail(actionMap, "RightStick"); // <<< ADJUST ACTION NAME IF NEEDED

        Debug.Log("Input Actions assigned from Asset.");
    }

    private InputAction FindActionOrFail(InputActionMap map, string actionName)
    {
        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found within Action Map '{map.name}'!", this);
        }
        return action;
    }


    void OnEnable()
    {
        // Enable all actions and register callbacks
        EnableAndRegisterAction(testAction, OnTestActionPerformed, "Test Action");
        EnableAndRegisterAction(buttonA, OnButtonAPerformed, "Button A");
        EnableAndRegisterAction(buttonB, OnButtonBPerformed, "Button B");
        EnableAndRegisterAction(buttonX, OnButtonXPerformed, "Button X");
        EnableAndRegisterAction(buttonY, OnButtonYPerformed, "Button Y");
        EnableAndRegisterAction(leftStick, OnLeftStickMoved, "Left Stick");
        EnableAndRegisterAction(rightStick, OnRightStickMoved, "Right Stick");
    }

    void OnDisable()
    {
        // Disable all actions and unregister callbacks
        DisableAndUnregisterAction(testAction, OnTestActionPerformed, "Test Action");
        DisableAndUnregisterAction(buttonA, OnButtonAPerformed, "Button A");
        DisableAndUnregisterAction(buttonB, OnButtonBPerformed, "Button B");
        DisableAndUnregisterAction(buttonX, OnButtonXPerformed, "Button X");
        DisableAndUnregisterAction(buttonY, OnButtonYPerformed, "Button Y");
        DisableAndUnregisterAction(leftStick, OnLeftStickMoved, "Left Stick");
        DisableAndUnregisterAction(rightStick, OnRightStickMoved, "Right Stick");
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

    // --- Helper Methods for Enable/Disable ---

    private void EnableAndRegisterAction(InputAction action, System.Action<InputAction.CallbackContext> callback, string actionName)
    {
        if (action != null)
        {
            action.Enable();
            action.performed += callback;
            // Debug.Log($"{actionName} enabled and callback registered.");
        }
        else
        {
            Debug.LogWarning($"InputAction '{actionName}' is not assigned in the Inspector.");
        }
    }

    private void DisableAndUnregisterAction(InputAction action, System.Action<InputAction.CallbackContext> callback, string actionName)
    {
        if (action != null && action.enabled) // Check if it was enabled before trying to disable
        {
            action.performed -= callback;
            action.Disable();
            // Debug.Log($"{actionName} disabled and callback unregistered.");
        }
    }


    // --- Callback Methods ---

    private void OnButtonAPerformed(InputAction.CallbackContext context)
    {
        // Buttons are often checked for the "press" phase
        if (context.ReadValueAsButton()) // More specific check for button press
        {
             Debug.Log("Button A Pressed!");
        }
        // You could also add logic for context.canceled for button release
    }

    private void OnButtonBPerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            Debug.Log("Button B Pressed!");
        }
    }

    private void OnButtonXPerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            Debug.Log("Button X Pressed!");
        }
    }

    private void OnButtonYPerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            Debug.Log("Button Y Pressed!");
        }
    }

    private void OnLeftStickMoved(InputAction.CallbackContext context)
    {
        Vector2 stickValue = context.ReadValue<Vector2>();
        // Joysticks report continuously, so only log if the value is significant
        if (stickValue.sqrMagnitude > 0.01f) // Use sqrMagnitude for efficiency
        {
            Debug.Log($"Left Stick Moved: {stickValue}");
        }
    }

    private void OnRightStickMoved(InputAction.CallbackContext context)
    {
        Vector2 stickValue = context.ReadValue<Vector2>();
        if (stickValue.sqrMagnitude > 0.01f)
        {
            Debug.Log($"Right Stick Moved: {stickValue}");
        }
    }


    // --- Update Method (Optional Polling) ---

    void Update()
    {
        // You can also poll the action's state directly in Update if needed
        // if (testAction != null && testAction.IsPressed())
        // {
        //     Debug.Log("Test Action is currently pressed (polled in Update).");
        // }
    }
}
