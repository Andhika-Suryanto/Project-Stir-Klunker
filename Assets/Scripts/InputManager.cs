using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    [Header("Steering Wheel Settings")]
    public float steeringWheelCooldown = 1f; // 1 second cooldown for steering wheel
    private float lastSteeringInputTime = 0f;
    private int lastSteeringDirection = 0; // Track last steering direction to prevent repeated inputs

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // Events for input
    public System.Action<int> OnNavigate; // -1 = left, 1 = right
    public System.Action OnConfirm;
    public System.Action OnBack;

    private void Update()
    {
        // Cooldown to prevent rapid inputs
        if (Time.time - lastInputTime < inputCooldown) return;

        // Navigation Input
        int direction = GetNavigationInput();
        if (direction != 0)
        {
            OnNavigate?.Invoke(direction);
            lastInputTime = Time.time;
            if (enableDebugLogs) Debug.Log($"Navigation input: {direction}");
        }

        // Confirm Input
        if (GetConfirmInput())
        {
            OnConfirm?.Invoke();
            lastInputTime = Time.time;
            if (enableDebugLogs) Debug.Log("Confirm input detected");
        }

        // Back Input
        if (GetBackInput())
        {
            OnBack?.Invoke();
            lastInputTime = Time.time;
            if (enableDebugLogs) Debug.Log("Back input detected");
        }
    }

    private int GetNavigationInput()
    {
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || 
                Keyboard.current.aKey.wasPressedThisFrame) return -1;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || 
                Keyboard.current.dKey.wasPressedThisFrame) return 1;
        }

        // Gamepad (Xbox/PS)
        if (Gamepad.current != null)
        {
            // D-Pad
            if (Gamepad.current.dpad.left.wasPressedThisFrame) return -1;
            if (Gamepad.current.dpad.right.wasPressedThisFrame) return 1;
            
            // Shoulder buttons
            if (Gamepad.current.leftShoulder.wasPressedThisFrame) return -1; // LB/L1
            if (Gamepad.current.rightShoulder.wasPressedThisFrame) return 1; // RB/R1
            
            // Left stick (removed - axis doesn't have wasPressedThisFrame)
            // For stick input, it's better to use a threshold system with a previous state check
        }

        // Logitech G29 Steering Wheel (with cooldown)
        int steeringDirection = GetSteeringWheelInputWithCooldown();
        if (steeringDirection != 0) return steeringDirection;

        return 0;
    }

    private bool GetConfirmInput()
    {
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || 
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (enableDebugLogs) Debug.Log("Keyboard confirm: Enter/Space");
                return true;
            }
        }

        // Gamepad - ONLY check buttonSouth for confirm
        if (Gamepad.current != null)
        {
            // Xbox A / PlayStation X (Cross) - Bottom button
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                if (enableDebugLogs) Debug.Log("Gamepad confirm: A/X (buttonSouth)");
                return true;
            }
        }

        // Logitech G29 buttons - Remove JoystickButton1 as it conflicts with B button
        // Only use JoystickButton0 for G29 confirm
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            if (enableDebugLogs) Debug.Log("G29 confirm button (Button 0)");
            return true;
        }

        return false;
    }

    private bool GetBackInput()
    {
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || 
                Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                if (enableDebugLogs) Debug.Log("Keyboard back: Escape/Backspace");
                return true;
            }
        }

        // Gamepad
        if (Gamepad.current != null)
        {
            // Xbox B / PlayStation O (Circle) - Right button
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                if (enableDebugLogs) Debug.Log("Gamepad back: B/O (buttonEast)");
                return true;
            }
            
            // Also check for Select/Back button on some controllers
            if (Gamepad.current.selectButton != null && 
                Gamepad.current.selectButton.wasPressedThisFrame)
            {
                if (enableDebugLogs) Debug.Log("Gamepad back: Select button");
                return true;
            }
        }

        // Logitech G29 back button - Use different buttons that don't conflict
        if (Input.GetKeyDown(KeyCode.JoystickButton2) || 
            Input.GetKeyDown(KeyCode.JoystickButton3) ||
            Input.GetKeyDown(KeyCode.JoystickButton6) || 
            Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (enableDebugLogs) Debug.Log("G29 back button");
            return true;
        }

        return false;
    }

    private int GetSteeringWheelInputWithCooldown()
    {
        try
        {
            float steeringInput = Input.GetAxis("Horizontal");
            
            // Determine current steering direction
            int currentDirection = 0;
            if (steeringInput > 0.3f) currentDirection = 1;  // Right
            else if (steeringInput < -0.3f) currentDirection = -1; // Left

            // If no steering input, reset the last direction
            if (currentDirection == 0)
            {
                lastSteeringDirection = 0;
                return 0;
            }

            // Check if we're still in the cooldown period
            if (Time.time - lastSteeringInputTime < steeringWheelCooldown)
            {
                return 0; // Still in cooldown
            }

            // Check if direction has changed or if we're starting fresh
            if (currentDirection != lastSteeringDirection)
            {
                // Update tracking variables
                lastSteeringDirection = currentDirection;
                lastSteeringInputTime = Time.time;
                
                if (enableDebugLogs) 
                {
                    Debug.Log($"Steering wheel input: {(currentDirection == 1 ? "RIGHT" : "LEFT")} (cooldown applied)");
                }
                
                return currentDirection;
            }
        }
        catch
        {
            // Steering wheel not connected
        }

        return 0;
    }

    // Original method kept for reference (now unused)
    private int GetSteeringWheelInput()
    {
        try
        {
            float steeringInput = Input.GetAxis("Horizontal");
            
            // Threshold for steering wheel detection
            if (steeringInput > 0.3f) return 1;  // Right
            if (steeringInput < -0.3f) return -1; // Left
        }
        catch
        {
            // Steering wheel not connected
        }

        return 0;
    }

    // Debug method to test button mappings
    [ContextMenu("Enable Debug Logs")]
    public void EnableDebugLogging()
    {
        enableDebugLogs = true;
        Debug.Log("Debug logging enabled for InputManager");
    }

    [ContextMenu("Disable Debug Logs")]
    public void DisableDebugLogging()
    {
        enableDebugLogs = false;
        Debug.Log("Debug logging disabled for InputManager");
    }

    // Method to adjust steering wheel cooldown at runtime
    [ContextMenu("Set Steering Cooldown to 0.5s")]
    public void SetSteeringCooldownHalf()
    {
        steeringWheelCooldown = 0.5f;
        Debug.Log("Steering wheel cooldown set to 0.5 seconds");
    }

    [ContextMenu("Set Steering Cooldown to 1s")]
    public void SetSteeringCooldownOne()
    {
        steeringWheelCooldown = 1f;
        Debug.Log("Steering wheel cooldown set to 1 second");
    }

    [ContextMenu("Set Steering Cooldown to 1.5s")]
    public void SetSteeringCooldownOneHalf()
    {
        steeringWheelCooldown = 1.5f;
        Debug.Log("Steering wheel cooldown set to 1.5 seconds");
    }
}