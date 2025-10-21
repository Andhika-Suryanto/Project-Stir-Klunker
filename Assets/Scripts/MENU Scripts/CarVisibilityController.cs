using UnityEngine;
using UnityEngine.UI;

public class CarVisibilityController : MonoBehaviour
{
    [Header("Target Object to Monitor")]
    [Tooltip("When this object is enabled, cars will be shown based on sprite. When disabled, all cars hide.")]
    public GameObject targetObject;
    
    [Header("Image Component to Read Sprite From")]
    [Tooltip("The Image component whose sprite name determines which car to show")]
    public Image spriteImageComponent;
    
    [Header("Car Mappings")]
    [Tooltip("Drag sprites and their corresponding car GameObjects here")]
    public CarSpriteMapping[] carMappings;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private Sprite lastCheckedSprite;
    private bool lastTargetObjectState;
    
    void Start()
    {
        // Initialize by checking state immediately
        lastTargetObjectState = targetObject != null && targetObject.activeInHierarchy;
        CheckAndUpdateCarVisibility();
    }
    
    void Update()
    {
        // Check if target object state changed
        bool currentState = targetObject != null && targetObject.activeInHierarchy;
        
        // Check if sprite changed (only if target object is active)
        Sprite currentSprite = (spriteImageComponent != null) ? spriteImageComponent.sprite : null;
        
        // Update if anything changed
        if (currentState != lastTargetObjectState || currentSprite != lastCheckedSprite)
        {
            lastTargetObjectState = currentState;
            lastCheckedSprite = currentSprite;
            CheckAndUpdateCarVisibility();
        }
    }
    
    void CheckAndUpdateCarVisibility()
    {
        // If target object is disabled, hide all cars
        if (targetObject == null || !targetObject.activeInHierarchy)
        {
            HideAllCars();
            if (enableDebugLogs) Debug.Log("Target object disabled - hiding all cars");
            return;
        }
        
        // If target object is enabled, check which car to show
        if (spriteImageComponent == null || spriteImageComponent.sprite == null)
        {
            HideAllCars();
            if (enableDebugLogs) Debug.LogWarning("No sprite found on Image component - hiding all cars");
            return;
        }
        
        // Get current sprite
        Sprite currentSprite = spriteImageComponent.sprite;
        
        // Find matching car and show only that one
        bool foundMatch = false;
        foreach (var mapping in carMappings)
        {
            if (mapping.targetSprite == currentSprite)
            {
                // Show this car
                if (mapping.carGameObject != null)
                {
                    mapping.carGameObject.SetActive(true);
                    foundMatch = true;
                    if (enableDebugLogs) 
                        Debug.Log($"Showing car: {mapping.carGameObject.name} (matched sprite: {currentSprite.name})");
                }
            }
            else
            {
                // Hide all other cars
                if (mapping.carGameObject != null)
                {
                    mapping.carGameObject.SetActive(false);
                }
            }
        }
        
        if (!foundMatch && enableDebugLogs)
        {
            Debug.LogWarning($"No car mapping found for sprite: {currentSprite.name}");
        }
    }
    
    void HideAllCars()
    {
        foreach (var mapping in carMappings)
        {
            if (mapping.carGameObject != null)
            {
                mapping.carGameObject.SetActive(false);
            }
        }
    }
    
    // Public method to force an update (useful for external calls)
    public void ForceUpdate()
    {
        CheckAndUpdateCarVisibility();
    }
}

[System.Serializable]
public class CarSpriteMapping
{
    [Tooltip("The sprite that represents this car (drag from Project)")]
    public Sprite targetSprite;
    
    [Tooltip("The car GameObject to show/hide")]
    public GameObject carGameObject;
}