using UnityEngine;

public class CameraPositionController : MonoBehaviour
{
    [Header("Target Object to Monitor")]
    [Tooltip("When this object is enabled, camera moves to Move Position. When disabled, moves to Original Position.")]
    public GameObject targetObject;
    
    [Header("Camera to Control")]
    [Tooltip("The camera that will move between positions")]
    public Camera targetCamera;
    
    [Header("Position Targets")]
    [Tooltip("The original position (where camera returns when object is disabled)")]
    public Transform originalPosition;
    
    [Tooltip("The move position (where camera goes when object is enabled)")]
    public Transform movePosition;
    
    [Header("Movement Settings")]
    [Tooltip("How fast the camera moves between positions")]
    public float moveSpeed = 5f;
    
    [Tooltip("Use smooth damping instead of linear interpolation")]
    public bool useSmoothDamping = true;
    
    [Tooltip("Smooth time for damping (only if useSmoothDamping is true)")]
    public float smoothTime = 0.3f;
    
    [Header("Rotation Settings")]
    [Tooltip("Should the camera also rotate to match the target transforms?")]
    public bool matchRotation = true;
    
    [Tooltip("Rotation speed (if matchRotation is enabled)")]
    public float rotationSpeed = 5f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private bool lastTargetObjectState;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 velocity = Vector3.zero; // For SmoothDamp
    
    void Start()
    {
        // Validate references
        if (targetCamera == null)
        {
            Debug.LogError("Target Camera is not assigned!");
            enabled = false;
            return;
        }
        
        if (originalPosition == null || movePosition == null)
        {
            Debug.LogError("Original Position or Move Position is not assigned!");
            enabled = false;
            return;
        }
        
        // Initialize state
        lastTargetObjectState = targetObject != null && targetObject.activeInHierarchy;
        UpdateTargetPosition();
    }
    
    void Update()
    {
        // Check if target object state changed
        bool currentState = targetObject != null && targetObject.activeInHierarchy;
        
        if (currentState != lastTargetObjectState)
        {
            lastTargetObjectState = currentState;
            UpdateTargetPosition();
        }
        
        // Move camera towards target position
        MoveCameraToTarget();
    }
    
    void UpdateTargetPosition()
    {
        if (lastTargetObjectState)
        {
            // Object is enabled - move to Move Position
            targetPosition = movePosition.position;
            targetRotation = movePosition.rotation;
            
            if (enableDebugLogs)
                Debug.Log($"Target object enabled - moving camera to Move Position: {targetPosition}");
        }
        else
        {
            // Object is disabled - return to Original Position
            targetPosition = originalPosition.position;
            targetRotation = originalPosition.rotation;
            
            if (enableDebugLogs)
                Debug.Log($"Target object disabled - moving camera to Original Position: {targetPosition}");
        }
    }
    
    void MoveCameraToTarget()
    {
        // Move position
        if (useSmoothDamping)
        {
            // Smooth damping (ease in/out)
            targetCamera.transform.position = Vector3.SmoothDamp(
                targetCamera.transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }
        else
        {
            // Linear interpolation (constant speed)
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );
        }
        
        // Rotate if enabled
        if (matchRotation)
        {
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }
    
    // Public method to instantly snap to target position (no smooth movement)
    public void SnapToTarget()
    {
        UpdateTargetPosition();
        targetCamera.transform.position = targetPosition;
        
        if (matchRotation)
        {
            targetCamera.transform.rotation = targetRotation;
        }
        
        velocity = Vector3.zero; // Reset smooth damp velocity
    }
    
    // Public method to force update (useful for external calls)
    public void ForceUpdate()
    {
        lastTargetObjectState = targetObject != null && targetObject.activeInHierarchy;
        UpdateTargetPosition();
    }
}