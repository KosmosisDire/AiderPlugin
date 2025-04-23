using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    // Actions will be created and configured in Awake
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


    void Awake()
    {
        // Create and configure actions directly
        testAction = new InputAction("TestAction", binding: "<Keyboard>/space"); // Example binding

        buttonA = new InputAction("ButtonA", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
        buttonB = new InputAction("ButtonB", type: InputActionType.Button, binding: "<Gamepad>/buttonEast");
        buttonX = new InputAction("ButtonX", type: InputActionType.Button, binding: "<Gamepad>/buttonWest");
        buttonY = new InputAction("ButtonY", type: InputActionType.Button, binding: "<Gamepad>/buttonNorth");

        leftStick = new InputAction("LeftStick", type: InputActionType.Value, binding: "<Gamepad>/leftStick");
        // Add deadzone processor for joysticks if desired
        leftStick.AddBinding("<XInputController>/leftStick").WithProcessor("stickDeadzone"); // Example for XInput specifically

        rightStick = new InputAction("RightStick", type: InputActionType.Value, binding: "<Gamepad>/rightStick");
        rightStick.AddBinding("<XInputController>/rightStick").WithProcessor("stickDeadzone"); // Example for XInput specifically

        // You can add more bindings if needed, e.g., for keyboard fallbacks
        // buttonA.AddBinding("<Keyboard>/j");

        Debug.Log("Input Actions created and configured.");
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
